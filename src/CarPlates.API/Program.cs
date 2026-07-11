using CarPlates.API.Configuration;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Middleware;
using CarPlates.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IScanRecordService, ScanRecordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));


builder.WebHost.ConfigureKestrel(options =>
{
    // Plain HTTP kept for convenient same-network debugging from a device/emulator
    // that hasn't (or can't) trust a dev certificate. HTTPS is required for the
    // real mobile app builds: iOS App Transport Security blocks plain HTTP by
    // default, and Android 9+ (API 28+) blocks cleartext traffic by default too -
    // both would silently fail every request against an HTTP-only API.
    options.ListenAnyIP(56035);
    options.ListenAnyIP(56036, listenOptions => listenOptions.UseHttps());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

// Only redirects to the HTTPS port configured above; harmless if a client
// only ever calls the HTTPS port directly.
app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();