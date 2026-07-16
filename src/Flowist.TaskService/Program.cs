using System.Reflection;
using System.Text;

using Flowist.Shared.Extensions;
using Flowist.TaskService.Authorization;
using Flowist.TaskService.Data;
using Flowist.TaskService.Options;
using Flowist.TaskService.Services;

using FluentValidation;
using FluentValidation.AspNetCore;

using MassTransit;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using RabbitMQ.Client;

using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder.AddService(
            serviceName: "Flowist.TaskService",
            serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown");
    })
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter();
    })
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter();
    });

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);
});

string rabbitMqHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException(
        "RabbitMq host configuration is missing.");

string rabbitMqUsername = builder.Configuration["RabbitMq:Username"]
    ?? throw new InvalidOperationException(
        "RabbitMq username configuration is missing.");

string rabbitMqPassword = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException(
        "RabbitMq password configuration is missing.");

int rabbitMqPort = builder.Configuration.GetValue("RabbitMq:Port", 5672);

string defaultConnection = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection connection string is missing.");

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flowist TaskService API",
        Version = "v1",
        Description = "Workspace, project and task management API for Flowist."
    });

    OpenApiSecurityScheme securityScheme = new()
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token. Example: Bearer eyJhbGciOi...",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    string xmlFile =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    string xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddGlobalExceptionHandling();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddFluentValidationAutoValidation(options =>
{
    options.DisableDataAnnotationsValidation = true;
});

builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

JwtOptions jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IAuthorizationHandler,
    WorkspaceRoleAuthorizationHandler>();

builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    WorkspaceRoleAuthorizationPolicyProvider>();

builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.UsingRabbitMq(
        (context, rabbitMqConfigurator) =>
        {
            rabbitMqConfigurator.Host(
                new Uri($"rabbitmq://{rabbitMqHost}:{rabbitMqPort}/"),
                hostConfigurator =>
                {
                    hostConfigurator.Username(rabbitMqUsername);
                    hostConfigurator.Password(rabbitMqPassword);
                });

            rabbitMqConfigurator.UseMessageRetry(
                retryConfigurator =>
                {
                    retryConfigurator.Interval(
                        3,
                        TimeSpan.FromSeconds(2));
                });
        });
});

builder.Services.AddSingleton<IConnection>(_ =>
{
    ConnectionFactory connectionFactory = new()
    {
        HostName = rabbitMqHost,
        Port = rabbitMqPort,
        UserName = rabbitMqUsername,
        Password = rabbitMqPassword
    };

    return connectionFactory
        .CreateConnectionAsync()
        .GetAwaiter()
        .GetResult();
});

builder.Services.AddDbContext<TaskServiceDbContext>(options =>
{
    options.UseNpgsql(defaultConnection);
});

builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        defaultConnection,
        name: "postgresql")
    .AddRabbitMQ(
        name: "rabbitmq");

var app = builder.Build();

app.UseCorrelationId();
app.UseRequestContextLogging();
app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();

    TaskServiceDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<TaskServiceDbContext>();

    await dbContext.Database.MigrateAsync();

    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Flowist TaskService API v1");

        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;