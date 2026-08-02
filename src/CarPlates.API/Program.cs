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

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Value ?? "";
var origins = allowedOrigins
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(o => Uri.TryCreate(o, UriKind.Absolute, out _))
    .ToArray();

if (origins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowWebApp", policy =>
        {
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CarPlates API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
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

builder.Services.Configure<LegacyDesOptions>(
    builder.Configuration.GetSection("LegacyDes"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HexaConnection")));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
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

builder.Services.AddHttpClient("FwApi", client =>
{
    client.BaseAddress = new Uri("https://online.arkancloud.com:7070");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(56035);
    //options.ListenAnyIP(56036, listenOptions => listenOptions.UseHttps());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowWebApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ReceivedIP>(CarPlates.Shared.Constants.SignalRConstants.HubPath);

app.Run();