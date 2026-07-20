using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Entities;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CarPlates.Mobile.ViewModels;

public partial class NewOrderViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ICustomerCarLookupService _customerCarLookupService;
    private readonly IWorkshopLookupService _workshopLookupService;
    private readonly ICustomerLookupService _customerLookupService;
    private readonly IItemLookupService _itemLookupService;

    // MakeName -> MakeID, so picking a brand can resolve the real ID needed to fetch models.
    private readonly Dictionary<string, int> _makeIdsByName = new();

    [ObservableProperty] private Vehicle _selectedVehicle = null!;
    [ObservableProperty] private Customer _selectedCustomer = null!;
    [ObservableProperty] private ObservableCollection<CartItem> _cartItems = new();
    [ObservableProperty] private WorkLocation _selectedLocation = null!;
    [ObservableProperty] private Technician _selectedTechnician = null!;
    [ObservableProperty] private string _orderNotes = string.Empty;
    [ObservableProperty] private string _signatureData = string.Empty;
    [ObservableProperty] private decimal _subTotal = 0;
    [ObservableProperty] private decimal _taxTotal = 0;
    [ObservableProperty] private decimal _total = 0;
    [ObservableProperty] private bool _isCustomerPopupVisible = false;
    [ObservableProperty] private bool _isVehiclePopupVisible = false;
    [ObservableProperty] private bool _isServicePopupVisible = false;
    [ObservableProperty] private bool _isLocationPopupVisible = false;
    [ObservableProperty] private bool _isTechnicianPopupVisible = false;
    [ObservableProperty] private bool _isSignaturePadVisible = false;
    [ObservableProperty] private string _searchPhoneNumber = string.Empty;
    [ObservableProperty] private string _newCustomerFirstName = string.Empty;
    [ObservableProperty] private string _newCustomerLastName = string.Empty;
    [ObservableProperty] private string _newCustomerPhone = string.Empty;
    [ObservableProperty] private string _newCustomerGender = "ذكر";
    [ObservableProperty] private string _newPlateNumber = string.Empty;
    [ObservableProperty] private string _newVin = string.Empty;
    [ObservableProperty] private string _selectedBrand = string.Empty;
    [ObservableProperty] private string _selectedModel = string.Empty;
    [ObservableProperty] private string _selectedVehicleType = string.Empty;
    [ObservableProperty] private string _selectedEngineType = string.Empty;
    [ObservableProperty] private int _newMileage = 0;
    [ObservableProperty] private int _newYear = 2025;
    [ObservableProperty] private string _selectedColor = "Blue";
    [ObservableProperty] private string _serviceSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<ServiceItem> _filteredServices = new();
    [ObservableProperty] private ServiceItem _newServiceItem = null!;
    [ObservableProperty] private string _newServiceName = string.Empty;
    [ObservableProperty] private string _newServiceCategory = "بانزين";
    [ObservableProperty] private string _newServiceType = "Product";
    [ObservableProperty] private decimal _newServicePrice = 0;
    [ObservableProperty] private decimal _newServiceCost = 0;
    [ObservableProperty] private bool _newServiceIsTaxable = true;
    [ObservableProperty] private string _newServiceTaxType = "VAT";
    [ObservableProperty] private decimal _newServiceTaxAmount = 0;
    [ObservableProperty] private decimal _newServiceTotalPrice = 0;

    // Loaded from the API instead of seeded locally.
    [ObservableProperty] private ObservableCollection<WorkLocation> _locations = new();
    [ObservableProperty] private ObservableCollection<Technician> _technicians = new();
    [ObservableProperty] private ObservableCollection<string> _availableModels = new();
    [ObservableProperty] private ObservableCollection<string> _brands = new();
    [ObservableProperty] private ObservableCollection<string> _vehicleTypes = new();
    [ObservableProperty] private ObservableCollection<string> _engineTypes = new();
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<ServiceItem> _serviceItems = new();
    [ObservableProperty] private ObservableCollection<ItemCategoryOption> _itemCategories = new();
    [ObservableProperty] private ItemCategoryOption? _selectedItemCategory;

    // No API source for a generic color list (wh_CustomerCars.Color is free text) or the
    // "add a custom service" category list, so these stay local.
    public ObservableCollection<string> Colors { get; } = new()
    {
        "Beige", "Black", "Blue", "Bronze", "Brown", "Gold", "Gray", "Green",
        "Orange", "Pink", "Purple", "Red", "Silver", "White", "Yellow"
    };

    public ObservableCollection<string> TaxTypes { get; } = new() { "VAT", "معفى من الضريبة" };
    public ObservableCollection<Vehicle> Vehicles { get; } = new();

    public ObservableCollection<int> VehicleYears { get; } = new();

    [ObservableProperty]
    private int selectedVehicleYear;

    public NewOrderViewModel(
        INavigationService navigation,
        ICustomerCarLookupService customerCarLookupService,
        IWorkshopLookupService workshopLookupService,
        ICustomerLookupService customerLookupService,
        IItemLookupService itemLookupService) : base(navigation)
    {
        _customerCarLookupService = customerCarLookupService;
        _workshopLookupService = workshopLookupService;
        _customerLookupService = customerLookupService;
        _itemLookupService = itemLookupService;

        Title = "إضافة سيارة جديدة";
        LoadVehicleYears();
        _ = LoadInitialDataAsync();
    }

    private void LoadVehicleYears()
    {
        VehicleYears.Clear();

        var currentYear = DateTime.Now.Year;

        for (int year = currentYear; year >= 1950; year--)
        {
            VehicleYears.Add(year);
        }

        SelectedVehicleYear = currentYear;
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

    // Pulls every reference list this form needs straight from the API: car makes,
    // vehicle/engine types, workshop technicians and locations, item categories, and an
    // unfiltered first page of the item catalog - nothing here is hardcoded any more.
    [RelayCommand]
    private async Task LoadInitialDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var makesTask = _customerCarLookupService.GetMakesAsync();
            var vehicleTypesTask = _customerCarLookupService.GetVehicleTypesAsync();
            var engineTypesTask = _customerCarLookupService.GetEngineTypesAsync();
            var techniciansTask = _workshopLookupService.GetTechniciansAsync(pageSize: 100);
            var locationsTask = _workshopLookupService.GetWorkLocationsAsync(pageSize: 100);
            var categoriesTask = _itemLookupService.GetCategoriesAsync(pageSize: 100);
            var itemsTask = _itemLookupService.SearchAsync(pageSize: 50);

            await Task.WhenAll(makesTask, vehicleTypesTask, engineTypesTask, techniciansTask, locationsTask, categoriesTask, itemsTask);

            _makeIdsByName.Clear();
            Brands.Clear();
            foreach (var make in makesTask.Result)
            {
                Brands.Add(make.MakeName);
                _makeIdsByName[make.MakeName] = make.MakeID;
            }

            VehicleTypes.Clear();
            foreach (var type in vehicleTypesTask.Result)
            {
                VehicleTypes.Add(type.Name_En ?? type.Name_Ar ?? string.Empty);
            }

            EngineTypes.Clear();
            foreach (var type in engineTypesTask.Result)
            {
                EngineTypes.Add(type.Name_En ?? type.Name_Ar ?? string.Empty);
            }

            Technicians.Clear();
            foreach (var tech in techniciansTask.Result.Items)
            {
                Technicians.Add(new Technician { Id = tech.Id.ToString(), Name = tech.Name_En ?? tech.Name_Ar ?? string.Empty });
            }

            Locations.Clear();
            foreach (var location in locationsTask.Result.Items)
            {
                Locations.Add(new WorkLocation { Id = location.Id.ToString(), Name = location.Name_En ?? location.Name_Ar ?? string.Empty, Type = location.Name_Ar ?? string.Empty });
            }

            ItemCategories.Clear();
            ItemCategories.Add(new ItemCategoryOption(null, "الكل")); // "All" - clears the category filter
            foreach (var category in categoriesTask.Result.Items)
            {
                ItemCategories.Add(new ItemCategoryOption(category.Id, category.Name_En ?? category.Name_Ar ?? string.Empty));
            }

            ApplyItemResults(itemsTask.Result.Items);
        });
    }

    partial void OnSelectedBrandChanged(string value)
    {
        _ = UpdateModelsForBrandAsync(value);
    }

    partial void OnSelectedItemCategoryChanged(ItemCategoryOption? value)
    {
        _ = FilterServicesAsync();
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

    private async Task UpdateModelsForBrandAsync(string brand)
    {
        AvailableModels.Clear();

        if (string.IsNullOrWhiteSpace(brand) || !_makeIdsByName.TryGetValue(brand, out var makeId))
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var models = await _customerCarLookupService.GetModelsAsync(makeId);
            foreach (var model in models)
            {
                AvailableModels.Add(model.ModelName);
            }
        });
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

    // Searches wh_Customers by mobile/phone/name via the API instead of filtering a
    // hardcoded local list. Multiple matches are left in Customers for the popup to list;
    // an exact phone match is selected immediately.
    [RelayCommand]
    private async Task SearchCustomerAsync()
    {
        await ExecuteAsync(async () =>
        {
            var results = await _customerLookupService.SearchAsync(SearchPhoneNumber, pageSize: 20);

            Customers.Clear();
            foreach (var c in results.Items)
            {
                Customers.Add(new Customer
                {
                    FirstName = c.Name_En,
                    LastName = string.Empty,
                    PhoneNumber = c.Mobile ?? c.Phone1 ?? string.Empty,
                    Gender = string.Empty
                });
            }

            var exactMatch = Customers.FirstOrDefault(c => c.PhoneNumber == SearchPhoneNumber);
            if (exactMatch != null)
            {
                SelectedCustomer = exactMatch;
                IsCustomerPopupVisible = false;
            }
            else
            {
                // No exact match on the server either - offer the new-customer form.
                NewCustomerPhone = SearchPhoneNumber;
            }
        });
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
        if (SelectedCustomer == null)
        {
            // Consistent with SubmitOrder validation: surface an error instead of throwing
            ErrorMessage = "الرجاء اختيار عميل";
            HasError = true;
            return;
        }

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid().ToString(),
            PlateNumber = NewPlateNumber,
            Vin = NewVin,
            Brand = SelectedBrand,
            Model = SelectedModel,
            VehicleType = SelectedVehicleType,
            EngineType = SelectedEngineType,
            Mileage = NewMileage,
            Year = NewYear,
            Color = SelectedColor,
            CustomerId = SelectedCustomer.Id
        };

        Vehicles.Add(vehicle);
        SelectedVehicle = vehicle;
        IsVehiclePopupVisible = false;
        ClearNewVehicleFields();
    }

    [RelayCommand]
    private void ShowServicePopup()
    {
        IsServicePopupVisible = true;
        _ = FilterServicesAsync();
    }

    [RelayCommand]
    private void CloseServicePopup()
    {
        IsServicePopupVisible = false;
    }

    // Searches the real item catalog (vw_wh_ItemBarCodes) by name/barcode, optionally
    // narrowed to the selected category (vw_wh_ItemSubGroups), instead of filtering an
    // in-memory list.
    [RelayCommand]
    private async Task FilterServicesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var categoryId = SelectedItemCategory?.Id;
            var search = string.IsNullOrWhiteSpace(ServiceSearchText) ? null : ServiceSearchText;

            var results = await _itemLookupService.SearchAsync(search, categoryId, pageSize: 50);
            ApplyItemResults(results.Items);
        });
    }

    private void ApplyItemResults(IReadOnlyList<ItemLookupResult> items)
    {
        var mapped = items.Select(ToServiceItem).ToList();
        ServiceItems = new ObservableCollection<ServiceItem>(mapped);
        FilteredServices = new ObservableCollection<ServiceItem>(mapped);
    }

    private static ServiceItem ToServiceItem(ItemLookupResult item)
    {
        var price = (decimal)(item.PackagePrice ?? 0);
        var taxRate = (decimal)(item.ItemTax ?? 0) / 100m;
        var taxAmount = price * taxRate;

        return new ServiceItem
        {
            Id = item.ItemBarCode,
            Name = item.Name_En ?? item.Name_Ar ?? item.ItemBarCode,
            Category = item.ItemGroupName_En ?? item.ItemGroupName_Ar ?? string.Empty,
            ItemType = "Product",
            Price = price,
            Cost = 0,
            IsTaxable = taxRate > 0,
            TaxType = "VAT",
            TaxAmount = taxAmount,
            TotalPrice = price + taxAmount
        };
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
        SignatureData = null!;
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
            Id = Guid.NewGuid().ToString(),
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
            "فحص" => "",
            "الميكانيك" => "",
            "بانزين" => "",
            "الكهربا" => "",
            "قطع غيار" => "",
            "السمكرة و البويا" => "",
            "زيوت المحرك" => "",
            "البطاريات" => "",
            _ => ""
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
        SelectedBrand = null!;
        SelectedModel = null!;
        SelectedVehicleType = null!;
        SelectedEngineType = null!;
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

// Wraps a category for the item-search filter picker; Id is null for the "All" entry.
public record ItemCategoryOption(int? Id, string Name);
