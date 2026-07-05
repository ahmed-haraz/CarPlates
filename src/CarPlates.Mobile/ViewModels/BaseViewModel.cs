using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarPlates.Mobile.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    protected async Task ExecuteAsync(Func<Task> action, string? errorMessage = null)
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = errorMessage ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string? errorMessage = null)
    {
        if (IsBusy) return default;

        IsBusy = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = errorMessage ?? ex.Message;
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
