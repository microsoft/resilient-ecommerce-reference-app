using Api.Models.Common;
using Api.Models.DTO;
using Api.Services.Repositories;
using Api.Services.Repositories.Exceptions;
using Api.Telemetry;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    public class ConcertsController : ControllerBase
    {
        private readonly ILogger<ConcertsController> _logger;
        private readonly IMapper _mapper;
        private readonly IConcertRepository _concertRepository;

        public ConcertsController(ILogger<ConcertsController> logger,
                                  IMapper mapper,
                                  IConcertRepository concertRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _concertRepository = concertRepository;
        }

        [HttpGet(Name = "GetUpcomingConcerts")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemCollectionResponse<ConcertDto>))]
        public async Task<IActionResult> GetUpcomingConcerts([FromQuery] int take = 10)
        {
            try
            {
                var concerts = await _concertRepository.GetUpcomingConcertsAsync(take);
                return Ok(new ItemCollectionResponse<ConcertDto> { Items = _mapper.Map<ICollection<ConcertDto>>(concerts) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(GetUpcomingConcerts));
                return Problem(new ErrorResponse(ex).Serialize());
            }
        }

        [HttpGet("{concertId}", Name = "GetConcertById")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ConcertDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetConcertByIdAsync([FromRoute] string concertId)
        {
            try
            {
                var concert = await _concertRepository.GetConcertByIdAsync(concertId);
                return Ok(_mapper.Map<ConcertDto>(concert));
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, "Client requested a nonexistent concert with ID '{}'", concertId);
                return NotFound(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(GetConcertByIdAsync));
                return Problem(new ErrorResponse(ex).Serialize());
            }
        }
    }
}
