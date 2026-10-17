using AutoMapper;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Catalog;

namespace FreshRescue.Business.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(
                dest => dest.Name,
                opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(
                dest => dest.Slug,
                opt => opt.MapFrom(src => src.Slug.Trim().ToLowerInvariant()));
    }
}
