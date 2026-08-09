using System.Security.Claims;

namespace CarPlates.API.Common;

public interface IUserContext
{
    string? UserId { get; }
    int BranchId { get; }
    int SalesRepId { get; }
    int StoreId { get; }
    int CarId { get; }
    int CashboxId { get; }
    int UserType { get; }
    string? CompanyCode { get; }
}

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? CompanyCode =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("companyCode");

    public int BranchId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("branchId"), out var id) ? id : 0;

    public int SalesRepId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("salesRepId"), out var id) ? id : 0;

    public int StoreId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("storeId"), out var id) ? id : 0;

    public int CarId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("carId"), out var id) ? id : 0;

    public int CashboxId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("cashboxId"), out var id) ? id : 0;

    public int UserType => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirstValue("usertype"), out var id) ? id : 0;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
}
