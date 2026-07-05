using AutoMapper;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Application.Vehicle.Queries;
using MediatR;

namespace CarPlates.Application.Vehicle.Handlers;

public class GetVehicleDetailsHandler : IRequestHandler<GetVehicleDetailsQuery, VehicleDetailsDto?>
{
    private readonly IScanRepository _scanRepository;
    private readonly IVehicleLookupService _vehicleLookupService;

    public GetVehicleDetailsHandler(
        IScanRepository scanRepository,
        IVehicleLookupService vehicleLookupService)
    {
        _scanRepository = scanRepository;
        _vehicleLookupService = vehicleLookupService;
    }

    public async Task<VehicleDetailsDto?> Handle(GetVehicleDetailsQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _vehicleLookupService.LookupAsync(request.PlateNumber, cancellationToken);

        if (!lookup.Found) return null;

        var scans = await _scanRepository.GetByPlateNumberAsync(request.PlateNumber, cancellationToken);
        var totalScans = scans != null ? 1 : 0;

        return new VehicleDetailsDto(
            request.PlateNumber,
            lookup.Brand,
            lookup.Model,
            lookup.Color,
            lookup.OwnerName,
            lookup.AccessStatus,
            scans?.ScanTime,
            totalScans,
            scans?.PhotoPath);
    }
}
