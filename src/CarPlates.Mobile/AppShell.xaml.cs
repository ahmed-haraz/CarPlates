namespace CarPlates.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute("vehicle", typeof(Views.Vehicle.VehicleDetailsPage));
        Routing.RegisterRoute("profile", typeof(Views.Profile.ProfilePage));
        Routing.RegisterRoute("about", typeof(Views.About.AboutPage));
    }
}
