using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class CashierViewModel : BaseViewModel
{
    private readonly IBillApiService _billApiService;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _canGoToPreviousPage;
    [ObservableProperty] private bool _canGoToNextPage;
    [ObservableProperty] private ObservableCollection<BillApiItem> _bills = new();
    [ObservableProperty] private string? _dateFromText;
    [ObservableProperty] private string? _dateToText;

    private const int PageSize = 20;

    public CashierViewModel(INavigationService navigation, IBillApiService billApiService) : base(navigation)
    {
        _billApiService = billApiService;
        Title = AppResources.Cashier;
    }

    [RelayCommand]
    private async Task LoadBillsAsync()
    {
        await ExecuteAsync(async () =>
        {
            int? dateFrom = ParseDate(DateFromText);
            int? dateTo = ParseDate(DateToText);

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
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadBillsAsync();
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

    private static int? ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParse(text, out var dt))
            return int.Parse(dt.ToString("yyyyMMdd"));
        return null;
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchAsync();
    }
}
