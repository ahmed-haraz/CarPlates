using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class ManualEntryViewModel : BaseViewModel
{
    private const int MaxPlateLength = 10;

    private static readonly Dictionary<char, string> EnglishToArabic = new()
    {
        ['a'] = "ا", ['A'] = "ا",
        ['b'] = "ب", ['B'] = "ب",
        ['c'] = "س", ['C'] = "س",
        ['d'] = "د", ['D'] = "د",
        ['e'] = "ي", ['E'] = "ي",
        ['f'] = "ف", ['F'] = "ف",
        ['g'] = "ق", ['G'] = "ق",
        ['h'] = "ه", ['H'] = "ه",
        ['i'] = "ي", ['I'] = "ي",
        ['j'] = "ج", ['J'] = "ج",
        ['k'] = "ك", ['K'] = "ك",
        ['l'] = "ل", ['L'] = "ل",
        ['m'] = "م", ['M'] = "م",
        ['n'] = "ن", ['N'] = "ن",
        ['o'] = "و", ['O'] = "و",
        ['p'] = "ب", ['P'] = "ب",
        ['q'] = "ق", ['Q'] = "ق",
        ['r'] = "ر", ['R'] = "ر",
        ['s'] = "س", ['S'] = "س",
        ['t'] = "ت", ['T'] = "ت",
        ['u'] = "و", ['U'] = "و",
        ['v'] = "ف", ['V'] = "ف",
        ['w'] = "و", ['W'] = "و",
        ['x'] = "إكس", ['X'] = "إكس",
        ['y'] = "ي", ['Y'] = "ي",
        ['z'] = "ز", ['Z'] = "ز",
    };

    [ObservableProperty]
    private string plateText = string.Empty;

    [ObservableProperty]
    private string arabicText = string.Empty;

    [ObservableProperty]
    private string plateInput = string.Empty;

    partial void OnPlateInputChanged(string value)
    {
        if (IsProcessing)
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
        ? new List<string>()
        : PlateText.Select(c => c.ToString()).ToList();

    public List<string> ArabicChars => string.IsNullOrEmpty(ArabicText)
        ? new List<string>()
        : ArabicText.Select(c => c.ToString()).ToList();

    [ObservableProperty]
    private string plateType = "خصوصي";

    [ObservableProperty]
    private bool isProcessing;

    public List<string> PlateTypes { get; } = new()
    {
        "خصوصي",
        "نقل عام",
        "تجاري",
        "دبلوماسي",
        "مؤقته",
        "معدات ثقيله",
        "اخرى"
    };

    public Color PlateTextColor => PlateType switch
    {
        "خصوصي" => Colors.Black,
        "نقل عام" => Colors.Orange,
        "تجاري" => Colors.Blue,
        "دبلوماسي" => Colors.Green,
        "مؤقته" => Colors.LightGray,
        "معدات ثقيله" => Colors.DarkRed,
        "اخرى" => Colors.Gray,
        _ => Colors.Black
    };

    public Color PlateBorderColor => PlateType switch
    {
        "خصوصي" => Colors.Wheat,
        "نقل عام" => Colors.Orange,
        "تجاري" => Colors.Blue,
        "دبلوماسي" => Colors.Green,
        "مؤقته" => Colors.LightGray,
        "معدات ثقيله" => Colors.DarkRed,
        "اخرى" => Colors.Gray,
        _ => Colors.Black
    };

    partial void OnPlateTypeChanged(string value)
    {
        OnPropertyChanged(nameof(PlateTextColor));
        OnPropertyChanged(nameof(PlateBorderColor));
    }

    public ManualEntryViewModel(INavigationService navigation) : base(navigation)
    {
        Title = AppResources.ManualEntry;
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

        IsProcessing = true;

        try
        {
            var trimmed = ArabicText.Trim();

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
                await Navigation.GoToCustomerDataAsync(trimmed);
            }
        }
        finally
        {
            IsProcessing = false;
        }
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