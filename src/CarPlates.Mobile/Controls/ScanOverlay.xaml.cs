namespace CarPlates.Mobile.Controls;

public partial class ScanOverlay : ContentView
{
    public ScanOverlay()
    {
        InitializeComponent();
        StartAnimation();
    }

    private void StartAnimation()
    {
        // Animate scan line moving up and down
        var animation = new Animation(v => ScanLine.TranslationY = v, 0, 200);
        animation.Commit(this, "ScanLineAnimation", length: 2000, repeat: () => true);
    }
}
