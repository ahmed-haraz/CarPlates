using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Mobile.Platforms.iOS.Services;

public class NfcCardReaderService : INfcCardReaderService
{
    public bool IsNfcAvailable => false;

    public Task<CardInfo?> ReadCardDataAsync()
    {
        System.Diagnostics.Debug.WriteLine("NFC card reading is not yet implemented on iOS.");
        return Task.FromResult<CardInfo?>(null);
    }
}
