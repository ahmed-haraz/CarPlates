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
    private readonly IBillApiService _billApiService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private DashboardStatisticsDto _statistics = new(0, 0, 0, 0, 0, 0,0,0);

    [ObservableProperty]
    private List<RecentScanDto> _recentScans = new();

    [ObservableProperty]
    private string _userName = "User";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private int _todayBills;

    [ObservableProperty]
    private double _todaySalesTotal;

    [ObservableProperty]
    private string? _logoUrl;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);

    partial void OnLogoUrlChanged(string? value) => OnPropertyChanged(nameof(HasLogo));

    public DashboardViewModel(IMediator mediator, IAuthenticationService authService, INavigationService navigation, IBillApiService billApiService, ISettingsService settingsService) : base(navigation)
    {
        _mediator = mediator;
        _authService = authService;
        _billApiService = billApiService;
        _settingsService = settingsService;
        Title = AppResources.Dashboard;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            await ExecuteAsync(async () =>
            {
                // Load the company logo saved at login.
                var logoUrl = await _settingsService.GetCompanyLogoUrlAsync();
                LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl;

                // Load statistics
                var statsQuery = new GetDashboardStatisticsQuery();
                Statistics = await _mediator.Send(statsQuery) ?? new DashboardStatisticsDto(0, 0, 0, 0, 0, 0,0,0);

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

                // Load today's bill stats
                var billStats = await _billApiService.GetTodayStatsAsync();
                if (billStats.Success)
                {
                    TodayBills = billStats.TodayBills;
                    TodaySalesTotal = billStats.TodayTotal;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            IsRefreshing = false;
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
