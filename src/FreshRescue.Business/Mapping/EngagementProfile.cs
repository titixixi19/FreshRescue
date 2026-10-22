using AutoMapper;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Engagement;

namespace FreshRescue.Business.Mapping;

/// <summary>
/// Cấu hình ánh xạ AutoMapper cho phân hệ tương tác (yêu thích cửa hàng và thông báo).
/// </summary>
public class EngagementProfile : Profile
{
    public EngagementProfile()
    {
        CreateMap<FavoriteStore, FavoriteStoreDto>()
            .ForMember(d => d.StoreId, opt => opt.MapFrom(s => s.StoreId))
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.Store.Name))
            .ForMember(d => d.AddressLine, opt => opt.MapFrom(s => s.Store.AddressLine))
            .ForMember(d => d.Ward, opt => opt.MapFrom(s => s.Store.Ward))
            .ForMember(d => d.District, opt => opt.MapFrom(s => s.Store.District))
            .ForMember(d => d.City, opt => opt.MapFrom(s => s.Store.City))
            .ForMember(d => d.BrandName, opt => opt.MapFrom(s => s.Store.Brand.Name))
            .ForMember(d => d.BrandLogoUrl, opt => opt.MapFrom(s => s.Store.Brand.LogoUrl))
            .ForMember(d => d.RatingAverage, opt => opt.MapFrom(s => s.Store.RatingAverage))
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedAt));

        CreateMap<Notification, NotificationDto>();
    }
}
