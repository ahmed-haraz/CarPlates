using CarPlates.Mobile.Controls;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CarPlates.Mobile.Platforms.iOS;

public partial class CameraPreviewHandler : ViewHandler<CameraPreview, UIView>
{
    public static IPropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper =
        new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewMapper);

    public CameraPreviewHandler()
        : base(PropertyMapper)
    {
    }

    protected override UIView CreatePlatformView()
    {
        return new UIView
        {
            BackgroundColor = UIColor.Black
        };
    }
}