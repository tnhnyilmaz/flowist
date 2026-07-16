using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

using Flowist.AuthService.Data;
using Flowist.AuthService.Options;
using Flowist.AuthService.Services;
using Flowist.Shared.Extensions;

using FluentValidation;
using FluentValidation.AspNetCore;

using MassTransit;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
            serviceName: "Flowist.AuthService",
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

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation(options =>
{
    options.DisableDataAnnotationsValidation = true;
});

builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flowist AuthService API",
        Version = "v1",
        Description = "Authentication and token management API for Flowist."
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
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

JwtOptions jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt configuration is missing.");

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

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            string? tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrWhiteSpace(tokenId))
            {
                context.Fail("Token id is missing.");
                return;
            }

            ITokenBlacklistService tokenBlacklistService =
                context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();

            bool isBlacklisted = await tokenBlacklistService.IsBlacklistedAsync(tokenId);

            if (isBlacklisted)
            {
                context.Fail("Token has been revoked.");
            }
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(
        "auth-fixed",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
});

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

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();
builder.Services.AddScoped<IRefreshTokenCacheService, RedisRefreshTokenCacheService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHostedService<ExpiredRefreshTokenCleanupService>();

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

builder.Services.AddDbContext<AuthDbContext>(options =>
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

    AuthDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    await dbContext.Database.MigrateAsync();

    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Flowist AuthService API v1");

        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;