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

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(56035);

    // options.ListenAnyIP(
    //     56036,
    //     listenOptions => listenOptions.UseHttps());
});

// ============================================================
// Build Application
// ============================================================

var app = builder.Build();

// ============================================================
// Swagger
// ============================================================

app.UseSwagger();

app.UseSwaggerUI();

// ============================================================
// Logging
// ============================================================

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