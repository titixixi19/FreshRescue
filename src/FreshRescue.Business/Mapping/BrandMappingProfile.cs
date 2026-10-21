using AutoMapper;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Catalog;

namespace FreshRescue.Business.Mapping;

public class BrandMappingProfile : Profile
{
    public BrandMappingProfile()
    {
        CreateMap<Brand, BrandProfileDto>();
        CreateMap<Brand, AdminBrandDto>()
    .ForMember(
        dest => dest.OwnerEmail,
        opt => opt.MapFrom(src => src.Owner.Email));
    }
}
