using CarPlates.API.Models;

namespace CarPlates.API.Interface;

public interface IReceiptTemplateService
{
    Task<List<ReceiptTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReceiptTemplate?> GetByFormatAsync(string format, CancellationToken cancellationToken = default);
    Task<ReceiptTemplate> SaveAsync(ReceiptTemplate template, CancellationToken cancellationToken = default);
    Task SeedDefaultsAsync(CancellationToken cancellationToken = default);
}
