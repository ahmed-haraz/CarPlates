using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class PaymentViewModel : BaseViewModel
{
    private readonly IPaymentApiService _paymentApiService;
    private readonly IReceiptPrintService _printService;
    private readonly INfcCardReaderService _nfcService;
    private readonly IPaymentGatewayService _gatewayService;

    [ObservableProperty] private long _headerId;
    [ObservableProperty] private string _docTransNo = string.Empty;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _plateNumber = string.Empty;
    [ObservableProperty] private string _workLocationName = string.Empty;
    [ObservableProperty] private string _technicianName = string.Empty;
    [ObservableProperty] private string _color = string.Empty;
    [ObservableProperty] private string _plateType = string.Empty;
    [ObservableProperty] private double _total;
    [ObservableProperty] private double _netTotal;
    [ObservableProperty] private double _paid;
    [ObservableProperty] private double _balance;
    [ObservableProperty] private int _transDate;

    [ObservableProperty] private double _cashAmount;
    [ObservableProperty] private double _visaAmount;
    [ObservableProperty] private double _bankAmount;
    [ObservableProperty] private string _notes = string.Empty;

    // Card details (Visa / gateway)
    [ObservableProperty] private string _cardNumber = string.Empty;
    [ObservableProperty] private string _cardExpiry = string.Empty;
    [ObservableProperty] private string _cardCvv = string.Empty;
    [ObservableProperty] private string _cardholderName = string.Empty;

    [ObservableProperty] private ReceiptApiResult? _receipt;
    [ObservableProperty] private bool _isPaid;
    [ObservableProperty] private bool _isProcessing;

    public PaymentViewModel(
        INavigationService navigation,
        IPaymentApiService paymentApiService,
        IReceiptPrintService printService,
        INfcCardReaderService nfcService,
        IPaymentGatewayService gatewayService) : base(navigation)
    {
        _paymentApiService = paymentApiService;
        _printService = printService;
        _nfcService = nfcService;
        _gatewayService = gatewayService;
        Title = AppResources.Cashier;
    }

    public void LoadBill(long headerId, string? docTransNo, string? customerName, string? plateNumber,
        double total, double netTotal, double paid, double balance, int transDate,
        string? workLocationName = null, string? technicianName = null, string? color = null, string? plateType = null)
    {
        HeaderId = headerId;
        DocTransNo = docTransNo ?? string.Empty;
        CustomerName = customerName ?? string.Empty;
        PlateNumber = plateNumber ?? string.Empty;
        WorkLocationName = workLocationName ?? string.Empty;
        TechnicianName = technicianName ?? string.Empty;
        Color = color ?? string.Empty;
        PlateType = plateType ?? string.Empty;
        Total = total;
        NetTotal = netTotal;
        Paid = paid;
        Balance = balance;
        TransDate = transDate;
        CashAmount = Balance;
        VisaAmount = 0;
        BankAmount = 0;
        CardNumber = string.Empty;
        CardExpiry = string.Empty;
        CardCvv = string.Empty;
        CardholderName = string.Empty;
        IsPaid = balance <= 0;
        Receipt = null;
    }

    partial void OnCashAmountChanged(double value) => RecalculateBalance();
    partial void OnVisaAmountChanged(double value) => RecalculateBalance();
    partial void OnBankAmountChanged(double value) => RecalculateBalance();

    private void RecalculateBalance() { }

    [RelayCommand]
    private async Task ScanNfcCardAsync()
    {
        var cardInfo = await _nfcService.ReadCardDataAsync();
        if (cardInfo == null) return;

        CardNumber = FormatCardNumber(cardInfo.CardNumber);
        CardExpiry = $"{cardInfo.ExpiryMonth:D2}/{cardInfo.ExpiryYear % 100:D2}";
        CardCvv = cardInfo.Cvv ?? string.Empty;
        CardholderName = cardInfo.CardholderName ?? string.Empty;
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (IsPaid || Balance <= 0)
        {
            await Navigation.DisplayAlertAsync(AppResources.Error, "Bill is already fully paid");
            return;
        }

        var payments = new List<PaymentDetailItem>
        {
            new(1, Balance)
        };
        CashAmount = Balance;

        IsProcessing = true;
        await ExecuteAsync(async () =>
        {
            var request = new PayBillApiRequest(HeaderId, payments, Notes);
            var result = await _paymentApiService.PayAsync(request);

            if (result.Success)
            {
                IsPaid = true;
                var receipt = await _paymentApiService.GetReceiptAsync(HeaderId);
                Receipt = receipt;
                await Navigation.DisplayAlertAsync(AppResources.Success,
                    $"Payment successful. Receipt: {result.ReceiptNo}");
            }
            else
            {
                await Navigation.DisplayAlertAsync(AppResources.Error, result.Message ?? "Payment failed");
            }
        });
        IsProcessing = false;
    }

    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (Receipt == null) return;

        await ExecuteAsync(async () =>
        {
            var printers = (await _printService.GetAvailablePrintersAsync()).ToList();
            var options = new List<string>();
            options.AddRange(printers);
            options.Add("Print via Driver (any printer)");
            options.Add("Enter printer IP...");

            var selected = await Navigation.DisplayActionSheetAsync(
                "Select Printer", "Cancel", null, [.. options]);

            if (selected == "Cancel" || selected == null)
                return;

            if (selected == "Print via Driver (any printer)")
            {
                await _printService.PrintReceiptAsync(Receipt, null, PrintFormat.ReceiptViaDriver);
            }
            else if (selected == "Enter printer IP...")
            {
                var ip = await Navigation.DisplayPromptAsync(
                    "Network Printer",
                    "Enter printer IP address (e.g. 192.168.1.100:9100):",
                    "Print", "Cancel",
                    placeholder: "IP:Port (default port 9100)");
                if (string.IsNullOrWhiteSpace(ip)) return;
                await _printService.PrintReceiptAsync(Receipt, ip.Trim(), PrintFormat.Receipt);
            }
            else
            {
                await _printService.PrintReceiptAsync(Receipt, selected, PrintFormat.Receipt);
            }

            await Navigation.DisplayAlertAsync("Print", "Receipt sent to printer");
        });
    }

    [RelayCommand]
    private async Task PrintA4Async()
    {
        if (Receipt == null) return;

        await ExecuteAsync(async () =>
        {
            await _printService.PrintReceiptAsync(Receipt, format: PrintFormat.A4);
        });
    }

    [RelayCommand]
    private async Task PreviewReceiptAsync()
    {
        if (Receipt == null) return;

        var items = string.Join("\n",
            Receipt.Details.Select(d => $"  {(d.ItemName ?? d.ItemBarCode),-25} {d.Qty,5:F0} x {d.Price,8:F2}"));

        var payments = string.Join("\n",
            Receipt.Payments.Select(p =>
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank", _ => "Other" };
                return $"  {method,-12} {p.Amount,10:F2}";
            }));

        var message = $"Receipt: {Receipt.ReceiptNo}\n" +
                      $"Date: {Receipt.TransDate}\n" +
                      $"Customer: {Receipt.CustomerName ?? "N/A"}\n" +
                      $"Plate: {Receipt.PlateNumber ?? "N/A"}\n" +
                      $"Location: {Receipt.WorkLocationName ?? "N/A"}\n" +
                      $"Technician: {Receipt.TechnicianName ?? "N/A"}\n" +
                      $"Color: {Receipt.Color ?? "N/A"}\n" +
                      $"Plate Type: {Receipt.PlateType ?? "N/A"}\n" +
                      $"{new string('-', 32)}\n" +
                      $"{items}\n" +
                      $"{new string('-', 32)}\n" +
                      $"Total:     {Receipt.Total,10:F2}\n" +
                      $"Paid:      {Receipt.Paid,10:F2}\n" +
                      $"Balance:   {Receipt.Balance,10:F2}\n" +
                      $"{new string('-', 32)}\n" +
                      $"{payments}\n" +
                      $"{new string('-', 32)}\n" +
                      $"Thank you!";

        await Navigation.DisplayAlertAsync("Receipt Preview", message);
    }

    private CardInfo? ParseCardInfo()
    {
        var raw = CardNumber.Replace(" ", "").Replace("-", "");
        if (raw.Length < 13) return null;

        int? expMonth = null, expYear = null;
        if (!string.IsNullOrWhiteSpace(CardExpiry))
        {
            var parts = CardExpiry.Split('/', ' ');
            if (parts.Length == 2 && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var y))
            {
                expMonth = m;
                expYear = y + (y < 100 ? 2000 : 0);
            }
        }

        return new CardInfo
        {
            CardNumber = raw,
            ExpiryMonth = expMonth ?? 0,
            ExpiryYear = expYear ?? 0,
            Cvv = string.IsNullOrWhiteSpace(CardCvv) ? null : CardCvv,
            CardholderName = string.IsNullOrWhiteSpace(CardholderName) ? null : CardholderName,
            IsNfcRead = false
        };
    }

    private static string FormatCardNumber(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        return string.Join(" ", Enumerable.Range(0, (digits.Length + 3) / 4)
            .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4))));
    }
}
