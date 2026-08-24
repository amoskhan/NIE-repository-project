using Application.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        Include(new RoleFieldsValidator<CreateRoleDto>(
            request => request.Code,
            request => request.Name,
            request => request.Description,
            request => request.AccessFunctionIds));
    }
}

public sealed class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleDtoValidator()
    {
        RuleFor(request => request.Id).NotEmpty();
        Include(new RoleFieldsValidator<UpdateRoleDto>(
            request => request.Code,
            request => request.Name,
            request => request.Description,
            request => request.AccessFunctionIds));
    }
}

public sealed class AssignRoleDtoValidator : AbstractValidator<AssignRoleDto>
{
    public AssignRoleDtoValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(request => request.RoleId).NotEmpty();
    }
}

public sealed class AssignApplicationAccessDtoValidator : AbstractValidator<AssignApplicationAccessDto>
{
    public AssignApplicationAccessDtoValidator()
    {
        RuleFor(request => request.ApplicationId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(256);
        RuleFor(request => request.RoleId).NotEmpty();
    }
}

public sealed class AssignAccessDtoValidator : AbstractValidator<AssignAccessDto>
{
    public AssignAccessDtoValidator()
    {
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(256);
        RuleFor(request => request.Scope).IsInEnum();
        RuleFor(request => request.RoleIds)
            .NotEmpty()
            .Must(ids => ids.Count <= 20)
            .WithMessage("No more than 20 roles can be assigned at once.")
            .Must(ContainsOnlyUniqueNonEmptyIds)
            .WithMessage("Role identifiers must be unique, non-empty UUIDs.");
        RuleFor(request => request.ApplicationIds)
            .Must(ids => ids.Count <= 20)
            .WithMessage("No more than 20 applications can be assigned at once.")
            .Must(ContainsOnlyUniqueNonEmptyIds)
            .WithMessage("Application identifiers must be unique, non-empty UUIDs.");
        RuleFor(request => request.ApplicationIds)
            .NotEmpty()
            .When(request => request.Scope == AccessAssignmentScope.Application);
        RuleFor(request => request.ApplicationIds)
            .Empty()
            .When(request => request.Scope == AccessAssignmentScope.Global)
            .WithMessage("Global assignments must not include application identifiers.");
    }

    private static bool ContainsOnlyUniqueNonEmptyIds(List<Guid> ids) =>
        ids.All(id => id != Guid.Empty) && ids.Distinct().Count() == ids.Count;
}

internal sealed class RoleFieldsValidator<T> : AbstractValidator<T>
{
    public RoleFieldsValidator(
        System.Linq.Expressions.Expression<Func<T, string>> code,
        System.Linq.Expressions.Expression<Func<T, string>> name,
        System.Linq.Expressions.Expression<Func<T, string?>> description,
        System.Linq.Expressions.Expression<Func<T, List<Guid>>> accessFunctionIds)
    {
        RuleFor(code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z][A-Za-z0-9._-]*$")
            .WithMessage("Role code must start with a letter and contain only letters, numbers, dots, hyphens, or underscores.");
        RuleFor(name).NotEmpty().MaximumLength(200);
        RuleFor(description).MaximumLength(1_000);
        RuleFor(accessFunctionIds)
            .NotNull()
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Access function identifiers must be non-empty UUIDs.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Access function identifiers must not contain duplicates.");
    }
}
