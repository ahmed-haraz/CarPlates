using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.History.Queries;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private List<ScanRecordListDto> _scanRecords = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private int _totalCount;

    public HistoryViewModel(IMediator mediator, INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        Title = AppResources.History;
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        await ExecuteAsync(async () =>
        {
            var query = new GetScanHistoryQuery(
                SearchQuery,
                Page: 1,
                PageSize: 50);

            var result = await _mediator.Send(query);
            if (result != null)
            {
                ScanRecords = result.Items.ToList();
                TotalCount = result.TotalCount;
            }
        });
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadHistoryAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(ScanRecordListDto record)
    {
        await Navigation.PushAsync<VehicleDetailsViewModel>(new Dictionary<string, object>
        {
            ["plateNumber"] = record.PlateNumber
        });
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        // Export to CSV/JSON
        await Navigation.DisplayAlertAsync(AppResources.Export, AppResources.ExportComingSoon);
    }

    [RelayCommand]
    private async Task FilterByDateAsync()
    {
        // Show date picker and filter
        await Navigation.DisplayAlertAsync(AppResources.Search, AppResources.DateFilterComingSoon);
    }
}
