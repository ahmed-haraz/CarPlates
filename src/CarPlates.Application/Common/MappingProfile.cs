using AutoMapper;
using CarPlates.Application.Common.DTOs;
using CarPlates.Domain.Entities;

namespace CarPlates.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ScanRecord, ScanRecordDto>();
        CreateMap<ScanRecord, ScanRecordListDto>();
        CreateMap<ScanRecord, RecentScanDto>();

        CreateMap<User, UserDto>();
    }
}
