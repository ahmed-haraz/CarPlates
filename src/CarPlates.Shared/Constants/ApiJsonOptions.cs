using System.Text.Json;

namespace CarPlates.Shared.Constants;

/// <summary>
/// ASP.NET Core controllers serialize responses using camelCase property names
/// by default (e.g. "username", "car", "wasNewCar"), while the client-side DTOs/records
/// use PascalCase (e.g. "Username", "Car", "WasNewCar"). The System.Text.Json
/// HttpClient extension methods (ReadFromJsonAsync/PostAsJsonAsync/GetFromJsonAsync)
/// are case-sensitive by default when no options are supplied, so those names never
/// matched and every incoming property silently deserialized to its default value
/// (null/0/false) instead of throwing - which is why things like the dashboard's
/// logged-in user name, or the vehicle lookup after a scan, silently came back empty.
///
/// Every HttpClient JSON call in the app should pass this options instance explicitly.
/// </summary>
public static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
}
