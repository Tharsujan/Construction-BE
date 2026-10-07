namespace Construction.Application.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, string name, string role);
}
