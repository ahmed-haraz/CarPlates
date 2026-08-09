namespace CarPlates.Application.Common.Interfaces;

public interface ICompanyApiService
{
    Task<CompanyInfoResult> GetCompanyInfoAsync(string companyCode, CancellationToken cancellationToken = default);
}

public record CompanyInfoResult(bool Success, string? CompanyName, string? LogoUrl, string? ErrorMessage);
