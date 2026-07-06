using Microsoft.Maui.Handlers;
using CarPlates.Mobile.Controls;

namespace CarPlates.Mobile.Platforms.Android.Handlers;

public partial class CameraPreviewHandler
    : ViewHandler<CameraPreview, global::Android.Views.View>
{
    public CameraPreviewHandler() : base(PropertyMapper)
    {
    }

    public static IPropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper =
        new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewMapper);

    protected override global::Android.Views.View CreatePlatformView()
    {
        var view = new global::Android.Views.View(Context);
        view.SetBackgroundColor(global::Android.Graphics.Color.Black);
        return view;
    }

    protected override void ConnectHandler(global::Android.Views.View platformView)
    {
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(global::Android.Views.View platformView)
    {
        base.DisconnectHandler(platformView);
    }
}