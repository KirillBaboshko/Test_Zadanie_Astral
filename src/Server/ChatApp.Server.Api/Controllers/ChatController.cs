using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.Commands.SendMessage;
using ChatApp.Server.Application.Queries.GetMessages;
using ChatApp.Server.Application.Queries.GetUserInfo;
using ChatApp.Server.Application.Queries.GetUsers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<SendMessageAuthRequest> _sendMessageAuthValidator;

    public ChatController(
        IMediator mediator,
        IValidator<SendMessageAuthRequest> sendMessageAuthValidator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _sendMessageAuthValidator = sendMessageAuthValidator ?? throw new ArgumentNullException(nameof(sendMessageAuthValidator));
    }

    [HttpPost("messages")]
    [Authorize]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(
        [FromBody] SendMessageAuthRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _sendMessageAuthValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return BadRequest(ModelState);
        }
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (String.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Не удалось определить пользователя из токена" });
        }

        // Создаем команду для MediatR
        var command = new SendMessageCommand(userId, request.Content);

        // Отправляем команду через MediatR (автоматически применяются Behaviors: Logging -> UnitOfWork)
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
            return NotFound(new { error = "Пользователь не найден" });

        // Формируем DTO для ответа
        var messageDto = new ChatMessageDto
        {
            Id = response.MessageId,
            SenderName = response.SenderName,
            Content = response.Content,
            Timestamp = response.Timestamp
        };

        return CreatedAtAction(nameof(GetMessages), new { since = messageDto.Timestamp }, messageDto);
    }

    [HttpGet("messages")]
    [ProducesResponseType(typeof(GetMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMessagesResponse>> GetMessages(
        [FromQuery] DateTime? since = null,
        [FromQuery] Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 1000)
        {
            ModelState.AddModelError(nameof(limit), "Лимит должен быть от 1 до 1000");
            return BadRequest(ModelState);
        }

        if (since.HasValue && since.Value > DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(since), "Дата 'since' не может быть в будущем");
            return BadRequest(ModelState);
        }

        var query = new GetMessagesQuery(since, limit);
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("messages/user/{userId}")]
    [ProducesResponseType(typeof(GetMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMessagesResponse>> GetMessagesByUserId(
        [FromRoute] Guid userId,
        [FromQuery] Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 1000)
        {
            ModelState.AddModelError(nameof(limit), "Лимит должен быть от 1 до 1000");
            return BadRequest(ModelState);
        }

        var query = new GetMessagesByUserQuery(userId, limit);
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("messages-for-name")]
    [ProducesResponseType(typeof(GetMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMessagesResponse>> GetMessagesByUsername(
        [FromQuery] String? senderName,
        [FromQuery] Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(senderName))
        {
            ModelState.AddModelError(nameof(senderName), "Имя пользователя не может быть пустым");
            return BadRequest(ModelState);
        }

        if (limit < 1 || limit > 1000)
        {
            ModelState.AddModelError(nameof(limit), "Лимит должен быть от 1 до 1000");
            return BadRequest(ModelState);
        }

        var query = new GetMessagesByUsernameQuery(senderName, limit);
        var response = await _mediator.Send(query, cancellationToken);

        if (response == null)
            return NotFound(new { message = $"Пользователь '{senderName}' не найден" });

        return Ok(response);
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetUsers(CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery();
        var users = await _mediator.Send(query, cancellationToken);
        return Ok(users);
    }

    [HttpGet("about-user/{username}")]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo(
        [FromRoute] String username,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            ModelState.AddModelError(nameof(username), "Имя пользователя не может быть пустым");
            return BadRequest(ModelState);
        }

        var query = new GetUserInfoQuery(username);
        var userInfo = await _mediator.Send(query, cancellationToken);

        if (userInfo == null)
            return NotFound(new { message = $"Пользователь '{username}' не найден" });

        return Ok(userInfo);
    }
}
