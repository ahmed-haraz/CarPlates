using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Dashboard.Queries;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private DashboardStatisticsDto _statistics = new(0, 0, 0, 0, 0, 0, 0, 0);

    [ObservableProperty]
    private List<RecentScanDto> _recentScans = [];

    [ObservableProperty]
    private string _userName = "User";

    public DashboardViewModel(IMediator mediator, IAuthenticationService authService, INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _authService = authService;
        Title = AppResources.Dashboard;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            await ExecuteAsync(async () =>
            {
                // Load statistics
                var statsQuery = new GetDashboardStatisticsQuery();
                Statistics = await _mediator.Send(statsQuery) ?? new DashboardStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0);

                // Load recent scans
                var recentQuery = new GetRecentScansQuery(5);
                var recent = await _mediator.Send(recentQuery);
                RecentScans = recent?.ToList() ?? [];

                // Load user info
                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    UserName = user.Username;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    [RelayCommand]
    private async Task NavigateToScanAsync()
    {
        await Navigation.SwitchTabAsync(MainTab.Scanner);
    }

    [RelayCommand]
    private async Task NavigateToHistoryAsync()
    {
        await Navigation.SwitchTabAsync(MainTab.History);
    }

    [RelayCommand]
    private async Task ViewVehicleDetailsAsync(RecentScanDto scan)
    {
        await Navigation.PushAsync<VehicleDetailsViewModel>(new Dictionary<string, object>
        {
            ["plateNumber"] = scan.PlateNumber
        });
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await Navigation.PushAsync<ProfileViewModel>();
    }
}
