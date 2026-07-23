using System.Security.Claims;

namespace CarPlates.API.Common;

public interface IUserContext
{
    string? UserId { get; }
    int BranchId { get; }
    int SalesRepId { get; }
}

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public int BranchId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("branchId"), out var id) ? id : 0;

    public int SalesRepId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("salesRepId"), out var id) ? id : 0;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
}
