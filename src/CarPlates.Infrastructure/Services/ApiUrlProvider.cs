using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Infrastructure.Services;

public class ApiUrlProvider(string initialApiUrl) : IApiUrlProvider
{
    private volatile string _currentApiUrl = initialApiUrl;

    public string CurrentApiUrl => _currentApiUrl;

    public void SetApiUrl(string url) => _currentApiUrl = url;
}
