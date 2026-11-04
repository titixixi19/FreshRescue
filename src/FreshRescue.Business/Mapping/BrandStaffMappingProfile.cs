using AutoMapper;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Catalog;

namespace FreshRescue.Business.Mapping;

public class BrandStaffMappingProfile : Profile
{
    public BrandStaffMappingProfile()
    {
        CreateMap<StoreStaff, BrandStaffDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.User.FullName))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.User.Email ?? string.Empty))
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.Store.Name));
    }
}
