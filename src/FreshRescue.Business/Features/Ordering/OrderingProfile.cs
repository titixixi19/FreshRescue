using AutoMapper;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Entities.Ordering;

namespace FreshRescue.Business.Features.Ordering;

/// <summary>Cần nạp kèm <c>Order.Store</c>, <c>Order.SaleBatch</c>, <c>Order.Items</c>.</summary>
public class OrderingProfile : Profile
{
    public OrderingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.Store.Name))
            .ForMember(d => d.StoreAddress, o => o.MapFrom(s => s.Store.AddressLine + ", " + s.Store.Ward + ", " + s.Store.District))
            .ForMember(d => d.PickupStartTime, o => o.MapFrom(s => s.SaleBatch.PickupStartTime))
            .ForMember(d => d.PickupEndTime, o => o.MapFrom(s => s.SaleBatch.PickupEndTime));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.OfferName, o => o.MapFrom(s => s.OfferNameSnapshot));
    }
}
