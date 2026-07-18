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
            new CreateScanRecordDto(request.PlateNumber, request.PlateType, request.Confidence, request.PhotoPath),
            cancellationToken);

        VehicleDetailsDto? vehicleInfo = null;
        if (lookupResult.Success)
        {
            vehicleInfo = new VehicleDetailsDto(
                request.PlateNumber,
                lookupResult.MakeName,
                lookupResult.ModelName,
                lookupResult.Color,
                lookupResult.CustomerName_En ?? lookupResult.CustomerName_Ar,
                null,
                DateTime.UtcNow,
                1,
                request.PhotoPath);
        }

        _loggingService.LogScanner(request.PlateNumber, request.Confidence, lookupResult.Success);

        return new ScanVehicleResult(true, scanDto, vehicleInfo, lookupResult.ErrorMessage);
    }
}