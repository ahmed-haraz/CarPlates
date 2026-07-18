using CarPlates.Domain.Entities;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class NewOrderViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty] private Vehicle _selectedVehicle;
    [ObservableProperty] private Customer _selectedCustomer;
    [ObservableProperty] private ObservableCollection<CartItem> _cartItems = new();
    [ObservableProperty] private WorkLocation _selectedLocation;
    [ObservableProperty] private Technician _selectedTechnician;
    [ObservableProperty] private string _orderNotes;
    [ObservableProperty] private string _signatureData;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _taxTotal;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private bool _isCustomerPopupVisible;
    [ObservableProperty] private bool _isVehiclePopupVisible;
    [ObservableProperty] private bool _isServicePopupVisible;
    [ObservableProperty] private bool _isLocationPopupVisible;
    [ObservableProperty] private bool _isTechnicianPopupVisible;
    [ObservableProperty] private bool _isSignaturePadVisible;
    [ObservableProperty] private string _searchPhoneNumber;
    [ObservableProperty] private string _newCustomerFirstName;
    [ObservableProperty] private string _newCustomerLastName;
    [ObservableProperty] private string _newCustomerPhone;
    [ObservableProperty] private string _newCustomerGender = "ذكر";
    [ObservableProperty] private string _newPlateNumber;
    [ObservableProperty] private string _newVin;
    [ObservableProperty] private string _selectedBrand;
    [ObservableProperty] private string _selectedModel;
    [ObservableProperty] private string _selectedVehicleType;
    [ObservableProperty] private string _selectedEngineType;
    [ObservableProperty] private int _newMileage;
    [ObservableProperty] private int _newYear = 2025;
    [ObservableProperty] private string _selectedColor = "Blue";
    [ObservableProperty] private string _serviceSearchText;
    [ObservableProperty] private ObservableCollection<ServiceItem> _filteredServices = new();
    [ObservableProperty] private ServiceItem _newServiceItem;
    [ObservableProperty] private string _newServiceName;
    [ObservableProperty] private string _newServiceCategory = "بانزين";
    [ObservableProperty] private string _newServiceType = "Product";
    [ObservableProperty] private decimal _newServicePrice;
    [ObservableProperty] private decimal _newServiceCost;
    [ObservableProperty] private bool _newServiceIsTaxable = true;
    [ObservableProperty] private string _newServiceTaxType = "VAT";
    [ObservableProperty] private decimal _newServiceTaxAmount;
    [ObservableProperty] private decimal _newServiceTotalPrice;
    [ObservableProperty] private ObservableCollection<WorkLocation> _locations = new();
    [ObservableProperty] private ObservableCollection<Technician> _technicians = new();
    [ObservableProperty] private ObservableCollection<string> _availableModels = new();

    public ObservableCollection<string> Brands => AppData.VehicleBrands;
    public ObservableCollection<string> VehicleTypes => AppData.VehicleTypes;
    public ObservableCollection<string> EngineTypes => AppData.EngineTypes;
    public ObservableCollection<string> Colors => AppData.Colors;
    public ObservableCollection<string> Categories => AppData.ServiceCategories;
    public ObservableCollection<string> TaxTypes { get; } = new() { "VAT", "معفى من الضريبة" };
    public ObservableCollection<Vehicle> Vehicles => AppData.Vehicles;
    public ObservableCollection<Customer> Customers => AppData.Customers;
    public ObservableCollection<ServiceItem> ServiceItems => AppData.ServiceItems;

    public NewOrderViewModel(INavigationService navigation) : base(navigation)
    {
        Title = "إضافة سيارة جديدة";
        LoadInitialData();
    }

    // Replaces Shell's [QueryProperty]/routing-based parameter passing. When the scanner
    // couldn't find a vehicle for the detected plate, it navigates here with the plate
    // number already known so the user doesn't have to retype it in the "Add Vehicle" form.
    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("plateNumber", out var value) && value is string plate && !string.IsNullOrWhiteSpace(plate))
        {
            NewPlateNumber = plate;
        }
    }

    private void LoadInitialData()
    {
        Locations.Add(new WorkLocation { Id = "1", Name = "كراج", Type = "كراج" });
        Locations.Add(new WorkLocation { Id = "2", Name = "محل", Type = "محل" });
        Technicians.Add(new Technician { Id = "1", Name = "bakr ibrahim" });
        FilteredServices = new ObservableCollection<ServiceItem>(ServiceItems);
    }

    partial void OnSelectedBrandChanged(string value)
    {
        UpdateModelsForBrand(value);
    }

    partial void OnNewServicePriceChanged(decimal value)
    {
        CalculateNewServiceTotals();
    }

    partial void OnNewServiceIsTaxableChanged(bool value)
    {
        CalculateNewServiceTotals();
    }

    private void CalculateNewServiceTotals()
    {
        if (NewServiceIsTaxable)
        {
            NewServiceTaxAmount = NewServicePrice * 0.15m; // 15% VAT
        }
        else
        {
            NewServiceTaxAmount = 0;
        }
        NewServiceTotalPrice = NewServicePrice + NewServiceTaxAmount;
    }

    private void UpdateModelsForBrand(string brand)
    {
        AvailableModels.Clear();
        if (brand == "بي ام دبليو")
        {
            AvailableModels.Add("1 - Series");
            AvailableModels.Add("2 - Series");
            AvailableModels.Add("3 - Series");
            AvailableModels.Add("4 - Series");
            AvailableModels.Add("5 - Series");
            AvailableModels.Add("218 اي");
            AvailableModels.Add("320 اي");
            AvailableModels.Add("318 اي");
            AvailableModels.Add("335 اي");
            AvailableModels.Add("330 اي");
            AvailableModels.Add("440i");
            AvailableModels.Add("420 دي");
            AvailableModels.Add("523 اي");
            AvailableModels.Add("520i");
            AvailableModels.Add("520");
        }
        else
        {
            AvailableModels.Add("Standard");
        }
    }

    [RelayCommand]
    private void ShowCustomerPopup()
    {
        IsCustomerPopupVisible = true;
    }

    [RelayCommand]
    private void CloseCustomerPopup()
    {
        IsCustomerPopupVisible = false;
    }

    [RelayCommand]
    private void SearchCustomer()
    {
        var customer = Customers.FirstOrDefault(c => c.PhoneNumber == SearchPhoneNumber);
        if (customer != null)
        {
            SelectedCustomer = customer;
            IsCustomerPopupVisible = false;
        }
        else
        {
            // Show new customer form within popup
            NewCustomerPhone = SearchPhoneNumber;
        }
    }

    [RelayCommand]
    private void SaveNewCustomer()
    {
        var customer = new Customer
        {
            FirstName = NewCustomerFirstName,
            LastName = NewCustomerLastName,
            PhoneNumber = NewCustomerPhone,
            Gender = NewCustomerGender
        };
        Customers.Add(customer);
        SelectedCustomer = customer;
        IsCustomerPopupVisible = false;
        ClearNewCustomerFields();
    }

    [RelayCommand]
    private void ShowVehiclePopup()
    {
        IsVehiclePopupVisible = true;
    }

    [RelayCommand]
    private void CloseVehiclePopup()
    {
        IsVehiclePopupVisible = false;
    }

    [RelayCommand]
    private void SaveNewVehicle()
    {
        var vehicle = new Vehicle
        {
            Id = System.Guid.NewGuid().ToString(),
            PlateNumber = NewPlateNumber,
            Vin = NewVin,
            Brand = SelectedBrand,
            Model = SelectedModel,
            VehicleType = SelectedVehicleType,
            EngineType = SelectedEngineType,
            Mileage = NewMileage,
            Year = NewYear,
            Color = SelectedColor,
            CustomerId = SelectedCustomer?.PhoneNumber
        };
        AppData.Vehicles.Add(vehicle);
        SelectedVehicle = vehicle;
        IsVehiclePopupVisible = false;
        ClearNewVehicleFields();
    }

    [RelayCommand]
    private void ShowServicePopup()
    {
        IsServicePopupVisible = true;
        FilterServices();
    }

    [RelayCommand]
    private void CloseServicePopup()
    {
        IsServicePopupVisible = false;
    }

    [RelayCommand]
    private void FilterServices()
    {
        if (string.IsNullOrWhiteSpace(ServiceSearchText))
        {
            FilteredServices = new ObservableCollection<ServiceItem>(ServiceItems);
        }
        else
        {
            var filtered = ServiceItems.Where(s => s.Name.Contains(ServiceSearchText) || s.Category.Contains(ServiceSearchText));
            FilteredServices = new ObservableCollection<ServiceItem>(filtered);
        }
    }

    [RelayCommand]
    private void AddServiceToCart(ServiceItem item)
    {
        if (item == null) return;
        var existing = CartItems.FirstOrDefault(c => c.ServiceItem.Id == item.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            CartItems.Add(new CartItem { ServiceItem = item, Quantity = 1 });
        }
        RecalculateTotals();
        IsServicePopupVisible = false;
    }

    [RelayCommand]
    private void RemoveCartItem(CartItem item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private void IncrementQuantity(CartItem item)
    {
        if (item == null) return;
        item.Quantity++;
        RecalculateTotals();
    }

    [RelayCommand]
    private void DecrementQuantity(CartItem item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity--;
            RecalculateTotals();
        }
    }

    [RelayCommand]
    private void ShowNewServiceForm()
    {
        NewServiceItem = new ServiceItem();
    }

    [RelayCommand]
    private void SaveNewService()
    {
        var item = new ServiceItem
        {
            Id = System.Guid.NewGuid().ToString(),
            Name = NewServiceName,
            Category = NewServiceCategory,
            ItemType = NewServiceType,
            Price = NewServicePrice,
            Cost = NewServiceCost,
            IsTaxable = NewServiceIsTaxable,
            TaxType = NewServiceTaxType,
            TaxAmount = NewServiceTaxAmount,
            TotalPrice = NewServiceTotalPrice,
            Icon = GetIconForCategory(NewServiceCategory)
        };
        ServiceItems.Add(item);
        FilteredServices.Add(item);
        ClearNewServiceFields();
    }

    [RelayCommand]
    private void ShowLocationPopup()
    {
        IsLocationPopupVisible = true;
    }

    [RelayCommand]
    private void SelectLocation(WorkLocation location)
    {
        SelectedLocation = location;
        IsLocationPopupVisible = false;
    }

    [RelayCommand]
    private void ShowTechnicianPopup()
    {
        IsTechnicianPopupVisible = true;
    }

    [RelayCommand]
    private void SelectTechnician(Technician tech)
    {
        SelectedTechnician = tech;
        IsTechnicianPopupVisible = false;
    }

    [RelayCommand]
    private void ShowSignaturePad()
    {
        IsSignaturePadVisible = true;
    }

    [RelayCommand]
    private void SaveSignature()
    {
        IsSignaturePadVisible = false;
    }

    [RelayCommand]
    private void ClearSignature()
    {
        SignatureData = null;
    }

    [RelayCommand]
    private async Task SubmitOrder()
    {
        if (SelectedVehicle == null || CartItems.Count == 0)
        {
            ErrorMessage = "الرجاء إكمال جميع البيانات المطلوبة";
            HasError = true;
            return;
        }

        var order = new Order
        {
            Id = System.Guid.NewGuid().ToString(),
            Vehicle = SelectedVehicle,
            Customer = SelectedCustomer,
            Items = new ObservableCollection<CartItem>(CartItems),
            Location = SelectedLocation,
            Technician = SelectedTechnician,
            Notes = OrderNotes,
            Signature = SignatureData,
            Status = "ملغاة"
        };
        AppData.Orders.Add(order);
        await Shell.Current.GoToAsync("//CashierPage");
    }

    private void RecalculateTotals()
    {
        SubTotal = CartItems.Sum(c => c.ServiceItem.Price * c.Quantity);
        TaxTotal = CartItems.Sum(c => c.ServiceItem.TaxAmount * c.Quantity);
        Total = CartItems.Sum(c => c.LineTotal);
    }

    private string GetIconForCategory(string category)
    {
        return category switch
        {
            "فحص" => "",
            "الميكانيك" => "",
            "بانزين" => "",
            "الكهربا" => "",
            "قطع غيار" => "",
            "السمكرة و البويا" => "",
            "زيوت المحرك" => "",
            "البطاريات" => "",
            _ => ""
        };
    }

    private void ClearNewCustomerFields()
    {
        NewCustomerFirstName = string.Empty;
        NewCustomerLastName = string.Empty;
        NewCustomerPhone = string.Empty;
        NewCustomerGender = "ذكر";
    }

    private void ClearNewVehicleFields()
    {
        NewPlateNumber = string.Empty;
        NewVin = string.Empty;
        SelectedBrand = null;
        SelectedModel = null;
        SelectedVehicleType = null;
        SelectedEngineType = null;
        NewMileage = 0;
        NewYear = 2025;
        SelectedColor = "Blue";
    }

    private void ClearNewServiceFields()
    {
        NewServiceName = string.Empty;
        NewServiceCategory = "بانزين";
        NewServiceType = "Product";
        NewServicePrice = 0;
        NewServiceCost = 0;
        NewServiceIsTaxable = true;
        NewServiceTaxAmount = 0;
        NewServiceTotalPrice = 0;
    }
}
