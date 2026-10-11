namespace ECommerce.Application.DTOs.Auth;

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
    public IReadOnlyList<string> Permissions { get; set; } = new List<string>();
}
