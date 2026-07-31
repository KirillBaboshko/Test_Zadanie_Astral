using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.Common;
using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.GetUserInfo;
using ChatApp.Server.Application.UseCases.GetUsers;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Server.Application.Validation;
using ChatApp.Server.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IUseCase<SendMessageUseCaseRequest, SendMessageUseCaseResponse> _sendMessageUseCase;
    private readonly GetMessagesUseCase _getMessagesUseCase;
    private readonly GetUsersUseCase _getUsersUseCase;
    private readonly GetUserInfoUseCase _getUserInfoUseCase;
    private readonly IValidator<SendMessageAuthRequest> _sendMessageAuthValidator;

    public ChatController(
        IUseCase<SendMessageUseCaseRequest, SendMessageUseCaseResponse> sendMessageUseCase,
        GetMessagesUseCase getMessagesUseCase,
        GetUsersUseCase getUsersUseCase,
        GetUserInfoUseCase getUserInfoUseCase,
        IValidator<SendMessageAuthRequest> sendMessageAuthValidator)
    {
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _getMessagesUseCase = getMessagesUseCase ?? throw new ArgumentNullException(nameof(getMessagesUseCase));
        _getUsersUseCase = getUsersUseCase ?? throw new ArgumentNullException(nameof(getUsersUseCase));
        _getUserInfoUseCase = getUserInfoUseCase ?? throw new ArgumentNullException(nameof(getUserInfoUseCase));
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

        // Создаем запрос для Use Case
        var useCaseRequest = new SendMessageUseCaseRequest
        {
            UserId = userId,
            Content = request.Content
        };

        // Выполняем Use Case с декораторами (Logging + UnitOfWork)
        var response = await _sendMessageUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

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

        var response = await _getMessagesUseCase.ExecuteAsync(since, limit, cancellationToken);

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

        var response = await _getMessagesUseCase.ExecuteForUserIdAsync(userId, limit, cancellationToken);

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

        var response = await _getMessagesUseCase.ExecuteForUsernameAsync(senderName, limit, cancellationToken);

        if (response == null)
            return NotFound(new { message = $"Пользователь '{senderName}' не найден" });

        return Ok(response);
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetUsers(CancellationToken cancellationToken = default)
    {
        var users = await _getUsersUseCase.ExecuteAsync(cancellationToken);
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

        var userInfo = await _getUserInfoUseCase.ExecuteAsync(username, cancellationToken);

        if (userInfo == null)
            return NotFound(new { message = $"Пользователь '{username}' не найден" });

        return Ok(userInfo);
    }
}
