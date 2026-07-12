using AutoMapper;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Scanner.Commands;
using CarPlates.Domain.Entities;
using MediatR;

namespace CarPlates.Application.Scanner.Handlers;

public class ScanVehicleCommandHandler(
    IScanRepository scanRepository,
    IVehicleLookupService vehicleLookupService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILoggingService loggingService) : IRequestHandler<ScanVehicleCommand, ScanVehicleResult>
{
    private readonly IScanRepository _scanRepository = scanRepository;
    private readonly IVehicleLookupService _vehicleLookupService = vehicleLookupService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ILoggingService _loggingService = loggingService;

    public async Task<ScanVehicleResult> Handle(ScanVehicleCommand request, CancellationToken cancellationToken)
    {
        var scanRecord = ScanRecord.Create(
            request.PlateNumber,
            request.PlateType,
            request.Confidence,
            request.PhotoPath);

        await _scanRepository.AddAsync(scanRecord, cancellationToken);

        var lookupResult = await _vehicleLookupService.LookupAsync(request.PlateNumber, cancellationToken);

        VehicleDetailsDto? vehicleInfo = null;
        if (lookupResult.Found)
        {
            scanRecord.UpdateVehicleInfo(
                lookupResult.Brand,
                lookupResult.Model,
                lookupResult.Color,
                lookupResult.OwnerName,
                lookupResult.AccessStatus);

            await _scanRepository.UpdateAsync(scanRecord, cancellationToken);

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
        else
        {
            scanRecord.MarkAsFailed(lookupResult.ErrorMessage ?? "Vehicle not found");
            await _scanRepository.UpdateAsync(scanRecord, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var scanDto = _mapper.Map<ScanRecordDto>(scanRecord);
        _loggingService.LogScanner(request.PlateNumber, request.Confidence, lookupResult.Found);

        return new ScanVehicleResult(true, scanDto, vehicleInfo, lookupResult.ErrorMessage);
    }
}
