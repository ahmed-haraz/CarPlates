namespace CarPlates.Mobile.Controls;

public partial class ErrorView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ErrorView), "Something went wrong");

    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description), typeof(string), typeof(ErrorView), "Please try again later");

    public static readonly BindableProperty RetryCommandProperty = BindableProperty.Create(
        nameof(RetryCommand), typeof(ICommand), typeof(ErrorView), null);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public ErrorView()
    {
        InitializeComponent();
        RetryButton.Clicked += (s, e) => RetryCommand?.Execute(null);
    }
}
