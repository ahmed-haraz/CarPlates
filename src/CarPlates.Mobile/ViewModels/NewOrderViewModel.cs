using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Entities;
using CarPlates.Mobile.Localization;
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
    private readonly IBillApiService _billApiService;

    // MakeName -> MakeID, so picking a brand can resolve the real ID needed to fetch models.
    private readonly Dictionary<string, int> _makeIdsByName = new();

    [ObservableProperty] private Vehicle _selectedVehicle = null!;
    [ObservableProperty] private Customer _selectedCustomer = null!;
    [ObservableProperty] private ObservableCollection<CartItem> _cartItems = new();
    [ObservableProperty] private WorkLocation _selectedLocation = null!;
    [ObservableProperty] private Technician _selectedTechnician = null!;
    [ObservableProperty] private string _orderNotes = string.Empty;
    [ObservableProperty] private string? _signatureData;
    [ObservableProperty] private ObservableCollection<OrderPhoto> _orderPhotos = new();
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
    [ObservableProperty] private string _selectedCountryCode = "+966";
    [ObservableProperty] private bool _isSearchingCustomer;
    [ObservableProperty] private string _customerSearchMessage = string.Empty;
    [ObservableProperty] private string _newCustomerFirstName = string.Empty;
    [ObservableProperty] private string _newCustomerLastName = string.Empty;
    [ObservableProperty] private string _newCustomerPhone = string.Empty;
    [ObservableProperty] private string _newPlateNumber = string.Empty;
    [ObservableProperty] private string _newPlateType = "خصوصي";
    [ObservableProperty] private string _newVin = string.Empty;
    [ObservableProperty] private string _selectedBrand = string.Empty;
    [ObservableProperty] private string _selectedModel = string.Empty;
    [ObservableProperty] private string _selectedVehicleType = string.Empty;
    [ObservableProperty] private string _selectedEngineType = string.Empty;
    [ObservableProperty] private int _newMileage = 0;
    [ObservableProperty] private int _newYear = 2025;
    [ObservableProperty] private ColorOption? _selectedColor;
    [ObservableProperty] private string _serviceSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<ServiceItem> _filteredServices = new();
    [ObservableProperty] private int _servicePage = 1;
    [ObservableProperty] private int _serviceTotalPages = 1;
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

    private const int PopupPageSize = 25;

    // Loaded from the API instead of seeded locally.
    [ObservableProperty] private ObservableCollection<WorkLocation> _locations = new();
    [ObservableProperty] private ObservableCollection<Technician> _technicians = new();
    [ObservableProperty] private ObservableCollection<string> _availableModels = new();
    [ObservableProperty] private ObservableCollection<string> _pagedModels = new();
    [ObservableProperty] private int _modelPage = 1;
    [ObservableProperty] private int _modelTotalPages = 1;
    [ObservableProperty] private ObservableCollection<string> _brands = new();
    [ObservableProperty] private ObservableCollection<string> _pagedBrands = new();
    [ObservableProperty] private int _brandPage = 1;
    [ObservableProperty] private int _brandTotalPages = 1;
    [ObservableProperty] private ObservableCollection<string> _vehicleTypes = new();
    [ObservableProperty] private ObservableCollection<string> _engineTypes = new();
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<ServiceItem> _serviceItems = new();
    [ObservableProperty] private ObservableCollection<ItemCategoryOption> _itemCategories = new();
    [ObservableProperty] private ObservableCollection<string> _categories = new();
    [ObservableProperty] private ItemCategoryOption? _selectedItemCategory;
    [ObservableProperty] private bool _isBrandPopupVisible;
    [ObservableProperty] private bool _isModelPopupVisible;
    [ObservableProperty] private bool _isColorPopupVisible;
    [ObservableProperty] private bool _isItemCategoryPopupVisible;
    [ObservableProperty] private ObservableCollection<ItemCategoryOption> _pagedItemCategories = new();
    [ObservableProperty] private int _itemCategoryPage = 1;
    [ObservableProperty] private int _itemCategoryTotalPages = 1;
    [ObservableProperty] private ObservableCollection<ColorOption> _pagedColors = new();
    [ObservableProperty] private int _colorPage = 1;
    [ObservableProperty] private int _colorTotalPages = 1;
    [ObservableProperty] private string _colorSearchText = string.Empty;
    [ObservableProperty] private string _itemCategorySearchText = string.Empty;
    [ObservableProperty] private string _technicianSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<Technician> _filteredTechnicians = new();
    [ObservableProperty] private ObservableCollection<Technician> _pagedTechnicians = new();
    [ObservableProperty] private int _technicianPage = 1;
    [ObservableProperty] private int _technicianTotalPages = 1;
    [ObservableProperty] private bool _isItemDetailPopupVisible;
    [ObservableProperty] private ServiceItem _editingServiceItem = null!;
    [ObservableProperty] private string _editingPriceText = string.Empty;
    [ObservableProperty] private string _detailTotalText = string.Empty;
    [ObservableProperty] private bool _isCartReviewVisible;
    [ObservableProperty] private string _brandSearchText = string.Empty;
    [ObservableProperty] private string _modelSearchText = string.Empty;
    [ObservableProperty] private string _locationSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<WorkLocation> _filteredLocations = new();
    [ObservableProperty] private int _locationPage = 1;
    [ObservableProperty] private int _locationTotalPages = 1;
    [ObservableProperty] private decimal _discountsTotal;

    public bool HasSignature => !string.IsNullOrWhiteSpace(SignatureData);
    public bool CanGoToPreviousLocationPage => LocationPage > 1;
    public bool CanGoToNextLocationPage => LocationPage < LocationTotalPages;
    public bool CanGoToPreviousBrandPage => BrandPage > 1;
    public bool CanGoToNextBrandPage => BrandPage < BrandTotalPages;
    public bool CanGoToPreviousModelPage => ModelPage > 1;
    public bool CanGoToNextModelPage => ModelPage < ModelTotalPages;
    public bool CanGoToPreviousServicePage => ServicePage > 1;
    public bool CanGoToNextServicePage => ServicePage < ServiceTotalPages;
    public bool CanGoToPreviousItemCategoryPage => ItemCategoryPage > 1;
    public bool CanGoToNextItemCategoryPage => ItemCategoryPage < ItemCategoryTotalPages;
    public bool CanGoToPreviousColorPage => ColorPage > 1;
    public bool CanGoToNextColorPage => ColorPage < ColorTotalPages;
    public bool CanGoToPreviousTechnicianPage => TechnicianPage > 1;
    public bool CanGoToNextTechnicianPage => TechnicianPage < TechnicianTotalPages;
    public bool IsPriceEditable => EditingServiceItem?.OpenSale == true;

    // No API source for a generic color list (wh_CustomerCars.Color is free text) or the
    // "add a custom service" category list, so these stay local. Colors carry a real swatch
    // so the picker can show a color circle, not just a name.
    public ObservableCollection<ColorOption> Colors { get; } =
    [
        new("Black", Color.FromArgb("#000000")),
        new("Jet Black", Color.FromArgb("#0A0A0A")),
        new("Obsidian Black", Color.FromArgb("#1C1C1C")),
        new("Midnight Black", Color.FromArgb("#191970")),
        new("Graphite", Color.FromArgb("#383838")),
        new("Charcoal", Color.FromArgb("#36454F")),
        new("Dark Gray", Color.FromArgb("#555555")),
        new("Gray", Color.FromArgb("#808080")),
        new("Silver", Color.FromArgb("#C0C0C0")),
        new("Brilliant Silver", Color.FromArgb("#C8C8C8")),
        new("Aluminum", Color.FromArgb("#A9A9A9")),
        new("Titanium", Color.FromArgb("#878681")),
        new("Gunmetal", Color.FromArgb("#2A3439")),
        new("Nardo Gray", Color.FromArgb("#8D9093")),

        new("White", Color.FromArgb("#FFFFFF")),
        new("Pearl White", Color.FromArgb("#F8F8FF")),
        new("Ivory White", Color.FromArgb("#FFFFF0")),
        new("Snow White", Color.FromArgb("#FFFAFA")),
        new("Cream", Color.FromArgb("#FFFDD0")),
        new("Beige", Color.FromArgb("#F5F5DC")),
        new("Champagne", Color.FromArgb("#F7E7CE")),

        new("Gold", Color.FromArgb("#FFD700")),
        new("Rose Gold", Color.FromArgb("#B76E79")),
        new("Bronze", Color.FromArgb("#CD7F32")),
        new("Copper", Color.FromArgb("#B87333")),

        new("Brown", Color.FromArgb("#8B4513")),
        new("Chocolate Brown", Color.FromArgb("#7B3F00")),
        new("Mocha", Color.FromArgb("#967969")),
        new("Mahogany", Color.FromArgb("#C04000")),

        new("Red", Color.FromArgb("#FF0000")),
        new("Bright Red", Color.FromArgb("#FF2400")),
        new("Candy Red", Color.FromArgb("#D2042D")),
        new("Ruby Red", Color.FromArgb("#9B111E")),
        new("Crimson", Color.FromArgb("#DC143C")),
        new("Burgundy", Color.FromArgb("#800020")),
        new("Maroon", Color.FromArgb("#800000")),
        new("Wine Red", Color.FromArgb("#722F37")),

        new("Orange", Color.FromArgb("#FFA500")),
        new("Burnt Orange", Color.FromArgb("#CC5500")),
        new("Copper Orange", Color.FromArgb("#DA8A67")),

        new("Yellow", Color.FromArgb("#FFFF00")),
        new("Canary Yellow", Color.FromArgb("#FFEF00")),
        new("Lemon Yellow", Color.FromArgb("#FFF44F")),
        new("Mustard", Color.FromArgb("#E1AD01")),

        new("Green", Color.FromArgb("#008000")),
        new("British Racing Green", Color.FromArgb("#004225")),
        new("Forest Green", Color.FromArgb("#228B22")),
        new("Dark Green", Color.FromArgb("#006400")),
        new("Olive Green", Color.FromArgb("#556B2F")),
        new("Lime Green", Color.FromArgb("#32CD32")),
        new("Mint Green", Color.FromArgb("#98FF98")),
        new("Emerald Green", Color.FromArgb("#50C878")),

        new("Blue", Color.FromArgb("#0000FF")),
        new("Navy Blue", Color.FromArgb("#000080")),
        new("Dark Blue", Color.FromArgb("#00008B")),
        new("Royal Blue", Color.FromArgb("#4169E1")),
        new("Electric Blue", Color.FromArgb("#7DF9FF")),
        new("Sky Blue", Color.FromArgb("#87CEEB")),
        new("Light Blue", Color.FromArgb("#ADD8E6")),
        new("Aqua Blue", Color.FromArgb("#00FFFF")),
        new("Teal", Color.FromArgb("#008080")),
        new("Turquoise", Color.FromArgb("#40E0D0")),
        new("Cyan", Color.FromArgb("#00FFFF")),

        new("Purple", Color.FromArgb("#800080")),
        new("Deep Purple", Color.FromArgb("#673AB7")),
        new("Violet", Color.FromArgb("#8F00FF")),
        new("Lavender", Color.FromArgb("#E6E6FA")),
        new("Plum", Color.FromArgb("#8E4585")),
        new("Magenta", Color.FromArgb("#FF00FF")),

        new("Pink", Color.FromArgb("#FFC0CB")),
        new("Hot Pink", Color.FromArgb("#FF69B4")),
        new("Coral", Color.FromArgb("#FF7F50")),

        new("Pearl Blue", Color.FromArgb("#6A8DFF")),
        new("Pearl Black", Color.FromArgb("#1A1A1A")),
        new("Pearl Red", Color.FromArgb("#AA0114")),
        new("Pearl Gray", Color.FromArgb("#B0B0B0")),

        new("Metallic Silver", Color.FromArgb("#BFC1C2")),
        new("Metallic Gray", Color.FromArgb("#6E7072")),
        new("Metallic Blue", Color.FromArgb("#3B6EA5")),
        new("Metallic Green", Color.FromArgb("#2E8B57")),
        new("Metallic Red", Color.FromArgb("#B22222")),
        new("Metallic Brown", Color.FromArgb("#8B5A2B")),
        new("Metallic Bronze", Color.FromArgb("#8C7853")),

        new("Matte Black", Color.FromArgb("#121212")),
        new("Matte Gray", Color.FromArgb("#696969")),
        new("Matte White", Color.FromArgb("#F5F5F5")),
        new("Matte Blue", Color.FromArgb("#1E3A8A")),
        new("Matte Green", Color.FromArgb("#355E3B")),
        new("Matte Red", Color.FromArgb("#8B0000")),

        new("Satin Black", Color.FromArgb("#242424")),
        new("Satin Silver", Color.FromArgb("#AFAFAF")),
        new("Satin Blue", Color.FromArgb("#3A5FCD")),
        new("Satin Gray", Color.FromArgb("#7E7F7F")),

        new("Two-Tone Black/White", Color.FromArgb("#808080")),
        new("Two-Tone Red/Black", Color.FromArgb("#990000")),
        new("Two-Tone Blue/White", Color.FromArgb("#4F81BD")),

        new("Custom", Color.FromArgb("#FFFFFF")),
        new("Other", Color.FromArgb("#999999")),
        new("Unknown", Color.FromArgb("#CCCCCC"))
    ];

    public ObservableCollection<string> TaxTypes { get; } = new() { "VAT", "معفى من الضريبة" };
    public ObservableCollection<string> CountryCodes { get; } = new()
    {
        "+966", "+971", "+973", "+974", "+965", "+968", "+962", "+963", "+964", "+967",
        "+20", "+27", "+30", "+31", "+32", "+33", "+34", "+36", "+39", "+40", "+41", "+43",
    };
    public ObservableCollection<Vehicle> Vehicles { get; } = new();

    public ObservableCollection<int> VehicleYears { get; } = new();

    [ObservableProperty]
    private int selectedVehicleYear;

    public NewOrderViewModel(
        INavigationService navigation,
        ICustomerCarLookupService customerCarLookupService,
        IWorkshopLookupService workshopLookupService,
        ICustomerLookupService customerLookupService,
        IItemLookupService itemLookupService,
        IBillApiService billApiService) : base(navigation)
    {
        _customerCarLookupService = customerCarLookupService;
        _workshopLookupService = workshopLookupService;
        _customerLookupService = customerLookupService;
        _itemLookupService = itemLookupService;
        _billApiService = billApiService;

        Title = LocalizationResourceManager.Instance["AddVehicle"];
        SelectedColor = Colors.First();
        LoadVehicleYears();
        CartItems.CollectionChanged += OnCartItemsChanged;
        _ = LoadInitialDataAsync();
    }

    private void OnCartItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CartItem item in e.NewItems)
                item.PropertyChanged += OnCartItemPropertyChanged;
        }
        if (e.OldItems != null)
        {
            foreach (CartItem item in e.OldItems)
                item.PropertyChanged -= OnCartItemPropertyChanged;
        }
        RecalculateTotals();
    }

    private void OnCartItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CartItem.LineTotal) || e.PropertyName == nameof(CartItem.Quantity))
            RecalculateTotals();
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
            ResetBrandPaging();

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
            FilteredLocations = new ObservableCollection<WorkLocation>(Locations);

            ItemCategories.Clear();
            Categories.Clear();
            ItemCategories.Add(new ItemCategoryOption(null, "الكل")); // "All" - clears the category filter
            foreach (var category in categoriesTask.Result.Items)
            {
                var categoryName = category.Name_En ?? category.Name_Ar ?? string.Empty;
                ItemCategories.Add(new ItemCategoryOption(category.Id, categoryName));
                Categories.Add(categoryName);
            }

            ApplyItemResults(itemsTask.Result.Items);
        });
    }

    partial void OnSelectedBrandChanged(string value)
    {
        SelectedModel = null!;
        _ = UpdateModelsForBrandAsync(value);
    }

    // Brand/Model/Color use a custom image-grid popup instead of a native Picker, since a
    // native Picker can't show an icon or swatch next to each option on any platform.
    [RelayCommand]
    private void ShowBrandPopup()
    {
        BrandSearchText = string.Empty;
        ResetBrandPaging();
        IsBrandPopupVisible = true;
    }

    [RelayCommand]
    private void CloseBrandPopup() => IsBrandPopupVisible = false;

    partial void OnBrandSearchTextChanged(string value)
    {
        ResetBrandPaging();
    }

    [RelayCommand]
    private void NextBrandPage()
    {
        if (BrandPage >= BrandTotalPages) return;
        BrandPage++;
        RefreshPagedBrands();
    }

    [RelayCommand]
    private void PreviousBrandPage()
    {
        if (BrandPage <= 1) return;
        BrandPage--;
        RefreshPagedBrands();
    }

    [RelayCommand]
    private void SelectBrand(string brand)
    {
        SelectedBrand = brand;
        IsBrandPopupVisible = false;
    }

    [RelayCommand]
    private void ShowModelPopup()
    {
        ModelSearchText = string.Empty;
        ResetModelPaging();
        IsModelPopupVisible = true;
    }

    [RelayCommand]
    private void CloseModelPopup() => IsModelPopupVisible = false;

    partial void OnModelSearchTextChanged(string value)
    {
        ResetModelPaging();
    }

    [RelayCommand]
    private void NextModelPage()
    {
        if (ModelPage >= ModelTotalPages) return;
        ModelPage++;
        RefreshPagedModels();
    }

    [RelayCommand]
    private void PreviousModelPage()
    {
        if (ModelPage <= 1) return;
        ModelPage--;
        RefreshPagedModels();
    }

    [RelayCommand]
    private void SelectVehicleModel(string model)
    {
        SelectedModel = model;
        IsModelPopupVisible = false;
    }

    [RelayCommand]
    private void ShowColorPopup()
    {
        ResetColorPaging();
        IsColorPopupVisible = true;
    }

    [RelayCommand]
    private void CloseColorPopup() => IsColorPopupVisible = false;

    partial void OnColorSearchTextChanged(string value)
    {
        ResetColorPaging();
    }

    [RelayCommand]
    private void SelectColorOption(ColorOption color)
    {
        SelectedColor = color;
        IsColorPopupVisible = false;
    }

    [RelayCommand]
    private void NextColorPage()
    {
        if (ColorPage >= ColorTotalPages) return;
        ColorPage++;
        RefreshPagedColors();
    }

    [RelayCommand]
    private void PreviousColorPage()
    {
        if (ColorPage <= 1) return;
        ColorPage--;
        RefreshPagedColors();
    }

    [RelayCommand]
    private async Task ShowItemCategoryPopup()
    {
        if (SelectedVehicle == null)
        {
            ShowAlert(AppResources.SelectVehicleFirst, string.Empty);
            return;
        }
        ResetItemCategoryPaging();
        IsItemCategoryPopupVisible = true;
    }

    [RelayCommand]
    private void CloseItemCategoryPopup() => IsItemCategoryPopupVisible = false;

    partial void OnItemCategorySearchTextChanged(string value)
    {
        ResetItemCategoryPaging();
    }

    [RelayCommand]
    private void SelectItemCategory(ItemCategoryOption category)
    {
        SelectedItemCategory = category;
        IsItemCategoryPopupVisible = false;
        IsServicePopupVisible = true;
        _ = FilterServicesAsync();
    }

    [RelayCommand]
    private void NextItemCategoryPage()
    {
        if (ItemCategoryPage >= ItemCategoryTotalPages) return;
        ItemCategoryPage++;
        RefreshPagedItemCategories();
    }

    [RelayCommand]
    private void PreviousItemCategoryPage()
    {
        if (ItemCategoryPage <= 1) return;
        ItemCategoryPage--;
        RefreshPagedItemCategories();
    }

    [RelayCommand]
    private void ShowItemDetail(ServiceItem item)
    {
        if (item == null) return;
        EditingServiceItem = new ServiceItem
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            ItemType = item.ItemType,
            Price = item.Price,
            Cost = item.Cost,
            Discount1 = item.Discount1,
            Discount2 = item.Discount2,
            Discount3 = item.Discount3,
            OpenSale = item.OpenSale,
            IsTaxable = item.IsTaxable,
            TaxType = item.TaxType,
            TaxAmount = item.TaxAmount,
            TotalPrice = item.TotalPrice,
            Quantity = 1,
            Icon = item.Icon
        };
        EditingPriceText = item.Price.ToString("F2");
        UpdateDetailTotal();
        OnPropertyChanged(nameof(IsPriceEditable));
        IsItemDetailPopupVisible = true;
    }

    partial void OnEditingPriceTextChanged(string value)
    {
        if (EditingServiceItem == null) return;
        if (decimal.TryParse(value, out var price) && price >= 0)
        {
            EditingServiceItem.Price = price;
            var taxAmount = EditingServiceItem.IsTaxable ? price * 0.15m : 0;
            EditingServiceItem.TaxAmount = taxAmount;
            var afterDiscount = price - EditingServiceItem.Discount1 - EditingServiceItem.Discount2 - EditingServiceItem.Discount3;
            EditingServiceItem.TotalPrice = afterDiscount + taxAmount;
            UpdateDetailTotal();
        }
    }

    private void UpdateDetailTotal()
    {
        if (EditingServiceItem != null)
        {
            DetailTotalText = $"{EditingServiceItem.TotalPrice:N2} SAR";
        }
    }

    [RelayCommand]
    private void ConfirmAddToCart()
    {
        if (EditingServiceItem == null) return;
        AddServiceToCart(EditingServiceItem);
        IsItemDetailPopupVisible = false;
        EditingServiceItem = null!;
    }

    [RelayCommand]
    private void CancelItemDetail()
    {
        IsItemDetailPopupVisible = false;
        EditingServiceItem = null!;
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
        ResetModelPaging();

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
            ResetModelPaging();
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
        if (string.IsNullOrWhiteSpace(SearchPhoneNumber) || SearchPhoneNumber.Length < 2)
        {
            ShowAlert(string.Empty, AppResources.InvalidPhoneNumber);
            return;
        }

        IsSearchingCustomer = true;
        CustomerSearchMessage = string.Empty;

        await ExecuteAsync(async () =>
        {
            var fullPhone = SelectedCountryCode + SearchPhoneNumber;
            var results = await _customerLookupService.SearchAsync(fullPhone, pageSize: 20);

            Customers.Clear();
            foreach (var c in results.Items)
            {
                Customers.Add(new Customer
                {
                    FirstName = c.Name_Ar,
                    LastName = c.Name_En,
                    PhoneNumber = c.Mobile ?? c.Phone1 ?? string.Empty
                });
            }

            var exactMatch = Customers.FirstOrDefault(c => c.PhoneNumber == fullPhone || c.PhoneNumber == SearchPhoneNumber);
            if (exactMatch != null)
            {
                SelectedCustomer = exactMatch;
                IsCustomerPopupVisible = false;
                CustomerSearchMessage = string.Empty;
            }
            else
            {
                NewCustomerPhone = fullPhone;
                CustomerSearchMessage = AppResources.CustomerNotFound;
            }
        });

        IsSearchingCustomer = false;
    }

    [RelayCommand]
    private void SaveNewCustomer()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerFirstName))
        {
            ShowAlert(string.Empty, AppResources.FirstNameRequired);
            return;
        }

        if (string.IsNullOrWhiteSpace(NewCustomerPhone) || NewCustomerPhone.Length != 9 || !NewCustomerPhone.StartsWith('5') || !NewCustomerPhone.All(char.IsDigit))
        {
            ShowAlert(string.Empty, AppResources.InvalidPhoneNumber);
            return;
        }

        var customer = new Customer
        {
            FirstName = NewCustomerFirstName,
            LastName = NewCustomerLastName,
            PhoneNumber = NewCustomerPhone
        };
        Customers.Add(customer);
        SelectedCustomer = customer;
        IsCustomerPopupVisible = false;
        ClearNewCustomerFields();
    }

    [RelayCommand]
    private async Task ShowVehiclePopup()
    {
        if (SelectedCustomer == null)
        {
            ShowAlert(AppResources.SelectCustomerFirst, string.Empty);
            return;
        }
        IsVehiclePopupVisible = true;
    }

    [RelayCommand]
    private void CloseVehiclePopup()
    {
        IsVehiclePopupVisible = false;
    }

    [RelayCommand]
    private async Task SaveNewVehicle()
    {
        if (SelectedCustomer == null)
        {
            ShowAlert(AppResources.SelectCustomerFirst, string.Empty);
            return;
        }

        if (NewVin.Length > 17)
        {
            NewVin = NewVin[..17];
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
            Year = SelectedVehicleYear,
            Color = SelectedColor?.Name,
            PlateType = NewPlateType,
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
        NewServiceItem = null!;
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

            ServicePage = 1;
            var results = await _itemLookupService.SearchAsync(search, categoryId, ServicePage, PopupPageSize);
            ServiceTotalPages = Math.Max(1, results.TotalPages);
            ApplyItemResults(results.Items);
        });
    }

    [RelayCommand]
    private async Task NextServicePageAsync()
    {
        if (ServicePage >= ServiceTotalPages) return;
        ServicePage++;
        await LoadServicePageAsync();
    }

    [RelayCommand]
    private async Task PreviousServicePageAsync()
    {
        if (ServicePage <= 1) return;
        ServicePage--;
        await LoadServicePageAsync();
    }

    private async Task LoadServicePageAsync()
    {
        await ExecuteAsync(async () =>
        {
            var categoryId = SelectedItemCategory?.Id;
            var search = string.IsNullOrWhiteSpace(ServiceSearchText) ? null : ServiceSearchText;
            var results = await _itemLookupService.SearchAsync(search, categoryId, ServicePage, PopupPageSize);
            ServiceTotalPages = Math.Max(1, results.TotalPages);
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
        var discount1 = (decimal)(item.Discount1 ?? 0);
        var discount2 = (decimal)(item.Discount2 ?? 0);
        var discount3 = (decimal)(item.Discount3 ?? 0);
        var afterDiscount = price - discount1 - discount2 - discount3;

        return new ServiceItem
        {
            Id = item.ItemBarCode,
            Name = item.Name_En ?? item.Name_Ar ?? item.ItemBarCode,
            Category = item.ItemGroupName_En ?? item.ItemGroupName_Ar ?? string.Empty,
            ItemType = "Product",
            Price = price,
            Cost = 0,
            Discount1 = discount1,
            Discount2 = discount2,
            Discount3 = discount3,
            OpenSale = item.OpenSale,
            IsTaxable = taxRate > 0,
            TaxType = "VAT",
            TaxAmount = taxAmount,
            TotalPrice = afterDiscount + taxAmount
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
        NewServiceItem = null!;
    }

    [RelayCommand]
    private async Task ShowLocationPopup()
    {
        if (SelectedVehicle == null)
        {
            ShowAlert(AppResources.SelectVehicleFirst, string.Empty);
            return;
        }
        LocationSearchText = string.Empty;
        ResetLocationPaging();
        IsLocationPopupVisible = true;
    }

    [RelayCommand]
    private void CloseLocationPopup() => IsLocationPopupVisible = false;

    [RelayCommand]
    private void SelectLocation(WorkLocation location)
    {
        SelectedLocation = location;
        IsLocationPopupVisible = false;
    }

    [RelayCommand]
    private async Task ShowTechnicianPopup()
    {
        if (SelectedVehicle == null)
        {
            ShowAlert(AppResources.SelectVehicleFirst, string.Empty);
            return;
        }
        TechnicianSearchText = string.Empty;
        ResetTechnicianPaging();
        IsTechnicianPopupVisible = true;
    }

    [RelayCommand]
    private void CloseTechnicianPopup() => IsTechnicianPopupVisible = false;

    partial void OnTechnicianSearchTextChanged(string value)
    {
        ResetTechnicianPaging();
    }

    [RelayCommand]
    private void NextTechnicianPage()
    {
        if (TechnicianPage >= TechnicianTotalPages) return;
        TechnicianPage++;
        RefreshPagedTechnicians();
    }

    [RelayCommand]
    private void PreviousTechnicianPage()
    {
        if (TechnicianPage <= 1) return;
        TechnicianPage--;
        RefreshPagedTechnicians();
    }

    private IEnumerable<Technician> GetFilteredTechnicians()
    {
        if (string.IsNullOrWhiteSpace(TechnicianSearchText))
            return Technicians;
        return Technicians.Where(t => t.Name != null && t.Name.Contains(TechnicianSearchText, StringComparison.OrdinalIgnoreCase));
    }

    private void ResetTechnicianPaging()
    {
        TechnicianPage = 1;
        var filtered = GetFilteredTechnicians().ToList();
        TechnicianTotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PopupPageSize));
        RefreshPagedTechnicians();
    }

    private void RefreshPagedTechnicians()
    {
        var filtered = GetFilteredTechnicians().ToList();
        PagedTechnicians = new ObservableCollection<Technician>(filtered.Skip((TechnicianPage - 1) * PopupPageSize).Take(PopupPageSize));
        OnPropertyChanged(nameof(CanGoToPreviousTechnicianPage));
        OnPropertyChanged(nameof(CanGoToNextTechnicianPage));
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

    partial void OnSignatureDataChanged(string? value)
    {
        OnPropertyChanged(nameof(HasSignature));
    }

    [RelayCommand]
    private void ClearLocation()
    {
        SelectedLocation = null!;
    }

    [RelayCommand]
    private void ClearTechnician()
    {
        SelectedTechnician = null!;
    }

    [RelayCommand]
    private void ClearServiceAssignment()
    {
        SelectedLocation = null!;
        SelectedTechnician = null!;
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ShowAlert("Camera", "Camera capture is not supported on this device.");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;

            var fileName = $"order-photo-{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
            var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await using var sourceStream = await photo.OpenReadAsync();
            await using var localStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(localStream);

            OrderPhotos.Add(new OrderPhoto(localPath));
        });
    }

    [RelayCommand]
    private void RemovePhoto(OrderPhoto photo)
    {
        if (photo == null) return;
        OrderPhotos.Remove(photo);
    }

    [RelayCommand]
    private async Task ReviewOrder()
    {
        var missing = new List<string>();

        if (SelectedCustomer == null)
            missing.Add(AppResources.CustomerField);

        if (SelectedVehicle == null)
        {
            missing.Add(AppResources.VehicleField);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(SelectedVehicle.PlateNumber))
                missing.Add(AppResources.PlateNumberField);
            if (string.IsNullOrWhiteSpace(SelectedVehicle.Brand))
                missing.Add(AppResources.BrandField);
            if (string.IsNullOrWhiteSpace(SelectedVehicle.Model))
                missing.Add(AppResources.ModelField);
            if (SelectedColor == null || string.IsNullOrWhiteSpace(SelectedColor.Name))
                missing.Add(AppResources.ColorField);
        }

        if (CartItems.Count == 0)
            missing.Add(AppResources.ItemsField);

        if (SelectedLocation == null)
            missing.Add(AppResources.LocationField);

        if (SelectedTechnician == null)
            missing.Add(AppResources.TechnicianField);

        if (missing.Count > 0)
        {
            ShowAlert(AppResources.PleaseCompleteData, "\n• " + string.Join("\n• ", missing));
            return;
        }

        IsCartReviewVisible = true;
    }

    [RelayCommand]
    private void CloseCartReview()
    {
        IsCartReviewVisible = false;
    }

    [RelayCommand]
    private async Task SubmitOrder()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var plateNo = SelectedVehicle?.PlateNumber ?? NewPlateNumber;

            var billDetails = CartItems.Select(ci => new CreateBillLineRequest(
                ItemBarCode: ci.ServiceItem.Id ?? string.Empty,
                ItemID: 0,
                Package: null,
                Qty: ci.Quantity,
                Price: (double)ci.ServiceItem.Price,
                DetailDiscount1: null,
                DetailTax: (double)ci.ServiceItem.TaxAmount,
                DetailNotes: null)).ToList();

            var request = new CreateBillRequest(
                BranchID: null,
                CustomerId: null,
                EngineerId: null,
                CarHeaderId: null,
                Notes: OrderNotes,
                RefrenceNo: plateNo,
                Details: billDetails);

            var result = await _billApiService.CreateBillAsync(request);

            if (!result.Success)
            {
                ShowAlert(AppResources.Error, result.ErrorMessage ?? "Failed to save bill");
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
                PhotoPaths = new ObservableCollection<string>(OrderPhotos.Select(photo => photo.Path)),
                Status = "مكتملة"
            };
            AppData.Orders.Add(order);

            await Navigation.GoToMainRootAsync();
        }
        catch (Exception ex)
        {
            ShowAlert(AppResources.Error, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RecalculateTotals()
    {
        SubTotal = CartItems.Sum(c => c.ServiceItem.Price * c.Quantity);
        TaxTotal = CartItems.Sum(c => c.ServiceItem.TaxAmount * c.Quantity);
        DiscountsTotal = CartItems.Sum(c => (c.ServiceItem.Discount1 + c.ServiceItem.Discount2 + c.ServiceItem.Discount3) * c.Quantity);
        Total = CartItems.Sum(c => c.LineTotal);
    }

    private string GetIconForCategory(string category)
    {
        return "car.svg";
    }

    private IEnumerable<string> GetFilteredBrands()
    {
        if (string.IsNullOrWhiteSpace(BrandSearchText))
            return Brands;
        return Brands.Where(b => b.Contains(BrandSearchText, StringComparison.OrdinalIgnoreCase));
    }

    internal void ResetBrandPaging()
    {
        BrandPage = 1;
        var filtered = GetFilteredBrands().ToList();
        BrandTotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PopupPageSize));
        RefreshPagedBrands();
    }

    private void RefreshPagedBrands()
    {
        var filtered = GetFilteredBrands().ToList();
        PagedBrands = new ObservableCollection<string>(filtered.Skip((BrandPage - 1) * PopupPageSize).Take(PopupPageSize));
        OnPropertyChanged(nameof(CanGoToPreviousBrandPage));
        OnPropertyChanged(nameof(CanGoToNextBrandPage));
    }

    private IEnumerable<string> GetFilteredModels()
    {
        if (string.IsNullOrWhiteSpace(ModelSearchText))
            return AvailableModels;
        return AvailableModels.Where(m => m.Contains(ModelSearchText, StringComparison.OrdinalIgnoreCase));
    }

    internal void ResetModelPaging()
    {
        ModelPage = 1;
        var filtered = GetFilteredModels().ToList();
        ModelTotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PopupPageSize));
        RefreshPagedModels();
    }

    private void RefreshPagedModels()
    {
        var filtered = GetFilteredModels().ToList();
        PagedModels = new ObservableCollection<string>(filtered.Skip((ModelPage - 1) * PopupPageSize).Take(PopupPageSize));
        OnPropertyChanged(nameof(CanGoToPreviousModelPage));
        OnPropertyChanged(nameof(CanGoToNextModelPage));
    }

    private IEnumerable<ItemCategoryOption> GetFilteredItemCategories()
    {
        if (string.IsNullOrWhiteSpace(ItemCategorySearchText))
            return ItemCategories;
        return ItemCategories.Where(c => c.Name.Contains(ItemCategorySearchText, StringComparison.OrdinalIgnoreCase));
    }

    private void ResetItemCategoryPaging()
    {
        ItemCategoryPage = 1;
        var filtered = GetFilteredItemCategories().ToList();
        ItemCategoryTotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PopupPageSize));
        RefreshPagedItemCategories();
    }

    private void RefreshPagedItemCategories()
    {
        var filtered = GetFilteredItemCategories().ToList();
        PagedItemCategories = new ObservableCollection<ItemCategoryOption>(filtered.Skip((ItemCategoryPage - 1) * PopupPageSize).Take(PopupPageSize));
        OnPropertyChanged(nameof(CanGoToPreviousItemCategoryPage));
        OnPropertyChanged(nameof(CanGoToNextItemCategoryPage));
    }

    private IEnumerable<ColorOption> GetFilteredColors()
    {
        if (string.IsNullOrWhiteSpace(ColorSearchText))
            return Colors;
        return Colors.Where(c => c.Name.Contains(ColorSearchText, StringComparison.OrdinalIgnoreCase));
    }

    private void ResetColorPaging()
    {
        ColorPage = 1;
        var filtered = GetFilteredColors().ToList();
        ColorTotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PopupPageSize));
        RefreshPagedColors();
    }

    private void RefreshPagedColors()
    {
        var filtered = GetFilteredColors().ToList();
        PagedColors = new ObservableCollection<ColorOption>(filtered.Skip((ColorPage - 1) * PopupPageSize).Take(PopupPageSize));
        OnPropertyChanged(nameof(CanGoToPreviousColorPage));
        OnPropertyChanged(nameof(CanGoToNextColorPage));
    }

    partial void OnBrandPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousBrandPage));
        OnPropertyChanged(nameof(CanGoToNextBrandPage));
    }

    partial void OnBrandTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousBrandPage));
        OnPropertyChanged(nameof(CanGoToNextBrandPage));
    }

    partial void OnModelPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousModelPage));
        OnPropertyChanged(nameof(CanGoToNextModelPage));
    }

    partial void OnModelTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousModelPage));
        OnPropertyChanged(nameof(CanGoToNextModelPage));
    }

    partial void OnServicePageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousServicePage));
        OnPropertyChanged(nameof(CanGoToNextServicePage));
    }

    partial void OnServiceTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousServicePage));
        OnPropertyChanged(nameof(CanGoToNextServicePage));
    }

    partial void OnItemCategoryPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousItemCategoryPage));
        OnPropertyChanged(nameof(CanGoToNextItemCategoryPage));
    }

    partial void OnItemCategoryTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousItemCategoryPage));
        OnPropertyChanged(nameof(CanGoToNextItemCategoryPage));
    }

    partial void OnLocationSearchTextChanged(string value)
    {
        ResetLocationPaging();
    }

    private IEnumerable<WorkLocation> GetFilteredLocations()
    {
        if (string.IsNullOrWhiteSpace(LocationSearchText))
            return Locations;
        return Locations.Where(l => l.Name != null && l.Name.Contains(LocationSearchText, StringComparison.OrdinalIgnoreCase));
    }

    private void ResetLocationPaging()
    {
        LocationPage = 1;
        var filtered = GetFilteredLocations().ToList();
        LocationTotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PopupPageSize));
        RefreshPagedLocations();
    }

    private void RefreshPagedLocations()
    {
        var filtered = GetFilteredLocations().ToList();
        FilteredLocations = new ObservableCollection<WorkLocation>(filtered.Skip((LocationPage - 1) * PopupPageSize).Take(PopupPageSize));
        OnPropertyChanged(nameof(CanGoToPreviousLocationPage));
        OnPropertyChanged(nameof(CanGoToNextLocationPage));
    }

    [RelayCommand]
    private void NextLocationPage()
    {
        if (LocationPage >= LocationTotalPages) return;
        LocationPage++;
        RefreshPagedLocations();
    }

    [RelayCommand]
    private void PreviousLocationPage()
    {
        if (LocationPage <= 1) return;
        LocationPage--;
        RefreshPagedLocations();
    }

    partial void OnLocationPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousLocationPage));
        OnPropertyChanged(nameof(CanGoToNextLocationPage));
    }

    partial void OnLocationTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousLocationPage));
        OnPropertyChanged(nameof(CanGoToNextLocationPage));
    }

    partial void OnColorPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousColorPage));
        OnPropertyChanged(nameof(CanGoToNextColorPage));
    }

    partial void OnColorTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousColorPage));
        OnPropertyChanged(nameof(CanGoToNextColorPage));
    }

    partial void OnTechnicianPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousTechnicianPage));
        OnPropertyChanged(nameof(CanGoToNextTechnicianPage));
    }

    partial void OnTechnicianTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousTechnicianPage));
        OnPropertyChanged(nameof(CanGoToNextTechnicianPage));
    }

    private void ClearNewCustomerFields()
    {
        NewCustomerFirstName = string.Empty;
        NewCustomerLastName = string.Empty;
        NewCustomerPhone = string.Empty;
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
        SelectedColor = Colors.First();
    }

    private void ClearNewServiceFields()
    {
        NewServiceName = string.Empty;
        NewServiceCategory = "بنزين";
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

// Pairs a color name with a real swatch so the color picker can show it, not just text.
public record ColorOption(string Name, Color Swatch);

public record OrderPhoto(string Path);
