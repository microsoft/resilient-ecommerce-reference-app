using Api.Models.DTO;
using Api.Models.Entities;
using AutoMapper;

namespace Api.Services
{
    /// <summary>
    /// AutoMapper configuration for mapping between DTOs and entities.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserRequest, User>();
            CreateMap<User, UserRequest>();

            CreateMap<UserDto, User>();
            CreateMap<User, UserDto>();

            CreateMap<ConcertDto, Concert>();
            CreateMap<Concert, ConcertDto>();

            CreateMap<TicketDto, Ticket>();
            CreateMap<Ticket, TicketDto>();

            CreateMap<OrderDto, Order>();
            CreateMap<Order, OrderDto>();

            CreateMap<KeyValuePair<string, int>, CartItemDto>()
                .ConstructUsing(entry => new CartItemDto { ConcertId = entry.Key, Quantity = entry.Value });
        }
    }
}
