using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public partial class ManualEntryViewModel : BaseViewModel
{
    private const string BackspaceKey = "backspace";
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

    private static readonly HashSet<char> AllowedChars = new()
    {
        '0','1','2','3','4','5','6','7','8','9',
        '-','_',' ',

        'ا','أ','إ','آ',
        'ب','ت','ث','ج','ح','خ',
        'د','ذ','ر','ز','س','ش',
        'ص','ض','ط','ظ',
        'ع','غ',
        'ف','ق','ك','ل','م','ن',
        'ه','و','ي',
        'ة','ى',
        'ء','ئ','ؤ'
    };

    [ObservableProperty]
    private string plateText = string.Empty;

    public List<string> PlateChars => string.IsNullOrEmpty(PlateText)
        ? new List<string>()
        : PlateText.Select(c => c.ToString()).ToList();

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
        "لقة"
    };

    public Color PlateTextColor => PlateType switch
    {
        "خصوصي" => Colors.Black,
        "نقل عام" => Colors.Blue,
        "تجاري" => Colors.Red,
        "دبلوماسي" => Colors.Green,
        "لقة" => Colors.Orange,
        _ => Colors.Black
    };

    public Color PlateBorderColor => PlateType switch
    {
        "خصوصي" => Colors.Black,
        "نقل عام" => Colors.Blue,
        "تجاري" => Colors.Red,
        "دبلوماسي" => Colors.Green,
        "لقة" => Colors.Orange,
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
    private void KeyPress(string key)
    {
        if (IsProcessing || string.IsNullOrWhiteSpace(key))
            return;

        // Backspace
        if (key == BackspaceKey)
        {
            PlateText = PlateText[..^1];
            OnPropertyChanged(nameof(PlateChars));
            return;
        }

        if (key.Length != 1)
            return;

        char c = key[0];

        // English letter
        if (char.IsLetter(c) && c <= 127)
        {
            c = char.ToUpperInvariant(c);

            if (!EnglishToArabic.TryGetValue(c, out var textToAppend))
                return;

            // Validate every generated character
            if (!textToAppend.All(AllowedChars.Contains))
                return;

            // Respect max length
            if (PlateText.Length + textToAppend.Length > MaxPlateLength)
                return;

            PlateText += textToAppend;
        }
        else
        {
            if (!AllowedChars.Contains(c))
                return;

            if (PlateText.Length >= MaxPlateLength)
                return;

            if (EnglishToArabic.TryGetValue(c, out var arabic))
                PlateText += arabic;
            else
                PlateText += c;
        }

        OnPropertyChanged(nameof(PlateChars));
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
            var trimmed = PlateText.Trim();

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
        PlateText = string.Empty;
        OnPropertyChanged(nameof(PlateChars));
    }
}