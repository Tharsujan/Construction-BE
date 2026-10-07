using Construction.Domain.Enums;

namespace Construction.Application.Auth;

public record RegisterRequest(string Name, string Email, string Phone, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, Guid UserId, string Name, string Email, UserRole Role);

public record UserProfileDto(Guid Id, string Name, string Email, string Phone, UserRole Role);
