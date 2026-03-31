namespace testASP.Models;

public sealed record User
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    // BCrypt хранит соль в хеше, поэтому отдельное поле для соли не нужно
    // public string PasswordSalt { get; init; } = string.Empty;
}
