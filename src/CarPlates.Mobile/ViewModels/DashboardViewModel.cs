using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Dashboard.Queries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IAuthenticationService _authService;
    private readonly IScanRepository _scanRepository;

    [ObservableProperty]
    private DashboardStatisticsDto _statistics = new(0, 0, 0, 0, 0);

    [ObservableProperty]
    private List<RecentScanDto> _recentScans = new();

    [ObservableProperty]
    private string _userName = "User";

    public DashboardViewModel(IMediator mediator, IAuthenticationService authService, IScanRepository scanRepository)
    {
        _mediator = mediator;
        _authService = authService;
        _scanRepository = scanRepository;
        Title = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load statistics
            var statsQuery = new GetDashboardStatisticsQuery();
            Statistics = await _mediator.Send(statsQuery) ?? new DashboardStatisticsDto(0, 0, 0, 0, 0);

            // Load recent scans
            var recentQuery = new GetRecentScansQuery(5);
            var recent = await _mediator.Send(recentQuery);
            RecentScans = recent?.ToList() ?? new List<RecentScanDto>();

            // Load user info
            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                UserName = user.FullName;
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToScanAsync()
    {
        await Shell.Current.GoToAsync("//scanner");
    }

    [RelayCommand]
    private async Task NavigateToHistoryAsync()
    {
        await Shell.Current.GoToAsync("//history");
    }

    [RelayCommand]
    private async Task ViewVehicleDetailsAsync(RecentScanDto scan)
    {
        await Shell.Current.GoToAsync($"vehicle?plateNumber={scan.PlateNumber}");
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await Shell.Current.GoToAsync("profile");
    }
}
