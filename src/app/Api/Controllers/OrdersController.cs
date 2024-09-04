using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Api.Telemetry;
using Api.Services.Repositories;
using Api.Services;
using AutoMapper;
using Api.Models.DTO;
using Api.Models.Common;
using Api.Services.Repositories.Exceptions;

namespace OrderClient.Controllers;

[ApiController]
[Route("api/users/{userId}/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly TicketPurchasingService _ticketService;

    public OrdersController(ILogger<OrdersController> logger,
                            IMapper mapper,
                            IUserRepository userRepository,
                            IOrderRepository orderRepository,
                            TicketPurchasingService ticketService)
    {
        _logger = logger;
        _mapper = mapper;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _ticketService = ticketService;
    }

    [HttpPost(Name = "CreateOrder")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OrderDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> CreateOrderAsync([FromRoute] string userId, [FromBody] OrderPaymentDetails purchaseTicketRequest)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            var newOrder = await _ticketService.TryCheckoutTickets(userId);

            return CreatedAtAction(nameof(GetOrderByIdAsync), new { userId, orderId = newOrder.Id }, _mapper.Map<OrderDto>(newOrder));
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Client requested an order for a nonexistend user with ID '{}'", userId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid cart state for user with ID '{}'", userId);
            return BadRequest(new ErrorResponse(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(CreateOrderAsync));
            return Problem(new ErrorResponse(ex).Serialize());
        }
    }

    [HttpGet(Name = "GetAllOrdersForUser")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<OrderDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAllOrdersAsync([FromRoute] string userId, [FromQuery] int skip = 0, [FromQuery] int take = 5)
    {
        try
        {
            var orders = await _orderRepository.GetAllOrdersForUserAsync(userId, skip, take);

            return Ok(new PagedResponse<OrderDto>()
            {
                PageData = _mapper.Map<ICollection<OrderDto>>(orders.PageData),
                Skipped = orders.Skipped,
                PageSize = orders.PageSize,
                TotalCount = orders.TotalCount,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(GetAllOrdersAsync));
            return Problem(new ErrorResponse(ex).Serialize());
        }
    }

    [HttpGet("{orderId}", Name = "GetOrder")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetOrderByIdAsync([FromRoute] string orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            return Ok(_mapper.Map<OrderDto>(order));
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Client requested a nonexistent order with ID '{}'", orderId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(GetOrderByIdAsync));
            return Problem(new ErrorResponse(ex).Serialize());
        }
    }
}
