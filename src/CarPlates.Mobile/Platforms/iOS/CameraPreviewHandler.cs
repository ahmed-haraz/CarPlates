using CarPlates.Mobile.Controls;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CarPlates.Mobile.Handlers;

public partial class CameraPreviewHandler : ViewHandler<CameraPreview, UIView>
{
    public CameraPreviewHandler() : base(PropertyMapper, CommandMapper)
    {
    }

    public static IPropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper =
        new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewMapper);

    public static ICommandMapper<CameraPreview, CameraPreviewHandler> CommandMapper =
        new CommandMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewCommandMapper);

    protected override UIView CreatePlatformView()
    {
        return new UIView
        {
            BackgroundColor = UIColor.Black
        };
    }
}
