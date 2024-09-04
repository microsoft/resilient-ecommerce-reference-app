using Api.Models.Common;
using Api.Models.DTO;
using Api.Services.Repositories;
using Api.Services.Repositories.Exceptions;
using Api.Telemetry;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Api.Controllers;

[ApiController]
[Route("api/users/{userId}/[controller]")]
public class CartsController : ControllerBase
{
    private readonly ILogger<CartsController> _logger;
    private readonly IMapper _mapper;
    private readonly ICartRepository _cartRepository;
    private readonly IConcertRepository _concertRepository;

    public CartsController(ILogger<CartsController> logger, IMapper mapper, ICartRepository cartRepository, IConcertRepository concertRepository)
    {
        _logger = logger;
        _mapper = mapper;
        _cartRepository = cartRepository;
        _concertRepository = concertRepository;
    }

    [HttpPut(Name = "AddToCart")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemCollectionResponse<CartItemDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<ActionResult> AddToCartAsync([FromRoute] string userId, [FromBody] CartItemDto request)
    {
        try
        {
            // Validates that the concert exists before adding it to the cart
            var concert = await _concertRepository.GetConcertByIdAsync(request.ConcertId);
            var updatedUserCart = await _cartRepository.UpdateCartAsync(userId, request.ConcertId, request.Quantity);

            return Ok(new ItemCollectionResponse<CartItemDto>() { Items = _mapper.Map<ICollection<CartItemDto>>(updatedUserCart) });
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Client requested a nonexistent concert with ID '{}'", request.ConcertId);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(AddToCartAsync));
            return Problem(new ErrorResponse(ex).Serialize());
        }
    }

    [HttpGet(Name = "GetCart")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemCollectionResponse<CartItemDto>))]
    public async Task<IActionResult> GetCartAsync(string userId)
    {
        try
        {
            var cart = await _cartRepository.GetCartAsync(userId);
            return Ok(new ItemCollectionResponse<CartItemDto>() { Items = _mapper.Map<ICollection<CartItemDto>>(cart) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(GetCartAsync));
            return Problem(new ErrorResponse(ex).Serialize());
        }
    }

    [HttpDelete(Name = "ClearCart")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCartAsync(string userId)
    {
        try
        {
            await _cartRepository.ClearCartAsync(userId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(ClearCartAsync));
            return Problem(new ErrorResponse(ex).Serialize());
        }
    }
}
