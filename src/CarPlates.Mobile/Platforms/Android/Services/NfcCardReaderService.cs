using Android.Content;
using Android.Nfc;
using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Mobile.Platforms.Android.Services;

public class NfcCardReaderService : INfcCardReaderService
{
    private NfcAdapter? _nfcAdapter;

    public bool IsNfcAvailable
    {
        get
        {
            var activity = Platform.CurrentActivity;
            if (activity == null) return false;
            _nfcAdapter ??= NfcAdapter.GetDefaultAdapter(activity);
            return _nfcAdapter != null && _nfcAdapter.IsEnabled;
        }
    }

    public async Task<CardInfo?> ReadCardDataAsync()
    {
        if (!IsNfcAvailable)
        {
            await Microsoft.Maui.Controls.Application.Current!.Windows[0].Page!.DisplayAlertAsync(
                "NFC", "NFC is not available or disabled. Please enter card details manually.", "OK");
            return null;
        }

        // NFC card reading requires a payment gateway SDK (Stripe, Square, etc.)
        // For now, guide the user to enter card details manually.
        await Microsoft.Maui.Controls.Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            "Scan Card",
            "Hold the card near the device.\n\n" +
            "Note: NFC card reading requires a payment gateway SDK integration. " +
            "For now, please enter card details manually.",
            "OK");

        return null;
    }
}
