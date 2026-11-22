using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarPlates.Shared.Constants;
using Microsoft.Data.SqlClient;

namespace CarPlates.API.Data;

/// <summary>
/// Resolves a tenant connection. Validated token claims own authenticated tenant
/// identity; a header is only a login/refresh routing hint, never authorization.
/// Resolution errors fail closed. No local cross-tenant fallback is permitted.
/// </summary>
public interface ICompanyConnectionProvider
{
    /// <summary>The company code resolved for the current request.</summary>
    string CompanyCode { get; }

    /// <summary>The SQL Server connection string for that company.</summary>
    string ConnectionString { get; }
}

public class CompanyConnectionProvider(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    ILogger<CompanyConnectionProvider> logger) : ICompanyConnectionProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<CompanyConnectionProvider> _logger = logger;

    private static readonly ConcurrentDictionary<string, CachedConnection> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Client => _httpClientFactory.CreateClient("FwApi");

    public string CompanyCode
    {
        get
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("Tenant resolution requires a request.");
            var headers = context.Request.Headers[CompanyConstants.CompanyCodeHeader];
            if (headers.Count > 1)
                throw new UnauthorizedAccessException("Multiple tenant headers are not allowed.");
            var header = headers.FirstOrDefault();
            var authenticated = context.User.Identity?.IsAuthenticated == true;
            string? code;
            if (authenticated)
            {
                var claims = context.User.FindAll("companyCode").ToArray();
                if (claims.Length != 1)
                    throw new UnauthorizedAccessException("A single tenant claim is required.");
                code = claims[0].Value;
                if (header != null && !string.Equals(header, code, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException("Tenant header does not match token.");
            }
            else
            {
                var path = context.Request.Path;
                if (path != "/api/v1/Auth/login" && path != "/api/v1/Auth/refresh")
                    throw new UnauthorizedAccessException("Tenant access requires authentication.");
                code = header;
            }

            if (string.IsNullOrWhiteSpace(code) || code.Length > 64 ||
                code.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
                throw new UnauthorizedAccessException("A valid company code is required.");
            return code;
        }
    }

    public string ConnectionString
    {
        get
        {
            var companyCode = CompanyCode;

            if (Cache.TryGetValue(companyCode, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return cached.Value;
            }

            var connectionString = FetchConnectionString(companyCode);
            Cache[companyCode] = new CachedConnection(connectionString, DateTimeOffset.UtcNow.Add(CacheTtl));
            return connectionString;
        }
    }

    private string FetchConnectionString(string companyCode)
    {
        try
        {
            using var response = Client.GetAsync($"api/FwMobileControls/1/1/{Uri.EscapeDataString(companyCode)}")
                .GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "FwApi returned {Status} for company {CompanyCode}; tenant access denied.",
                    (int)response.StatusCode, companyCode);
                throw new InvalidOperationException("Tenant database resolution failed.");
            }

            var control = response.Content.ReadFromJsonAsync<CompanyControlRaw>(JsonOptions)
                .GetAwaiter().GetResult();

            if (control == null ||
                string.IsNullOrWhiteSpace(control.Server) ||
                string.IsNullOrWhiteSpace(control.Database) ||
                string.IsNullOrWhiteSpace(control.User) ||
                control.Password == null)
            {
                _logger.LogWarning(
                    "FwApi returned no database configuration for company {CompanyCode}; tenant access denied.",
                    companyCode);
                throw new InvalidOperationException("Tenant database resolution failed.");
            }

            var sql = new SqlConnectionStringBuilder
            {
                DataSource = control.Server,
                InitialCatalog = control.Database,
                UserID = control.User,
                Password = control.Password,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

            _logger.LogInformation("Resolved database connection for company {CompanyCode}", companyCode);
            return sql.ConnectionString;
        }
        catch (Exception)
        {
            _logger.LogWarning(
                "Failed to resolve connection for company {CompanyCode} from FwApi; tenant access denied.",
                companyCode);
            throw new InvalidOperationException("Tenant database resolution failed.");
        }
    }

    private record CompanyControlRaw(
        [property: JsonPropertyName("server")] string? Server,
        [property: JsonPropertyName("sUser")] string? User,
        [property: JsonPropertyName("sDatabase")] string? Database,
        [property: JsonPropertyName("sPassword")] string? Password);

    private record CachedConnection(string Value, DateTimeOffset ExpiresAt);
}
