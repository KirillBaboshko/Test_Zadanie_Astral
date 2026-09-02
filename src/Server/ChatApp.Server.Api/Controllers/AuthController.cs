using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Server.Application.Commands.Auth;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IMediator mediator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return BadRequest(ModelState);
        }

        var command = new RegisterCommand(request.Username, request.Password);
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
            return Conflict(new { message = response.ErrorMessage });

        var authResponse = new AuthResponse
        {
            Token = response.Token!,
            UserId = response.UserId,
            Username = response.Username!
        };

        return CreatedAtAction(nameof(Register), authResponse);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return BadRequest(ModelState);
        }

        var command = new LoginCommand(request.Username, request.Password);
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
            return Unauthorized(new { message = response.ErrorMessage });

        var authResponse = new AuthResponse
        {
            Token = response.Token!,
            UserId = response.UserId,
            Username = response.Username!
        };

        return Ok(authResponse);
    }
}
