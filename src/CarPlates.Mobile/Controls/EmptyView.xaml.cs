namespace CarPlates.Mobile.Controls;

public partial class EmptyView : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(EmptyView), "📭");

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(EmptyView), "No Data");

    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description), typeof(string), typeof(EmptyView), "There's nothing to show here yet");

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

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

    public EmptyView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(Icon)) IconLabel.Text = Icon;
        if (propertyName == nameof(Title)) TitleLabel.Text = Title;
        if (propertyName == nameof(Description)) DescriptionLabel.Text = Description;
    }
}
