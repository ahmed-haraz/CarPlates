using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.History.Queries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IScanRepository _scanRepository;

    [ObservableProperty]
    private List<ScanRecordListDto> _scanRecords = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private int _totalCount;

    public HistoryViewModel(IMediator mediator, IScanRepository scanRepository)
    {
        _mediator = mediator;
        _scanRepository = scanRepository;
        Title = "History";
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
        await Shell.Current.GoToAsync($"vehicle?plateNumber={record.PlateNumber}");
    }

    [RelayCommand]
    private async Task DeleteAsync(ScanRecordListDto record)
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Delete Record",
            $"Delete scan for {record.PlateNumber}?",
            "Delete", "Cancel");

        if (confirm)
        {
            await _scanRepository.DeleteAsync(record.Id);
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        // Export to CSV/JSON
        await Shell.Current.DisplayAlert("Export", "Export feature coming soon", "OK");
    }

    [RelayCommand]
    private async Task FilterByDateAsync()
    {
        // Show date picker and filter
        await Shell.Current.DisplayAlert("Filter", "Date filter coming soon", "OK");
    }
}
