using CarPlates.Application.Common.DTOs;
using MediatR;

namespace CarPlates.Application.Vehicle.Queries;

public record GetVehicleDetailsQuery(string PlateNumber) : IRequest<VehicleDetailsDto?>;
