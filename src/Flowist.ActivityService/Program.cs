using System.Reflection;
using System.Text;

using Flowist.ActivityService.Consumers;
using Flowist.ActivityService.Data;
using Flowist.ActivityService.Options;
using Flowist.ActivityService.Services;
using Flowist.Shared.Extensions;

using FluentValidation;
using FluentValidation.AspNetCore;

using MassTransit;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation(options =>
{
    options.DisableDataAnnotationsValidation = true;
});

builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flowist ActivityService API",
        Version = "v1",
        Description = "Activity feed and audit trail API for Flowist."
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

    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddGlobalExceptionHandling();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

JwtOptions jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IProcessedEventService, ProcessedEventService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<ActivityDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddConsumer<TaskCreatedEventConsumer>()
    .Endpoint(endpointConfigurator =>
    {
        endpointConfigurator.Name = "activity-task-created";
    });

    busConfigurator.AddConsumer<TaskAssignedEventConsumer>()
        .Endpoint(endpointConfigurator =>
        {
            endpointConfigurator.Name = "activity-task-assigned";
        });
    busConfigurator.AddConsumer<ProjectCreatedEventConsumer>()
    .Endpoint(endpointConfigurator =>
    {
        endpointConfigurator.Name = "activity-project-created";
    });
    busConfigurator.AddConsumer<MemberAddedEventConsumer>()
    .Endpoint(endpointConfigurator =>
    {
        endpointConfigurator.Name = "activity-member-added";
    });
    busConfigurator.AddConsumer<UserRegisteredEventConsumer>()
    .Endpoint(endpointConfigurator =>
    {
        endpointConfigurator.Name = "activity-user-registered";
    });
    busConfigurator.UsingRabbitMq((context, rabbitMqConfigurator) =>
    {
        string host = builder.Configuration["RabbitMq:Host"]
            ?? throw new InvalidOperationException("RabbitMq host configuration is missing.");

        string username = builder.Configuration["RabbitMq:Username"]
            ?? throw new InvalidOperationException("RabbitMq username configuration is missing.");

        string password = builder.Configuration["RabbitMq:Password"]
            ?? throw new InvalidOperationException("RabbitMq password configuration is missing.");

        rabbitMqConfigurator.Host(host, "/", hostConfigurator =>
        {
            hostConfigurator.Username(username);
            hostConfigurator.Password(password);
        });

        rabbitMqConfigurator.UseMessageRetry(retryConfigurator =>
        {
            retryConfigurator.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(10),
                intervalDelta: TimeSpan.FromSeconds(2));
        });

        rabbitMqConfigurator.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseGlobalExceptionHandling();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    ActivityDbContext dbContext = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
    await dbContext.Database.MigrateAsync();

    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Flowist ActivityService API v1");
        options.RoutePrefix = "swagger";
    });
}
app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();