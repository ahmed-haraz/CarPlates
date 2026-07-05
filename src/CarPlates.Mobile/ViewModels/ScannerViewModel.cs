using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Scanner.Commands;
using CarPlates.Domain.Enums;
using CarPlates.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CarPlates.Mobile.ViewModels;

public partial class ScannerViewModel : BaseViewModel
{
    private readonly IMediator _mediator;
    private readonly IPlateRecognitionService _plateRecognitionService;
    private readonly ICameraService _cameraService;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;

    private DateTime _lastScanTime = DateTime.MinValue;
    private readonly Queue<string> _recentPlates = new();

    [ObservableProperty]
    private bool _isScanning = true;

    [ObservableProperty]
    private bool _isTorchOn;

    [ObservableProperty]
    private bool _showVehicleInfo;

    [ObservableProperty]
    private VehicleDetailsDto? _vehicleInfo;

    [ObservableProperty]
    private string _detectedPlate = string.Empty;

    [ObservableProperty]
    private float _detectionConfidence;

    [ObservableProperty]
    private string _scanStatus = "Point camera at license plate";

    public ScannerViewModel(
        IMediator mediator,
        IPlateRecognitionService plateRecognitionService,
        ICameraService cameraService,
        ISettingsService settingsService,
        ILoggingService loggingService)
    {
        _mediator = mediator;
        _plateRecognitionService = plateRecognitionService;
        _cameraService = cameraService;
        _settingsService = settingsService;
        _loggingService = loggingService;
        Title = "Scan";
    }

    [RelayCommand]
    private async Task StartScanningAsync()
    {
        IsScanning = true;
        ShowVehicleInfo = false;
        ScanStatus = "Point camera at license plate";
        await _cameraService.StartPreviewAsync();
    }

    [RelayCommand]
    private async Task StopScanningAsync()
    {
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
        await _cameraService.SwitchCameraAsync();
    }

    [RelayCommand]
    private async Task ProcessFrameAsync(byte[] imageData)
    {
        if (!IsScanning || IsBusy) return;

        // Duplicate filter
        var minInterval = TimeSpan.FromSeconds(ScannerConstants.DuplicateFilterSeconds);
        if (DateTime.UtcNow - _lastScanTime < minInterval) return;

        try
        {
            using var stream = new MemoryStream(imageData);
            var result = await _plateRecognitionService.RecognizeAsync(stream);

            if (result.Success && result.PlateNumber != null)
            {
                var confidence = await _settingsService.GetOcrConfidenceAsync();
                if (result.PlateNumber.Confidence < confidence) return;

                // Check for duplicates in recent scans
                if (_recentPlates.Contains(result.PlateNumber.Value)) return;

                _lastScanTime = DateTime.UtcNow;
                _recentPlates.Enqueue(result.PlateNumber.Value);
                if (_recentPlates.Count > 10) _recentPlates.Dequeue();

                DetectedPlate = result.PlateNumber.Value;
                DetectionConfidence = result.PlateNumber.Confidence;
                ScanStatus = $"Detected: {DetectedPlate}";

                await ProcessScanAsync(result.PlateNumber);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Error processing frame");
        }
    }

    private async Task ProcessScanAsync(PlateNumber plateNumber)
    {
        await ExecuteAsync(async () =>
        {
            var command = new ScanVehicleCommand(
                plateNumber.Value,
                plateNumber.Type.ToString(),
                plateNumber.Confidence,
                null); // Photo path would be set by camera service

            var result = await _mediator.Send(command);

            if (result.Success && result.VehicleInfo != null)
            {
                VehicleInfo = result.VehicleInfo;
                ShowVehicleInfo = true;
                _loggingService.LogScanner(plateNumber.Value, plateNumber.Confidence, true);

                // Auto-resume if enabled
                var autoResume = await _settingsService.GetAutoResumeAsync();
                if (autoResume)
                {
                    await Task.Delay(3000);
                    ShowVehicleInfo = false;
                }
            }
            else
            {
                _loggingService.LogScanner(plateNumber.Value, plateNumber.Confidence, false);
                ScanStatus = result.ErrorMessage ?? "Vehicle not found";
            }
        });
    }

    [RelayCommand]
    private void DismissVehicleInfo()
    {
        ShowVehicleInfo = false;
        ScanStatus = "Point camera at license plate";
    }

    [RelayCommand]
    private async Task ManualEntryAsync()
    {
        // Navigate to manual plate entry or show entry dialog
        await Shell.Current.DisplayPromptAsync("Manual Entry", "Enter plate number:",
            accept: "Search", cancel: "Cancel");
    }
}
