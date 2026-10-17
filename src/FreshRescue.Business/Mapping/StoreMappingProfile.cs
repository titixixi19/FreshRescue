using AutoMapper;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Catalog;

namespace FreshRescue.Business.Mapping;

public class StoreMappingProfile : Profile
{
    public StoreMappingProfile()
    {
        CreateMap<Store, StoreDto>();
    }
}
