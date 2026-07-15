using CarPlates.Domain.Entities;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CarPlates.Mobile.ViewModels;

public partial class CashierViewModel : BaseViewModel
{
    [ObservableProperty] private ObservableCollection<Order> _orders = new();
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private string _selectedFilter = "الكل";
    [ObservableProperty] private ObservableCollection<string> _filters = new()
    {
        "الكل", "غير معين", "تم التعيين", "قيد الخدمة", "ملغاة"
    };
    [ObservableProperty] private ObservableCollection<Order> _filteredOrders = new();

    public CashierViewModel(INavigationService navigation) : base(navigation)
    {
        Title = "الكاشير";
        LoadOrders();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilters();
    }

    private void LoadOrders()
    {
        Orders = new ObservableCollection<Order>(AppData.Orders);
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var query = Orders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(o => 
                o.Vehicle?.PlateNumber?.Contains(SearchText) == true ||
                o.Vehicle?.Brand?.Contains(SearchText) == true ||
                o.Customer?.FirstName?.Contains(SearchText) == true);
        }

        if (SelectedFilter != "الكل")
        {
            query = query.Where(o => o.Vehicle?.Status == SelectedFilter || o.Status == SelectedFilter);
        }

        FilteredOrders = new ObservableCollection<Order>(query);
    }

    [RelayCommand]
    private async Task NewOrder()
    {
        await Shell.Current.GoToAsync("NewOrderPage");
    }

    [RelayCommand]
    private async Task ViewOrderDetails(Order order)
    {
        if (order == null) return;
        var navigationParameter = new Dictionary<string, object>
        {
            { "Order", order }
        };
        await Shell.Current.GoToAsync("OrderDetailsPage", navigationParameter);
    }
}
