# Developer Guide — How This App Is Wired Together

This is the practical companion to `Architecture.md`. That file tells you *what* the layers
are. This one tells you *where to click* when you want to change something.

If you only read one section, read **"Adding a new screen"** — it covers 90% of what you'll
do day to day.

---

## 1. The four layers, in plain words

```
CarPlates.Mobile          <- screens (XAML) + ViewModels. What the user sees and taps.
        |
        v  (ViewModel calls _mediator.Send(someQuery))
CarPlates.Application      <- "what to do". One Query/Command + one Handler per action.
        |
        v  (Handler calls an interface, e.g. IVehicleLookupService)
CarPlates.Infrastructure    <- "how to do it". Talks to the real API or the local SQLite db.
        |
        v
The outside world           <- CarPlates.API server, or the phone's SQLite file
```

**The one rule that matters most:** a ViewModel never calls Infrastructure directly, and a
Handler never knows if it's talking to the network or to SQLite. Everything passes through an
interface. This is why you can, for example, swap `VehicleLookupService` for a fake one in
tests without touching a single ViewModel.

---

## 2. Adding a new screen (the full recipe)

Say you want a new "Notifications" screen that calls a new API endpoint.

### Step 1 — Define the request
`src/CarPlates.Application/Notifications/Queries/GetNotifications.cs`
```csharp
public record GetNotificationsQuery() : IRequest<List<NotificationDto>>;
```
This is just a data holder. No logic here.

### Step 2 — Write the handler
`src/CarPlates.Application/Notifications/Handlers/GetNotificationsHandler.cs`
```csharp
public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly INotificationService _notificationService; // interface, not a concrete class

    public GetNotificationsHandler(INotificationService notificationService)
        => _notificationService = notificationService;

    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
        => await _notificationService.GetAllAsync(ct);
}
```

### Step 3 — Define + implement the interface (skip if reusing an existing one)
Interface goes in `src/CarPlates.Application/Common/Interfaces/INotificationService.cs`:
```csharp
public interface INotificationService
{
    Task<List<NotificationDto>> GetAllAsync(CancellationToken ct = default);
}
```
Implementation goes in `src/CarPlates.Infrastructure/Api/NotificationService.cs`:
```csharp
public class NotificationService(IHttpClientFactory httpClientFactory) : INotificationService
{
    private HttpClient Client => httpClientFactory.CreateClient("CarPlatesApi");

    public async Task<List<NotificationDto>> GetAllAsync(CancellationToken ct = default)
        => await Client.GetFromJsonAsync<List<NotificationDto>>("notifications", ct) ?? [];
}
```
`"CarPlatesApi"` is the pre-configured `HttpClient` — base URL, timeout, and the auth token
handler are already attached. You never set headers or tokens by hand.

### Step 4 — Register the new service in DI
Open `src/CarPlates.Infrastructure/DependencyInjection/ServiceRegistration.cs` and add one line:
```csharp
services.AddScoped<INotificationService, NotificationService>();
```

### Step 5 — Build the ViewModel
`src/CarPlates.Mobile/ViewModels/NotificationsViewModel.cs`
```csharp
public partial class NotificationsViewModel : BaseViewModel
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private List<NotificationDto> _notifications = [];

    public NotificationsViewModel(IMediator mediator, INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        Title = "Notifications";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            Notifications = await _mediator.Send(new GetNotificationsQuery());
        });
    }
}
```
`ExecuteAsync` (inherited from `BaseViewModel`) automatically manages the busy spinner and
catches errors for you — always wrap your work in it.

### Step 6 — Build the View
`src/CarPlates.Mobile/Views/Notifications/NotificationsPage.xaml`:
```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:CarPlates.Mobile.ViewModels"
             x:Class="CarPlates.Mobile.Views.Notifications.NotificationsPage"
             x:DataType="vm:NotificationsViewModel"
             Title="{Binding Title}">
    <CollectionView ItemsSource="{Binding Notifications}" />
</ContentPage>
```
`src/CarPlates.Mobile/Views/Notifications/NotificationsPage.xaml.cs`:
```csharp
public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
```

Then register **both** the page and the ViewModel in `MauiProgram.cs`:
```csharp
builder.Services.AddTransient<NotificationsViewModel>();
builder.Services.AddTransient<NotificationsPage>();
```

> **Naming rule to never break:** `NavigationService` finds the right page by stripping
> "ViewModel" off your class name and looking for a page with that name. `NotificationsViewModel`
> must be paired with a class literally named `NotificationsPage` — get the name wrong and
> navigation throws at runtime with a clear error telling you what it expected.

### Step 7 — Navigate to it
From any other ViewModel that already has `Navigation` (inherited from `BaseViewModel`):
```csharp
await Navigation.PushAsync<NotificationsViewModel>();
```
Need to pass data in? Pass a dictionary, and read it in the target ViewModel via
`IQueryAttributable.ApplyQueryAttributes` (see `VehicleDetailsViewModel.cs` for a working example):
```csharp
await Navigation.PushAsync<VehicleDetailsViewModel>(new Dictionary<string, object>
{
    ["plateNumber"] = "ABC123"
});
```

That's it — seven steps, and steps 3–4 only apply if you're adding a brand-new API call rather
than reusing an existing service.

---

## 3. Editing an existing screen

You almost never need to touch more than two files:

- **Change what's on screen visually** → edit the `.xaml` file only (`Views/{Feature}/...Page.xaml`).
- **Change what happens when a button is tapped, or what data loads** → edit the `.cs`
  ViewModel (`ViewModels/...ViewModel.cs`). Look for the `[RelayCommand]` method matching the
  `Command="{Binding YourCommand}"` in the XAML.
- **Change business logic / what a query returns** → edit the `Handler` in
  `Application/{Feature}/Handlers/`.
- **Change how data is fetched (API endpoint, SQL query)** → edit the implementation in
  `Infrastructure/Api/` or `Infrastructure/Storage/`.

Rule of thumb: work outside-in. Start at the XAML, follow the `Binding` to the ViewModel command,
follow the query/command it sends to the Handler, follow the interface call to Infrastructure.

---

## 4. Calling the API — cheat sheet

- All HTTP calls go through `IHttpClientFactory.CreateClient("CarPlatesApi")`. Never `new HttpClient()`.
- Auth token attachment is automatic via `AuthDelegatingHandler` — don't add `Authorization`
  headers yourself.
- The API base URL is stored in `IApiUrlProvider` and can be changed at runtime from the
  Settings screen — no restart needed.
- Always accept a `CancellationToken` and pass it down; MediatR pipeline behaviors and page
  navigation rely on cancellation working correctly.

---

## 5. Common gotchas

| Symptom | Likely cause |
|---|---|
| `PushAsync<TViewModel>` throws "No page found matching naming convention" | Your Page class name doesn't match `{ViewModel name minus "ViewModel"} + "Page"`. |
| ViewModel property changes don't update the UI | Property isn't marked `[ObservableProperty]`, or you're setting the backing field instead of the generated property. |
| A screen silently shows no data, no error | Something threw inside the `ExecuteAsync(...)` block — it's caught silently by design (see `BaseViewModel.ExecuteAsync`). Check the Debug output / logs for `[BaseViewModel] Unhandled error:`. |
| New service isn't found at runtime ("Unable to resolve service") | You wrote the interface + implementation but forgot Step 4 — register it in `ServiceRegistration.cs`. |
| SQLite query throws `NullReferenceException` deep in `sqlite-net` | Avoid `!someBoolProperty` inside a `.Where()` lambda — sqlite-net's expression compiler chokes on unary negation. Use `someBoolProperty == false` instead. |

---

## 6. Where things live (quick index)

| I want to... | Go to |
|---|---|
| Add a new screen | `Mobile/Views/{Feature}/`, `Mobile/ViewModels/` |
| Add a new API-backed action | `Application/{Feature}/Queries` or `Commands`, `Application/{Feature}/Handlers` |
| Add a new API client call | `Infrastructure/Api/` |
| Add/change local database logic | `Infrastructure/Storage/` |
| Register a new service for DI | `Infrastructure/DependencyInjection/ServiceRegistration.cs` |
| Register a new page/ViewModel | `Mobile/MauiProgram.cs` |
| Change navigation behavior | `Mobile/Navigation/NavigationService.cs` |
| Change validation rules for a command | `Application/{Feature}/Validation/` (FluentValidation) |
| Change DTO-to-entity mapping | AutoMapper profile under `Application/Common/` |
