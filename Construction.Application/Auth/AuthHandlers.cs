using Construction.Application.Common;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Auth;

public record RegisterCommand(RegisterRequest Request) : IRequest<ApiResult<AuthResponse>>;
public record LoginCommand(LoginRequest Request) : IRequest<ApiResult<AuthResponse>>;
public record GetMeQuery : IRequest<ApiResult<UserProfileDto>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResult<AuthResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenService _tokens;

    public RegisterCommandHandler(IAppDbContext db, IPasswordHasher passwords, IJwtTokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<ApiResult<AuthResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (exists)
        {
            return ApiResult<AuthResponse>.Fail("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = command.Request.Name.Trim(),
            Email = email,
            Phone = command.Request.Phone.Trim(),
            PasswordHash = _passwords.Hash(command.Request.Password),
            Role = UserRole.Client,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<AuthResponse>.Ok(ToAuth(user, _tokens));
    }

    internal static AuthResponse ToAuth(User user, IJwtTokenService tokens) => new(
        tokens.CreateToken(user.Id, user.Email, user.Name, user.Role.ToString()),
        user.Id,
        user.Name,
        user.Email,
        user.Role);
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResult<AuthResponse>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenService _tokens;

    public LoginCommandHandler(IAppDbContext db, IPasswordHasher passwords, IJwtTokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<ApiResult<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is null || !_passwords.Verify(command.Request.Password, user.PasswordHash))
        {
            return ApiResult<AuthResponse>.Fail("Email or password is incorrect.", ApiErrorKind.Unauthorized);
        }

        return ApiResult<AuthResponse>.Ok(RegisterCommandHandler.ToAuth(user, _tokens));
    }
}

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, ApiResult<UserProfileDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMeQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<UserProfileDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<UserProfileDto>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == _currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResult<UserProfileDto>.Fail("Account was not found.", ApiErrorKind.Unauthorized);
        }

        return ApiResult<UserProfileDto>.Ok(new UserProfileDto(user.Id, user.Name, user.Email, user.Phone, user.Role));
    }
}
