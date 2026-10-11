using ECommerce.Application.DTOs.Users;
using FluentValidation;

namespace ECommerce.Application.Validators.Users;

public class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesDto>
{
    public UpdateUserRolesValidator()
    {
        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles list cannot be null.")
            .Must(r => r != null && r.Count > 0).WithMessage("User must be assigned at least one role.");
    }
}
