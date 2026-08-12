using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Infrastructure.Services;

public class ApiUrlProvider(string initialApiUrl) : IApiUrlProvider
{
    private volatile string _currentApiUrl = Normalize(initialApiUrl);

    public string CurrentApiUrl => _currentApiUrl;

    public void SetApiUrl(string url) => _currentApiUrl = Normalize(url);

    /// <summary>
    /// Ensures the API URL is a clean base URL that always carries the "/api/v1/"
    /// path segment. Without it, requests are sent to the site root (e.g.
    /// ".../Auth/login") where IIS serves them as static paths and rejects the
    /// HTTP verb with a 405 error.
    ///
    /// Rules:
    ///  - host-only URL (e.g. "https://host:8052")  -> "https://host:8052/api/v1/"
    ///  - already versioned (e.g. ".../api/v1")     -> kept, trailing "/" ensured
    ///  - full endpoint pasted (e.g. ".../api/v1/Auth/login")
    ///                                             -> truncated to ".../api/v1/"
    ///  - something else (e.g. ".../v2")           -> left untouched (plus "/")
    /// </summary>
    public static string Normalize(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var trimmed = url.Trim().TrimEnd('/');

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            // Not a valid absolute URL - store as given; the caller's request
            // will surface a meaningful error rather than a silent rewrite.
            return trimmed;
        }

        // Search the path for the versioned API segment.
        var pathSegments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var apiIndex = pathSegments.FindIndex(segment =>
            string.Equals(segment, "api", StringComparison.OrdinalIgnoreCase) &&
            pathSegments.Count > pathSegments.IndexOf(segment) + 1 &&
            pathSegments[pathSegments.IndexOf(segment) + 1].StartsWith("v", StringComparison.OrdinalIgnoreCase));

        if (apiIndex >= 0)
        {
            // Rebuild the authority + the path up to and including the version segment.
            var versionedPath = string.Join('/', pathSegments.Take(apiIndex + 2));
            return $"{uri.Scheme}://{uri.Authority}/{versionedPath}/";
        }

        return trimmed + "/api/v1/";
    }
}
