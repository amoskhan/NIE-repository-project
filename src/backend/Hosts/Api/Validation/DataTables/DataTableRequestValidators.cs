using Application.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class DataTableFilterDtoValidator : AbstractValidator<DataTableFilterDto>
{
    public DataTableFilterDtoValidator()
    {
        RuleFor(filter => filter.Key)
            .NotEmpty()
            .MaximumLength(80);
        RuleFor(filter => filter.Values)
            .NotNull()
            .Must(values => values.Count <= 100)
            .WithMessage("A column filter supports at most 100 selected values.");
        RuleForEach(filter => filter.Values)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class DataTableSortDtoValidator : AbstractValidator<DataTableSortDto>
{
    public DataTableSortDtoValidator()
    {
        RuleFor(sort => sort.Key)
            .NotEmpty()
            .MaximumLength(80);
        RuleFor(sort => sort.Direction)
            .Must(direction => direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be asc or desc.");
    }
}

public class DataTableRequestDtoValidator : AbstractValidator<DataTableRequestDto>
{
    public DataTableRequestDtoValidator()
    {
        RuleFor(request => request.Search)
            .MaximumLength(200);
        RuleFor(request => request.SortBy)
            .MaximumLength(80);
        RuleFor(request => request.SortDirection)
            .Must(direction => direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be asc or desc.");
        RuleFor(request => request.Sorts)
            .NotNull()
            .Must(sorts => sorts.Count <= 5)
            .WithMessage("A data table supports at most five ordered sorts.")
            .Must(sorts => sorts.Select(sort => sort.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() == sorts.Count)
            .WithMessage("A data table cannot sort the same column more than once.");
        RuleForEach(request => request.Sorts)
            .SetValidator(new DataTableSortDtoValidator());
        RuleFor(request => request.Filters)
            .NotNull()
            .Must(filters => filters.Count <= 20)
            .WithMessage("A data table supports at most 20 column filters.");
        RuleForEach(request => request.Filters)
            .SetValidator(new DataTableFilterDtoValidator());
    }
}

public sealed class DataTableFilterOptionsRequestDtoValidator : AbstractValidator<DataTableFilterOptionsRequestDto>
{
    public DataTableFilterOptionsRequestDtoValidator()
    {
        Include(new DataTableRequestDtoValidator());
        RuleFor(request => request.ColumnKey)
            .NotEmpty()
            .MaximumLength(80);
        RuleFor(request => request.OptionSearch)
            .MaximumLength(200);
    }
}
