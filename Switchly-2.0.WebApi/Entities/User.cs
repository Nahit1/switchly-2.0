namespace Switchly_2._0.WebApi.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

}