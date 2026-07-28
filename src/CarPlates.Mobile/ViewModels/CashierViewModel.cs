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
    [ObservableProperty] private bool _showAllBills = true;
    [ObservableProperty] private bool _showPaidBills = false;
    [ObservableProperty] private bool _showUnpaidBills = false;

    public enum BillStatus { All, Paid, Unpaid }

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
                var items = $"Bill #{bill.HeaderId}\nDate: {bill.TransDate}\nCustomer: {bill.CustomerName ?? "N/A"}\nPlate: {bill.PlateNumber ?? "N/A"}\nTotal: {bill.NetTotal:N2}\nPaid: {bill.Paid:N2}\nBalance: {bill.Balance:N2}";
                await Navigation.DisplayAlertAsync("Bill Details", items);
                return;
            }

            var details = string.Join("\n",
                receipt.Details.Select(d => $"  {d.ItemBarCode,-20} {d.Qty,5:F0} x {d.Price,8:F2}"));

            var message = $"Receipt: {receipt.ReceiptNo ?? "N/A"}\n" +
                          $"Bill #{receipt.HeaderId}\n" +
                          $"Date: {receipt.TransDate}\n" +
                          $"Customer: {receipt.CustomerName ?? "N/A"}\n" +
                           $"Plate: {receipt.PlateNumber ?? "N/A"}\n" +
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
        paymentVm.LoadBill(bill.HeaderId, bill.DocTransNo, bill.CustomerName, bill.PlateNumber,
            bill.Total, bill.NetTotal, bill.Paid, bill.Balance, bill.TransDate ?? 0);
        var page = new Views.Actions.PaymentPage(paymentVm);
        await Navigation.PushPageAsync(page);
    }

    [RelayCommand]
    private async Task PrintBillAsync(BillApiItem bill)
    {
        try
        {
            IsBusy = true;
            var detail = await _billApiService.GetBillByIdAsync(bill.HeaderId);
            if (detail == null)
            {
                await Navigation.DisplayAlertAsync(AppResources.Error, "Unable to load bill details");
                return;
            }

            var receipt = new ReceiptApiResult(
                ReceiptNo: null,
                HeaderId: detail.HeaderId,
                DocTransNo: detail.DocTransNo,
                TransDate: detail.TransDate,
                CustomerName: detail.CustomerName,
                ReferenceNo: detail.ReferenceNo,
                PlateNumber: detail.PlateNumber,
                Total: detail.Total,
                NetTotal: detail.NetTotal,
                Paid: detail.Paid,
                Balance: detail.Balance,
                PayType: detail.PayType,
                Payments: [],
                Details: detail.Details.Select(d => new BillDetailApiItem(
                    d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                    d.DetailDiscount1, d.DetailDiscount2, d.DetailDiscountR1, d.DetailDiscountR2,
                    d.DetailTax, d.DetailTaxR, d.Value)).ToList());

            // 1. Show preview formatted exactly as the printed receipt
            var preview = BuildPrintPreview(detail);
            await Navigation.DisplayAlertAsync("Print Preview", preview);
            IsBusy = false;

            // 2. Pick printer
            var printers = (await _printService.GetAvailablePrintersAsync()).ToList();

            if (printers.Count == 0)
            {
                var noBt = await Navigation.DisplayActionSheetAsync(
                    "No Bluetooth printers found. Print as A4?", "Cancel", null, "Print A4");
                if (noBt == "Print A4")
                    await _printService.PrintReceiptAsync(receipt, null, PrintFormat.A4);
                else
                    return;
            }
            else
            {
                var options = printers.Cast<string>().ToList();
                options.Add("Print A4");
                var selected = await Navigation.DisplayActionSheetAsync(
                    "Select Printer", "Cancel", null, [.. options]);

                if (selected == "Cancel" || selected == null)
                    return;

                if (selected == "Print A4")
                {
                    await _printService.PrintReceiptAsync(receipt, null, PrintFormat.A4);
                }
                else
                {
                    await _printService.PrintReceiptAsync(receipt, selected, PrintFormat.Receipt);
                }
            }

            await Navigation.DisplayAlertAsync("Print", "Receipt sent to printer");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PrintBill] {ex}");
            await Navigation.DisplayAlertAsync("Print Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildPrintPreview(BillDetailResult detail)
    {
        var items = string.Join("\n",
            detail.Details.Select(d =>
                $"  {d.ItemBarCode,-20} {d.Qty,5:F0} {d.Price,8:F2}"));

        var preview = $"ARKAN MAINTENANCE\n" +
                      $"{new string('-', 32)}\n" +
                      $"Receipt: {detail.DocTransNo ?? "N/A"}\n" +
                      $"Date: {detail.TransDate}\n" +
                      $"Customer: {detail.CustomerName ?? "N/A"}\n" +
                       $"Plate: {detail.PlateNumber ?? "N/A"}\n" +
                      $"{new string('-', 32)}\n" +
                      $"  {"Item",-20} {"Qty",5} {"Price",8}\n" +
                      $"{items}\n" +
                      $"{new string('-', 32)}\n" +
                      $"Total:     {detail.Total,10:F2}\n" +
                      $"Paid:      {detail.Paid,10:F2}\n" +
                      $"Balance:   {detail.Balance,10:F2}\n" +
                      $"Thank you for your visit!";

        return preview;
    }


    partial void OnDateFromChanged(DateTime value)
    {
        _ = LoadBillsAsync();
    }

    partial void OnDateToChanged(DateTime value)
    {
        _ = LoadBillsAsync();
    }

    partial void OnShowAllBillsChanged(bool value)
    {
        ShowPaidBills = false;
        ShowUnpaidBills = false;
        _ = LoadBillsAsync();
    }

    partial void OnShowPaidBillsChanged(bool value)
    {
        ShowAllBills = false;
        ShowUnpaidBills = false;
        _ = LoadBillsAsync();
    }

    partial void OnShowUnpaidBillsChanged(bool value)
    {
        ShowAllBills = false;
        ShowPaidBills = false;
        _ = LoadBillsAsync();
    }

    private async Task FilterBillsAsync(BillStatus status)
    {
        var result = await _billApiService.SearchBillsAsync(
            SearchText,
            DateFrom != DateTime.MinValue ? int.Parse(DateFrom.ToString("yyyyMMdd")) : null,
            DateTo != DateTime.MinValue ? int.Parse(DateTo.ToString("yyyyMMdd")) : null,
            CurrentPage,
            PageSize);

        if (!result.Success)
        {
            ShowAlert(AppResources.Error, result.ErrorMessage ?? "Failed to load bills");
            return;
        }

        var filteredBills = result.Items;

        switch (status)
        {
            case BillStatus.Paid:
                filteredBills = result.Items.Where(b => b.Paid > 0).ToList();
                break;
            case BillStatus.Unpaid:
                filteredBills = result.Items.Where(b => b.Balance > 0).ToList();
                break;
            case BillStatus.All:
            default:
                break;
        }

        Bills = new ObservableCollection<BillApiItem>(filteredBills);
        TotalPages = Math.Max(result.TotalPages, 1);
        CanGoToPreviousPage = CurrentPage > 1;
        CanGoToNextPage = CurrentPage < TotalPages;
    }
}