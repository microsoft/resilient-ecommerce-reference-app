using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Api.Telemetry;
using Api.Services.Repositories;
using Api.Models.DTO;
using Api.Services.Repositories.Exceptions;
using AutoMapper;
using Api.Models.Entities;
using Api.Models.Common;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;

        public UsersController(ILogger<UsersController> logger, IMapper mapper, IUserRepository userRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet("{userId}", Name = "GetUser")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> GetUserByIdAsync(string userId)
        {
            try
            {
                var userEntity = await _userRepository.GetUserByIdAsync(userId);
                return Ok(_mapper.Map<UserDto>(userEntity));
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, "Client requested a nonexistent user with ID '{}'", userId);
                return NotFound(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(GetUserByIdAsync));
                return Problem(new ErrorResponse(ex).Serialize());
            }
        }

        [HttpPost(Name = "CreateUser")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserRequest request)
        {
            try
            {
                var userEntity = await _userRepository.CreateUserAsync(_mapper.Map<User>(request));
                return CreatedAtAction(nameof(GetUserByIdAsync), new { userId = userEntity.Id }, _mapper.Map<UserDto>(userEntity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(CreateUserAsync));
                return Problem(new ErrorResponse(ex).Serialize());
            }
        }

        [HttpPatch("{userId}", Name = "UpdateUser")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
        public async Task<IActionResult> UpdateUserAsync([FromRoute] string userId, [FromBody] UserRequest request)
        {
            try
            {
                var userEntity = _mapper.Map<User>(request);
                userEntity.Id = userId;

                await _userRepository.UpdateUserAsync(userEntity);
                return Accepted(_mapper.Map<UserDto>(userEntity));
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, "Client requested to update a nonexistent user with ID '{}'", userId);
                return NotFound(new ErrorResponse($"Invalid user ID. No user found with ID {userId}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error encountered during '{}' action", nameof(UpdateUserAsync));
                return Problem(new ErrorResponse(ex).Serialize());
            }
        }
    }
}
