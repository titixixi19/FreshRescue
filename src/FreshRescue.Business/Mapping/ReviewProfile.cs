using AutoMapper;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Engagement;

namespace FreshRescue.Business.Mapping;

public sealed class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<Review, ReviewDto>();
    }
}
