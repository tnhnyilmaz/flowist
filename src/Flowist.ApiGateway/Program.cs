using System.Threading.RateLimiting;

using Flowist.Shared.Extensions;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCorsPolicy", policy =>
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("http://localhost:5123/swagger/v1/swagger.json", "AuthService API");
        options.SwaggerEndpoint("http://localhost:5147/swagger/v1/swagger.json", "TaskService API");
        options.SwaggerEndpoint("http://localhost:5233/swagger/v1/swagger.json", "NotificationService API");
        options.SwaggerEndpoint("http://localhost:5038/swagger/v1/swagger.json", "ActivityService API");
        options.RoutePrefix = "swagger";
    });
}

app.UseCorrelationId();
app.UseRequestContextLogging();
app.UseSerilogRequestLogging();

app.UseCors("GatewayCorsPolicy");

app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapReverseProxy();

app.Run();