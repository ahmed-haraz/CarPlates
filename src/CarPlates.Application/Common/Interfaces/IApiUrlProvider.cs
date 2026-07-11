namespace CarPlates.Application.Common.Interfaces;

/// <summary>
/// Holds the API base URL currently in effect. Unlike reading Preferences once at
/// app startup and baking that value into the HttpClient's BaseAddress forever,
/// this is consulted fresh every time a new HttpClient is requested from the
/// factory - so changing the API URL in Settings takes effect on the very next
/// API call, no app restart required.
/// </summary>
public interface IApiUrlProvider
{
    string CurrentApiUrl { get; }
    void SetApiUrl(string url);
}
