using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Scanner.Commands;
using MediatR;

namespace CarPlates.Application.Scanner.Handlers;

public class ScanVehicleCommandHandler(
    IScanRepository scanRepository,
    IVehicleLookupService vehicleLookupService,
    ILoggingService loggingService) : IRequestHandler<ScanVehicleCommand, ScanVehicleResult>
{
    private readonly IScanRepository _scanRepository = scanRepository;
    private readonly IVehicleLookupService _vehicleLookupService = vehicleLookupService;
    private readonly ILoggingService _loggingService = loggingService;

    public async Task<ScanVehicleResult> Handle(ScanVehicleCommand request, CancellationToken cancellationToken)
    {
        var lookupResult = await _vehicleLookupService.LookupAsync(request.PlateNumber, cancellationToken);

        // The API creates/enriches the scan record server-side (finding or
        // registering the vehicle), so the client just sends the raw scan data.
        var scanDto = await _scanRepository.CreateAsync(
            new CreateScanRecordDto(request.PlateNumber, request.PlateType, request.Confidence, request.PhotoPath),
            cancellationToken);

        VehicleDetailsDto? vehicleInfo = null;
        if (lookupResult.Found)
        {
            vehicleInfo = new VehicleDetailsDto(
                request.PlateNumber,
                lookupResult.Brand,
                lookupResult.Model,
                lookupResult.Color,
                lookupResult.OwnerName,
                lookupResult.AccessStatus,
                DateTime.UtcNow,
                1,
                request.PhotoPath);
        }

        _loggingService.LogScanner(request.PlateNumber, request.Confidence, lookupResult.Found);

        return new ScanVehicleResult(true, scanDto, vehicleInfo, lookupResult.ErrorMessage);
    }
}
