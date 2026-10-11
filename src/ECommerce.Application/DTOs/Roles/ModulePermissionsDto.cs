namespace ECommerce.Application.DTOs.Roles;

public class ModulePermissionsDto
{
    public string Module { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
