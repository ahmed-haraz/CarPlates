using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CarPlates.Domain.Entities;

public partial class Customer : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _lastName;
    [ObservableProperty] private string? _phoneNumber;
    [ObservableProperty] private string? _gender;
}

public partial class Vehicle : ObservableObject
{
    [ObservableProperty] private string? _id;
    [ObservableProperty] private string? _plateNumber;
    [ObservableProperty] private string? _vin;
    [ObservableProperty] private string? _brand;
    [ObservableProperty] private string? _model;
    [ObservableProperty] private string? _vehicleType; // نوع المركبة
    [ObservableProperty] private string? _engineType; // نوع المحرك
    [ObservableProperty] private int _mileage;
    [ObservableProperty] private int _year;
    [ObservableProperty] private string? _color;
    [ObservableProperty] private int _customerId;
    [ObservableProperty] private string? _status = "غير معين"; // غير معين / تم التعيين / قيد الخدمة
}

public partial class ServiceItem : ObservableObject
{
    [ObservableProperty] private string? _id;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _category; // الفئة
    [ObservableProperty] private string? _itemType; // نوع الصنف (Product/Service)
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private decimal _cost;
    [ObservableProperty] private decimal _discount1;
    [ObservableProperty] private decimal _discount2;
    [ObservableProperty] private decimal _discount3;
    [ObservableProperty] private bool _openSale;
    [ObservableProperty] private bool _isTaxable;
    [ObservableProperty] private string? _taxType; // VAT
    [ObservableProperty] private decimal _taxAmount;
    [ObservableProperty] private decimal _totalPrice; // Price - Discounts + Tax
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private string? _icon;
}

public partial class CartItem : ObservableObject
{
    [ObservableProperty] private ServiceItem _serviceItem = null!;
    [ObservableProperty] private int _quantity = 1;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }

    public decimal LineTotal => ServiceItem.TotalPrice * Quantity;
}

public partial class WorkLocation : ObservableObject
{
    [ObservableProperty] private string? _id;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _type; // كراج / محل
}

public partial class Technician : ObservableObject
{
    [ObservableProperty] private string? _id;
    [ObservableProperty] private string? _name;
}

public partial class Order : ObservableObject
{
    [ObservableProperty] private string? _id;
    [ObservableProperty] private Vehicle? _vehicle;
    [ObservableProperty] private Customer? _customer;
    [ObservableProperty] private ObservableCollection<CartItem> _items = new();
    [ObservableProperty] private WorkLocation? _location;
    [ObservableProperty] private Technician? _technician;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _signature;
    [ObservableProperty] private ObservableCollection<string> _photoPaths = new();
    [ObservableProperty] private DateTime _createdAt = DateTime.Now;
    [ObservableProperty] private string? _status = "ملغاة";

    public decimal SubTotal => Items.Sum(i => i.ServiceItem.Price * i.Quantity);
    public decimal TaxTotal => Items.Sum(i => i.ServiceItem.TaxAmount * i.Quantity);
    public decimal Total => Items.Sum(i => i.LineTotal);
}

public static class AppData
{
    public static ObservableCollection<Vehicle> Vehicles { get; } = new();
    public static ObservableCollection<Customer> Customers { get; } = new();
    public static ObservableCollection<ServiceItem> ServiceItems { get; } = new();
    public static ObservableCollection<Order> Orders { get; } = new();
    public static ObservableCollection<string> VehicleBrands { get; } = new()
    {
        "ألفا روميو", "اكيورا", "أبارث", "أستون مارتن", "أشوك ليلاند", "اي ام سي",
        "BAW", "بايك", "أودي", "بي ام دبليو", "بيستون", "بنتلي", "بي واي دي", "بويك", "برابوس"
    };
    public static ObservableCollection<string> VehicleTypes { get; } = new()
    {
        "سيدان", "دفع رباعي", "شاحنة", "كوبيه", "فان", "أخرى"
    };
    public static ObservableCollection<string> EngineTypes { get; } = new()
    {
        "Petrol", "Diesel", "Hybrid"
    };
    public static ObservableCollection<string> Colors { get; } = new()
    {
        "Beige", "Black", "Blue", "Bronze", "Brown", "Gold", "Gray", "Green",
        "Orange", "Pink", "Purple", "Red", "Silver", "White", "Yellow"
    };
    public static ObservableCollection<string> ServiceCategories { get; } = new()
    {
        "منتج التحويل", "الفنين", "فحص", "الميكانيك", "بانزين", "اختبار كاراجي",
        "قطع غيار", "السمكرة و البويا", "الكهربا", "سيارة صغيرة", "سيارة وسط", "سيارة كبيرة",
        "ماء الردياتر", "خدمات", "زيوت القير", "اطارات المركبة", "زيوت المحرك", "فلتر الزيت",
        "فلتر الهواء", "البطاريات", "العناية بالمركبة"
    };
}
