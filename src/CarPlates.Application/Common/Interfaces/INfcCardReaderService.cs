namespace CarPlates.Application.Common.Interfaces;

public class CardInfo
{
    public string CardNumber { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string? Cvv { get; set; }
    public string? CardholderName { get; set; }
    public bool IsNfcRead { get; set; }
}

public interface INfcCardReaderService
{
    Task<CardInfo?> ReadCardDataAsync();
    bool IsNfcAvailable { get; }
}
