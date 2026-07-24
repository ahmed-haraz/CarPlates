using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CarPlates.Mobile.Views.Actions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class CashierViewModel : BaseViewModel
{
    private readonly IBillApiService _billApiService;
    private readonly IPaymentApiService _paymentApiService;
    private readonly IReceiptPrintService _printService;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _canGoToPreviousPage;
    [ObservableProperty] private bool _canGoToNextPage;
    [ObservableProperty] private ObservableCollection<BillApiItem> _bills = new();
    [ObservableProperty] private DateTime _dateFrom;
    [ObservableProperty] private DateTime _dateTo;

    private const int PageSize = 20;

    public CashierViewModel(
        INavigationService navigation,
        IBillApiService billApiService,
        IPaymentApiService paymentApiService,
        IReceiptPrintService printService) : base(navigation)
    {
        _billApiService = billApiService;
        _paymentApiService = paymentApiService;
        _printService = printService;
        Title = AppResources.Cashier;
        _dateFrom = DateTime.Today;
        _dateTo = DateTime.Today;
    }

    [RelayCommand]
    private async Task LoadBillsAsync()
    {
        await ExecuteAsync(async () =>
        {
            int? dateFrom = DateFrom != DateTime.MinValue ? int.Parse(DateFrom.ToString("yyyyMMdd")) : null;
            int? dateTo = DateTo != DateTime.MinValue ? int.Parse(DateTo.ToString("yyyyMMdd")) : null;

            var result = await _billApiService.SearchBillsAsync(
                SearchText, dateFrom, dateTo, CurrentPage, PageSize);

            if (result.Success)
            {
                Bills = new ObservableCollection<BillApiItem>(result.Items);
                TotalPages = Math.Max(result.TotalPages, 1);
                CanGoToPreviousPage = CurrentPage > 1;
                CanGoToNextPage = CurrentPage < TotalPages;
            }
            else
            {
                ShowAlert(AppResources.Error, result.ErrorMessage ?? "Failed to load bills");
            }
        });
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage <= 1) return;
        CurrentPage--;
        await LoadBillsAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage >= TotalPages) return;
        CurrentPage++;
        await LoadBillsAsync();
    }

    [RelayCommand]
    private async Task PreviewBillAsync(BillApiItem bill)
    {
        await ExecuteAsync(async () =>
        {
            var receipt = await _paymentApiService.GetReceiptAsync(bill.HeaderId);
            if (receipt == null)
            {
                var items = $"Bill #{bill.HeaderId}\nDate: {bill.TransDate}\nCustomer: {bill.CustomerName ?? "N/A"}\nPlate: {bill.ReferenceNo ?? "N/A"}\nTotal: {bill.NetTotal:N2}\nPaid: {bill.Paid:N2}\nBalance: {bill.Balance:N2}";
                await Navigation.DisplayAlertAsync("Bill Details", items);
                return;
            }

            var details = string.Join("\n",
                receipt.Details.Select(d => $"  {d.ItemBarCode,-20} {d.Qty,5:F0} x {d.Price,8:F2}"));

            var message = $"Receipt: {receipt.ReceiptNo ?? "N/A"}\n" +
                          $"Bill #{receipt.HeaderId}\n" +
                          $"Date: {receipt.TransDate}\n" +
                          $"Customer: {receipt.CustomerName ?? "N/A"}\n" +
                          $"Plate: {receipt.ReferenceNo ?? "N/A"}\n" +
                          $"{new string('-', 32)}\n" +
                          $"{details}\n" +
                          $"{new string('-', 32)}\n" +
                          $"Total:     {receipt.Total,10:F2}\n" +
                          $"Paid:      {receipt.Paid,10:F2}\n" +
                          $"Balance:   {receipt.Balance,10:F2}";

            await Navigation.DisplayAlertAsync("Bill Preview", message);
        });
    }

    [RelayCommand]
    private async Task PayBillAsync(BillApiItem bill)
    {
        var paymentVm = IPlatformApplication.Current!.Services.GetRequiredService<PaymentViewModel>();
        paymentVm.LoadBill(bill.HeaderId, bill.DocTransNo, bill.CustomerName, bill.ReferenceNo,
            bill.Total, bill.NetTotal, bill.Paid, bill.Balance, bill.TransDate ?? 0);
        var page = new Views.Actions.PaymentPage(paymentVm);
        await Navigation.PushPageAsync(page);
    }

    [RelayCommand]
    private async Task PrintBillAsync(BillApiItem bill)
    {
        await ExecuteAsync(async () =>
        {
            var receipt = await _paymentApiService.GetReceiptAsync(bill.HeaderId);
            if (receipt == null)
            {
                ShowAlert(AppResources.Error, "Unable to load receipt data");
                return;
            }

            await _printService.PrintReceiptAsync(receipt, PrintFormat.Receipt);
        });
    }

    partial void OnDateFromChanged(DateTime value)
    {
        _ = LoadBillsAsync();
    }

    partial void OnDateToChanged(DateTime value)
    {
        _ = LoadBillsAsync();
    }
}
