using CarPlates.Shared.Models;

namespace CarPlates.Application.Common.Interfaces;

/// <summary>Provides receipt templates from the server, with local fallback.</summary>
public interface IReceiptTemplateService
{
    /// <summary>Returns the template content for a given format, or null if unavailable.</summary>
    Task<string?> GetTemplateAsync(string format, CancellationToken cancellationToken = default);

    /// <summary>Clears cached templates so they are re-fetched on the next request.</summary>
    void InvalidateCache();
}
