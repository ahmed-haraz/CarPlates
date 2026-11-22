using System.Threading.RateLimiting;
using CarPlates.API.Common;
using CarPlates.API.Configuration;
using CarPlates.API.Data;
using CarPlates.API.Hubs;
using CarPlates.API.Interface;
using CarPlates.API.Middleware;
using CarPlates.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Resolve environment variable placeholders
// ============================================================

foreach (var entry in builder.Configuration.AsEnumerable()
             .Where(e => e.Value?.StartsWith("${") == true && e.Value.EndsWith("}"))
             .ToList())
{
    var envVarName = entry.Value!.Substring(2, entry.Value.Length - 3);

    var envValue = Environment.GetEnvironmentVariable(envVarName);

    if (envValue != null)
    {
        builder.Configuration[entry.Key] = envValue;
    }
}

// ============================================================
// Serilog
// ============================================================

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File(
        "logs/api-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// CORS
// ============================================================

var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

origins = origins
    .Where(origin =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    .Select(origin => origin.TrimEnd('/'))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ============================================================
// Controllers / API
// ============================================================

builder.Services.AddControllers();

// In-process IP limit. Production edge protection and account-aware controls
// are still required, especially across multiple replicas or a reverse proxy.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();

// ============================================================
// Swagger
// ============================================================

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CarPlates API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",

        Name = "Authorization",

        In = ParameterLocation.Header,

        Type = SecuritySchemeType.Http,

        Scheme = "bearer",

        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// ============================================================
// Legacy DES
// ============================================================

builder.Services.Configure<LegacyDesOptions>(
    builder.Configuration.GetSection("LegacyDes"));

// ============================================================
// Database
// ============================================================

// The SQL connection string is resolved per request from the
// company code sent by the calling app (X-Company-Code header).
//
// Each request scope builds its own DbContext options so different
// companies hit different databases.

builder.Services.AddScoped<ICompanyConnectionProvider, CompanyConnectionProvider>();

builder.Services.AddDbContext<ApplicationDbContext>(
    (sp, options) =>
    {
        var connectionProvider =
            sp.GetRequiredService<ICompanyConnectionProvider>();

        options.UseSqlServer(
            connectionProvider.ConnectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null));
    },
    ServiceLifetime.Scoped,
    ServiceLifetime.Scoped);

// ============================================================
// Authentication / Authorization
// ============================================================

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();

// ============================================================
// Services
// ============================================================

builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddScoped<IScanRecordService, ScanRecordService>();

builder.Services.AddScoped<ICustomerCarService, CustomerCarService>();

builder.Services.AddScoped<IWorkshopLookupService, WorkshopLookupService>();

builder.Services.AddScoped<IItemService, ItemService>();

builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<IBillService, BillService>();

builder.Services.AddScoped<IBillAttachmentService, BillAttachmentService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IDeviceValidationService, DeviceValidationService>();

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<ILovService, LovService>();

builder.Services.AddScoped<IPaymentGatewaySettingsService, PaymentGatewaySettingsService>();

builder.Services.AddScoped<IVehicleColorService, VehicleColorService>();

builder.Services.AddScoped<IReceiptTemplateService, ReceiptTemplateService>();

builder.Services.AddHostedService<PublishIPMonitorService>();

// ============================================================
// HTTP Clients
// ============================================================

builder.Services.AddHttpClient("FwApi", client =>
{
    client.BaseAddress = new Uri("https://online.arkancloud.com:7070");

    client.Timeout = TimeSpan.FromSeconds(30);

    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/json");
});

// ============================================================
// AutoMapper
// ============================================================

builder.Services.AddAutoMapper(
    cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

// ============================================================
// Kestrel
// ============================================================

// Configure HTTPS endpoints/certificates through Kestrel configuration or
// a reviewed trusted-proxy deployment. Do not hard-code an all-interface HTTP port.

// ============================================================
// Build Application
// ============================================================

var app = builder.Build();

// ============================================================
// Swagger
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================
// Logging
// ============================================================

// Reject, do not redirect, plaintext requests carrying credentials.
// Forwarded headers are deliberately not trusted automatically.
app.Use(async (context, next) =>
{
    if (!app.Environment.IsDevelopment() && !context.Request.IsHttps)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    await next(context);
});
app.UseRouting();
app.UseRateLimiter();
app.UseSerilogRequestLogging();

// ============================================================
// Exception Middleware
// ============================================================

app.UseMiddleware<ExceptionMiddleware>();

// ============================================================
// CORS
// ============================================================

app.UseCors("AllowWebApp");

// ============================================================
// Authentication / Authorization
// ============================================================

app.UseAuthentication();

app.UseAuthorization();

// ============================================================
// Endpoints
// ============================================================

app.MapControllers();

app.MapHub<ReceivedIP>(
    CarPlates.Shared.Constants.SignalRConstants.HubPath);

// ============================================================
// Run
// ============================================================

app.Run();
