using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class PaymentGatewaySettingsService(ApplicationDbContext context) : IPaymentGatewaySettingsService
{
    public async Task<PaymentGatewaySetting?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await context.PaymentGatewaySettings
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentGatewaySetting> SaveAsync(PaymentGatewaySetting settings, CancellationToken cancellationToken = default)
    {
        var existing = await context.PaymentGatewaySettings
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            existing.IsEnabled = settings.IsEnabled;
            existing.GatewayName = settings.GatewayName;
            existing.MerchantId = settings.MerchantId;
            existing.ApiKey = settings.ApiKey;
            existing.EndpointUrl = settings.EndpointUrl;
            existing.AdditionalSettings = settings.AdditionalSettings;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            settings.CreatedAt = DateTime.UtcNow;
            settings.UpdatedAt = DateTime.UtcNow;
            context.PaymentGatewaySettings.Add(settings);
            existing = settings;
        }

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
