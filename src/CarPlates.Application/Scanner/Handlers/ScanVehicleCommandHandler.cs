using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Scanner.Commands;
using MediatR;

namespace CarPlates.Application.Scanner.Handlers;

public class ScanVehicleCommandHandler(
    IScanRepository scanRepository,
    ICustomerCarLookupService customerCarLookupService,
    IAuthenticationService authenticationService,
    ILoggingService loggingService) : IRequestHandler<ScanVehicleCommand, ScanVehicleResult>
{
    private readonly IScanRepository _scanRepository = scanRepository;
    private readonly ICustomerCarLookupService _customerCarLookupService = customerCarLookupService;
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly ILoggingService _loggingService = loggingService;

    public async Task<ScanVehicleResult> Handle(ScanVehicleCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticationService.GetCurrentUserAsync(cancellationToken);

        var scanRequest = new CustomerCarScanRequest(
            PlateNumber: request.PlateNumber,
            BranchID: currentUser?.BranchId ?? 0);

        var lookupResult = await _customerCarLookupService.ScanAsync(scanRequest, cancellationToken);

        var scanDto = await _scanRepository.CreateAsync(
            new CreateScanRecordDto(
                request.PlateNumber,
                request.PlateType,
                request.Confidence,
                request.PhotoPath,
                currentUser?.BranchId ?? 0,
                request.Latitude,
                request.Longitude,
                request.Notes),
            cancellationToken);

        VehicleDetailsDto? vehicleInfo = null;
        if (lookupResult.Success)
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var brand = isRtl ? (lookupResult.MakeName_Ar ?? lookupResult.MakeName_En ?? lookupResult.MakeName)
                              : (lookupResult.MakeName_En ?? lookupResult.MakeName_Ar ?? lookupResult.MakeName);
            var model = isRtl ? (lookupResult.ModelName_Ar ?? lookupResult.ModelName_En ?? lookupResult.ModelName)
                              : (lookupResult.ModelName_En ?? lookupResult.ModelName_Ar ?? lookupResult.ModelName);
            var owner = isRtl ? (lookupResult.CustomerName_Ar ?? lookupResult.CustomerName_En)
                              : (lookupResult.CustomerName_En ?? lookupResult.CustomerName_Ar);

            vehicleInfo = new VehicleDetailsDto(
                request.PlateNumber,
                brand,
                model,
                lookupResult.Color,
                owner,
                null,
                DateTime.UtcNow,
                1,
                request.PhotoPath,
                request.PlateType,
                CarHeaderId: lookupResult.CarHeaderId);
            }

        _loggingService.LogScanner(request.PlateNumber, request.Confidence, lookupResult.Success);

        return new ScanVehicleResult(true, scanDto, vehicleInfo, lookupResult.ErrorMessage);
    }
}