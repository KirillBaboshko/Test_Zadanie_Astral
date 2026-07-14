using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.SendMessage;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly SendMessageUseCase _sendMessageUseCase;
    private readonly GetMessagesUseCase _getMessagesUseCase;

    public ChatController(
        SendMessageUseCase sendMessageUseCase,
        GetMessagesUseCase getMessagesUseCase)
    {
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _getMessagesUseCase = getMessagesUseCase ?? throw new ArgumentNullException(nameof(getMessagesUseCase));
    }

    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var message = await _sendMessageUseCase.ExecuteAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetMessages), new { since = message.Timestamp }, message);
    }

    [HttpGet("messages")]
    [ProducesResponseType(typeof(GetMessagesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetMessagesResponse>> GetMessages(
        [FromQuery] DateTime? since = null,
        [FromQuery] Int32 limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 1000)
            return BadRequest("Limit должен быть от 1 до 1000");

        var response = await _getMessagesUseCase.ExecuteAsync(since, limit, cancellationToken);

        return Ok(response);
    }
}
