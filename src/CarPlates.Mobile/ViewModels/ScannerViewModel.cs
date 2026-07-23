using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Scanner.Commands;
using CarPlates.Domain.ValueObjects;
using CarPlates.Mobile.Controls;
using CarPlates.Mobile.Helpers;
using CarPlates.Mobile.Localization;
using CarPlates.Mobile.Navigation;
using CarPlates.Shared.Constants;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

public partial class ScannerViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IPlateRecognitionService _plateRecognitionService;
    private readonly ICameraService _cameraService;
    private readonly IDocumentScannerService _documentScannerService;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;

    private DateTime _lastScanTime = DateTime.MinValue;
    private readonly Queue<string> _recentPlates = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isTorchOn;

    [ObservableProperty]
    private CameraFacing _cameraFacing = CameraFacing.Back;

    [ObservableProperty]
    private string _detectedPlate = string.Empty;

    [ObservableProperty]
    private float _detectionConfidence;

    [ObservableProperty]
    private string _scanStatus = AppResources.PointCameraAtPlate;

    public ScannerViewModel(
        IMediator mediator,
        IPlateRecognitionService plateRecognitionService,
        ICameraService cameraService,
        IDocumentScannerService documentScannerService,
        ISettingsService settingsService,
        ILoggingService loggingService,
        INavigationService navigation) : base(navigation)
    {
        _mediator = mediator;
        _plateRecognitionService = plateRecognitionService;
        _cameraService = cameraService;
        _documentScannerService = documentScannerService;
        _settingsService = settingsService;
        _loggingService = loggingService;
        Title = AppResources.Scan;
    }

    [RelayCommand]
    private async Task StartScanningAsync()
    {
        if (IsScanning) return;

        var hasCameraPermission = await PermissionHelper.RequestCameraPermissionAsync();
        if (!hasCameraPermission)
        {
            ScanStatus = AppResources.CameraPermissionRequired;
            _loggingService.LogWarning("Camera permission was denied when opening the scanner");
            return;
        }

        IsScanning = true;
        ScanStatus = AppResources.PointCameraAtPlate;
        await _cameraService.StartPreviewAsync();
    }

    [RelayCommand]
    private async Task StopScanningAsync()
    {
        if (!IsScanning) return;

        IsScanning = false;
        await _cameraService.StopPreviewAsync();
    }

    [RelayCommand]
    private async Task ToggleTorchAsync()
    {
        IsTorchOn = await _cameraService.ToggleTorchAsync();
    }

    [RelayCommand]
    private async Task SwitchCameraAsync()
    {
        var isFrontCamera = await _cameraService.SwitchCameraAsync();
        CameraFacing = isFrontCamera ? CameraFacing.Front : CameraFacing.Back;
    }

    [RelayCommand]
    private async Task ProcessFrameAsync(byte[] imageData)
    {
        if (!IsScanning || IsBusy) return;

        try
        {
            using var stream = new MemoryStream(imageData);
            var result = await _plateRecognitionService.RecognizeAsync(stream);
            await ProcessRecognitionResultAsync(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Error processing frame");
        }
    }

    [RelayCommand]
    private async Task ProcessRecognizedTextAsync(string? text)
    {
        if (!IsScanning || IsBusy || string.IsNullOrWhiteSpace(text)) return;

        try
        {
            var result = await _plateRecognitionService.RecognizeFromTextAsync(text);
            await ProcessRecognitionResultAsync(result);
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Error processing recognized text");
        }
    }

    private async Task ProcessRecognitionResultAsync(PlateRecognitionResult result)
    {
        if (!result.Success || result.PlateNumber == null) return;

        var minInterval = TimeSpan.FromSeconds(ScannerConstants.DuplicateFilterSeconds);
        if (DateTime.UtcNow - _lastScanTime < minInterval) return;

        var confidence = await _settingsService.GetOcrConfidenceAsync();
        if (result.PlateNumber.Confidence < confidence) return;

        if (_recentPlates.Contains(result.PlateNumber.Value)) return;

        _lastScanTime = DateTime.UtcNow;
        _recentPlates.Enqueue(result.PlateNumber.Value);
        if (_recentPlates.Count > 10) _recentPlates.Dequeue();

        DetectedPlate = result.PlateNumber.Value;
        DetectionConfidence = result.PlateNumber.Confidence;
        ScanStatus = string.Format(AppResources.DetectedFormat, DetectedPlate);

        await ProcessScanAsync(result.PlateNumber);
    }

    private async Task ProcessScanAsync(PlateNumber plateNumber)
    {
        // Hard gate: never call the lookup API unless the scanned/typed value
        // actually matches a recognized car-plate format.
        if (!_plateRecognitionService.IsValidPlate(plateNumber.Value))
        {
            ScanStatus = AppResources.InvalidPlateNumber;
            return;
        }

        await ExecuteAsync(async () =>
        {
            var command = new ScanVehicleCommand(
                plateNumber.Value,
                plateNumber.Type.ToString(),
                plateNumber.Confidence,
                null); // Photo path would be set by camera service

            var result = await _mediator.Send(command);

            await StopScanningAsync();

            if (result.Success && result.VehicleInfo != null)
            {
                _loggingService.LogScanner(plateNumber.Value, plateNumber.Confidence, true);
                await Navigation.PushAsync<VehicleDetailsViewModel>(new Dictionary<string, object>
                {
                    ["plateNumber"] = plateNumber.Value
                });
            }
            else
            {
                _loggingService.LogScanner(plateNumber.Value, plateNumber.Confidence, false);
                await Navigation.GoToCustomerDataAsync(plateNumber.Value);
            }
        });
    }

    [RelayCommand]
    private async Task ScanDocumentAsync()
    {
        if (IsBusy) return;

        var hasCameraPermission = await PermissionHelper.RequestCameraPermissionAsync();
        if (!hasCameraPermission)
        {
            ScanStatus = AppResources.CameraPermissionRequired;
            _loggingService.LogWarning("Camera permission was denied when opening the document scanner");
            return;
        }

        ScanStatus = AppResources.ScanningDocument;

        DocumentScanResult scanResult;
        try
        {
            scanResult = await _documentScannerService.ScanAsync();
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Error launching document scanner");
            ScanStatus = ex.Message;
            return;
        }

        if (!scanResult.Success || string.IsNullOrWhiteSpace(scanResult.RecognizedText))
        {
            // A null error message means the user simply cancelled the
            // scanner - don't show that as a failure.
            ScanStatus = scanResult.ErrorMessage ?? AppResources.PointCameraAtPlate;
            if (scanResult.ErrorMessage is not null)
            {
                _loggingService.LogWarning($"Document scan failed: {scanResult.ErrorMessage}");
            }
            return;
        }

        var recognitionResult = await _plateRecognitionService.RecognizeFromTextAsync(scanResult.RecognizedText);

        if (!recognitionResult.Success || recognitionResult.PlateNumber == null)
        {
            ScanStatus = AppResources.NoTextDetectedOnPlate;
            _loggingService.LogOcr(scanResult.RecognizedText, null, 0f);
            return;
        }

        DetectedPlate = recognitionResult.PlateNumber.Value;
        DetectionConfidence = recognitionResult.PlateNumber.Confidence;
        ScanStatus = string.Format(AppResources.DetectedFormat, DetectedPlate);

        // ProcessScanAsync owns its own busy/error state, since it performs
        // the actual vehicle-lookup API call.
        await ProcessScanAsync(recognitionResult.PlateNumber);
    }

    [RelayCommand]
    private async Task ManualEntryAsync()
    {
        if (IsBusy) return;

        await Navigation.GoToManualEntryAsync();
    }
}