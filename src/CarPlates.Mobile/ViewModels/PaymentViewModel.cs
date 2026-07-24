using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class PaymentViewModel : BaseViewModel
{
    private readonly IPaymentApiService _paymentApiService;
    private readonly IReceiptPrintService _printService;

    [ObservableProperty] private long _headerId;
    [ObservableProperty] private string _docTransNo = string.Empty;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _referenceNo = string.Empty;
    [ObservableProperty] private double _total;
    [ObservableProperty] private double _netTotal;
    [ObservableProperty] private double _paid;
    [ObservableProperty] private double _balance;
    [ObservableProperty] private int _transDate;

    [ObservableProperty] private double _cashAmount;
    [ObservableProperty] private double _visaAmount;
    [ObservableProperty] private double _bankAmount;
    [ObservableProperty] private string _notes = string.Empty;

    [ObservableProperty] private ReceiptApiResult? _receipt;
    [ObservableProperty] private bool _isPaid;
    [ObservableProperty] private bool _isProcessing;

    public PaymentViewModel(
        INavigationService navigation,
        IPaymentApiService paymentApiService,
        IReceiptPrintService printService) : base(navigation)
    {
        _paymentApiService = paymentApiService;
        _printService = printService;
        Title = AppResources.Cashier;
    }

    public void LoadBill(long headerId, string? docTransNo, string? customerName, string? referenceNo,
        double total, double netTotal, double paid, double balance, int transDate)
    {
        HeaderId = headerId;
        DocTransNo = docTransNo ?? string.Empty;
        CustomerName = customerName ?? string.Empty;
        ReferenceNo = referenceNo ?? string.Empty;
        Total = total;
        NetTotal = netTotal;
        Paid = paid;
        Balance = balance;
        TransDate = transDate;
        CashAmount = Balance;
        VisaAmount = 0;
        BankAmount = 0;
        IsPaid = false;
        Receipt = null;
    }

    partial void OnCashAmountChanged(double value) => RecalculateBalance();
    partial void OnVisaAmountChanged(double value) => RecalculateBalance();
    partial void OnBankAmountChanged(double value) => RecalculateBalance();

    private void RecalculateBalance() { }

    [RelayCommand]
    private async Task PayAsync()
    {
        var payments = new List<PaymentDetailItem>();
        if (CashAmount > 0) payments.Add(new PaymentDetailItem(1, CashAmount));
        if (VisaAmount > 0) payments.Add(new PaymentDetailItem(2, VisaAmount));
        if (BankAmount > 0) payments.Add(new PaymentDetailItem(3, BankAmount));

        if (payments.Count == 0)
        {
            await Navigation.DisplayAlertAsync(AppResources.Error, "Enter at least one payment amount");
            return;
        }

        var totalPay = payments.Sum(p => p.Amount);
        if (totalPay <= 0)
        {
            await Navigation.DisplayAlertAsync(AppResources.Error, "Payment amount must be greater than zero");
            return;
        }

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
            await _printService.PrintReceiptAsync(Receipt, PrintFormat.Receipt);
        });
    }

    [RelayCommand]
    private async Task PrintA4Async()
    {
        if (Receipt == null) return;

        await ExecuteAsync(async () =>
        {
            await _printService.PrintReceiptAsync(Receipt, PrintFormat.A4);
        });
    }

    [RelayCommand]
    private async Task PreviewReceiptAsync()
    {
        if (Receipt == null) return;

        var items = string.Join("\n",
            Receipt.Details.Select(d => $"  {d.ItemBarCode,-20} {d.Qty,5:F0} x {d.Price,8:F2}"));

        var payments = string.Join("\n",
            Receipt.Payments.Select(p =>
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank", _ => "Other" };
                return $"  {method,-12} {p.Amount,10:F2}";
            }));

        var message = $"Receipt: {Receipt.ReceiptNo}\n" +
                      $"Date: {Receipt.TransDate}\n" +
                      $"Customer: {Receipt.CustomerName ?? "N/A"}\n" +
                      $"Plate: {Receipt.ReferenceNo ?? "N/A"}\n" +
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
}
