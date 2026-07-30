namespace Identity.Application.Abstractions;

/// <summary>Hash/verificação de senha (impl na Infrastructure). Preserva o esquema do GearFlow.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
