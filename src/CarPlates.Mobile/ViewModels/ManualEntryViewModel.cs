using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Mobile.Helpers;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class ManualEntryViewModel : BaseViewModel
{
    private const int MaxPlateLength = 10;
    private readonly ICustomerCarLookupService _customerCarLookupService;
    private readonly IAuthenticationService _authenticationService;

    private static readonly Dictionary<char, string> EnglishToArabic = new()
    {
        ['a'] = "ا",
        ['A'] = "ا",
        ['b'] = "ب",
        ['B'] = "ب",
        ['c'] = "س",
        ['C'] = "س",
        ['d'] = "د",
        ['D'] = "د",
        ['e'] = "ي",
        ['E'] = "ي",
        ['f'] = "ف",
        ['F'] = "ف",
        ['g'] = "ق",
        ['G'] = "ق",
        ['h'] = "ه",
        ['H'] = "ه",
        ['i'] = "ي",
        ['I'] = "ي",
        ['j'] = "ج",
        ['J'] = "ج",
        ['k'] = "ك",
        ['K'] = "ك",
        ['l'] = "ل",
        ['L'] = "ل",
        ['m'] = "م",
        ['M'] = "م",
        ['n'] = "ن",
        ['N'] = "ن",
        ['o'] = "و",
        ['O'] = "و",
        ['p'] = "ب",
        ['P'] = "ب",
        ['q'] = "ق",
        ['Q'] = "ق",
        ['r'] = "ر",
        ['R'] = "ر",
        ['s'] = "س",
        ['S'] = "س",
        ['t'] = "ت",
        ['T'] = "ت",
        ['u'] = "و",
        ['U'] = "و",
        ['v'] = "ف",
        ['V'] = "ف",
        ['w'] = "و",
        ['W'] = "و",
        ['x'] = "إكس",
        ['X'] = "إكس",
        ['y'] = "ي",
        ['Y'] = "ي",
        ['z'] = "ز",
        ['Z'] = "ز",
    };

    [ObservableProperty]
    private string plateText = string.Empty;

    [ObservableProperty]
    private string arabicText = string.Empty;

    [ObservableProperty]
    private string plateInput = string.Empty;

    partial void OnPlateInputChanged(string value)
    {
        if (IsBusy)
            return;

        if (string.IsNullOrEmpty(value))
        {
            PlateText = string.Empty;
            ArabicText = string.Empty;
            OnPropertyChanged(nameof(PlateChars));
            OnPropertyChanged(nameof(ArabicChars));
            return;
        }

        PlateText = string.Empty;
        ArabicText = string.Empty;

        bool isFirst = true;
        foreach (char c in value)
        {
            if (char.IsLetter(c) && c <= 127)
            {
                bool useUpper = isFirst || char.IsDigit(PlateText.Length > 0 ? PlateText[^1] : '0');
                char transformed = useUpper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);

                if (EnglishToArabic.TryGetValue(transformed, out var arabicChar))
                {
                    PlateText += transformed;
                    ArabicText += arabicChar;
                    isFirst = false;
                }
            }
            else if (char.IsDigit(c) || c == '-' || c == '_')
            {
                PlateText += c;
                ArabicText += c;
                isFirst = false;
            }
        }

        OnPropertyChanged(nameof(PlateChars));
        OnPropertyChanged(nameof(ArabicChars));
    }

    public List<string> PlateChars => string.IsNullOrEmpty(PlateText)
        ? []
        : [.. PlateText.Select(c => c.ToString())];

    public List<string> ArabicChars => string.IsNullOrEmpty(ArabicText)
        ? []
        : [.. ArabicText.Select(c => c.ToString())];

    [ObservableProperty]
    private string plateType = "خصوصي";

    public List<PlateTypeOption> PlateTypes { get; }



    public Color PlateTextColor => PlateType switch
    {
        "خصوصي" or "Private" => Colors.Black,
        "نقل عام" or "Public Transport" => Colors.Orange,
        "تجاري" or "Commercial" => Colors.Blue,
        "دبلوماسي" or "Diplomatic" => Colors.Green,
        "مؤقته" or "Temporary" => Colors.LightGray,
        "معدات ثقيله" or "Heavy Equipment" => Colors.DarkRed,
        "اخرى" or "Other" => Colors.Gray,
        _ => Colors.Black
    };

    public Color PlateBorderColor => PlateType switch
    {
        "خصوصي" or "Private" => Colors.Wheat,
        "نقل عام" or "Public Transport" => Colors.Orange,
        "تجاري" or "Commercial" => Colors.Blue,
        "دبلوماسي" or "Diplomatic" => Colors.Green,
        "مؤقته" or "Temporary" => Colors.LightGray,
        "معدات ثقيله" or "Heavy Equipment" => Colors.DarkRed,
        "اخرى" or "Other" => Colors.Gray,
        _ => Colors.Black
    };

    partial void OnPlateTypeChanged(string value)
    {
        OnPropertyChanged(nameof(PlateTextColor));
        OnPropertyChanged(nameof(PlateBorderColor));
    }

    public ManualEntryViewModel(
        INavigationService navigation,
        ICustomerCarLookupService customerCarLookupService,
        IAuthenticationService authenticationService) : base(navigation)
    {
        _customerCarLookupService = customerCarLookupService;
        _authenticationService = authenticationService;
        Title = AppResources.ManualEntry;
        PlateTypes =
        [
            new("خصوصي", "Private"),
            new("نقل عام", "Public Transport"),
            new("تجاري", "Commercial"),
            new("دبلوماسي", "Diplomatic"),
            new("مؤقته", "Temporary"),
            new("معدات ثقيله", "Heavy Equipment"),
            new("اخرى", "Other"),
        ];
    }

    [RelayCommand]
    private async Task ClosePage()
    {
        await Navigation.GoBackAsync();
    }

    [RelayCommand]
    private void SelectPlateType(string type)
    {
        PlateType = type;
    }

    [RelayCommand]
    private async Task SubmitPlate()
    {
        if (string.IsNullOrWhiteSpace(PlateText))
            return;

        await ExecuteAsync(async () =>
        {
            var trimmed = PlateText.Trim().ToUpperInvariant();

            await Navigation.GoBackAsync();

            var rootPage = Microsoft.Maui.Controls.Application.Current?
                .Windows
                .FirstOrDefault()?
                .Page;

            if (rootPage is NavigationPage navigationPage &&
                navigationPage.CurrentPage?.BindingContext is ScannerViewModel scanner)
            {
                await scanner.ProcessRecognizedTextCommand.ExecuteAsync(trimmed);
            }
            else
            {
                var currentUser = await _authenticationService.GetCurrentUserAsync();
                var scanResult = await _customerCarLookupService.ScanAsync(
                    new CustomerCarScanRequest(trimmed, currentUser?.BranchId ?? 0));
                if (scanResult.Success)
                {
                    var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                    var brand = isRtl ? (scanResult.MakeName_Ar ?? scanResult.MakeName_En ?? scanResult.MakeName)
                                      : (scanResult.MakeName_En ?? scanResult.MakeName_Ar ?? scanResult.MakeName);
                    var model = isRtl ? (scanResult.ModelName_Ar ?? scanResult.ModelName_En ?? scanResult.ModelName)
                                      : (scanResult.ModelName_En ?? scanResult.ModelName_Ar ?? scanResult.ModelName);
                    var owner = isRtl ? (scanResult.CustomerName_Ar ?? scanResult.CustomerName_En)
                                      : (scanResult.CustomerName_En ?? scanResult.CustomerName_Ar);
                    var vehicleInfo = new VehicleDetailsDto(
                        trimmed,
                        brand,
                        model,
                        scanResult.Color,
                        owner,
                        null,
                        DateTime.UtcNow,
                        1,
                        null,
                        CarHeaderId: scanResult.CarHeaderId,
                        CustomerName_Ar: scanResult.CustomerName_Ar,
                        CustomerName_En: scanResult.CustomerName_En,
                        CustomerMobile: scanResult.CustomerMobile);
                    await Navigation.GoToCustomerDataAsync(trimmed, vehicleInfo);
                }
                else
                {
                    await Navigation.GoToCustomerDataAsync(trimmed);
                }
            }
        });
    }

    [RelayCommand]
    private void ClearPlate()
    {
        PlateInput = string.Empty;
        PlateText = string.Empty;
        ArabicText = string.Empty;
        OnPropertyChanged(nameof(PlateChars));
        OnPropertyChanged(nameof(ArabicChars));
    }

    [RelayCommand]
    private void Backspace()
    {
        if (PlateInput.Length > 0)
        {
            PlateInput = PlateInput[..^1];
        }
    }
}

public class PlateTypeOption(string nameAr, string nameEn)
{
    public string NameAr { get; } = nameAr;
    public string NameEn { get; } = nameEn;
    public string DisplayName { get; } = LocalizeHelper.Localize(nameAr, nameEn);

    public override string ToString() => NameAr;
}