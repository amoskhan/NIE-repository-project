using Application.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class DataTablePreferenceFilterDtoValidator : AbstractValidator<DataTablePreferenceFilterDto>
{
    public DataTablePreferenceFilterDtoValidator()
    {
        RuleFor(filter => filter.Key).NotEmpty().MaximumLength(80);
        RuleFor(filter => filter.Values)
            .NotNull()
            .Must(values => values.Count <= 100)
            .WithMessage("A saved filter supports at most 100 selected values.");
        RuleForEach(filter => filter.Values).NotEmpty().MaximumLength(200);
    }
}

public sealed class DataTablePreferenceSettingsDtoValidator : AbstractValidator<DataTablePreferenceSettingsDto>
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100];
    private static readonly string[] AllowedDensities = ["compact", "comfortable", "spacious"];
    private static readonly string[] AllowedAppearances = ["elevated", "minimal", "striped"];

    public DataTablePreferenceSettingsDtoValidator()
    {
        RuleFor(settings => settings.PageSize).Must(AllowedPageSizes.Contains);
        RuleFor(settings => settings.Sorts)
            .NotNull()
            .Must(sorts => sorts.Count <= 5)
            .Must(sorts => sorts.Select(sort => sort.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() == sorts.Count);
        RuleForEach(settings => settings.Sorts).SetValidator(new DataTableSortDtoValidator());
        RuleFor(settings => settings.Filters)
            .NotNull()
            .Must(filters => filters.Count <= 20)
            .Must(filters => filters.Select(filter => filter.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() == filters.Count);
        RuleForEach(settings => settings.Filters).SetValidator(new DataTablePreferenceFilterDtoValidator());
        RuleFor(settings => settings.ColumnOrder)
            .NotNull()
            .Must(columns => columns.Count <= 80)
            .Must(columns => columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() == columns.Count);
        RuleForEach(settings => settings.ColumnOrder).NotEmpty().MaximumLength(80);
        RuleFor(settings => settings.HiddenColumns)
            .NotNull()
            .Must(columns => columns.Count <= 80)
            .Must(columns => columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() == columns.Count);
        RuleForEach(settings => settings.HiddenColumns).NotEmpty().MaximumLength(80);
        RuleFor(settings => settings.Density).Must(AllowedDensities.Contains);
        RuleFor(settings => settings.Appearance).Must(AllowedAppearances.Contains);
    }
}

public sealed class UpsertUserDataTablePreferenceDtoValidator : AbstractValidator<UpsertUserDataTablePreferenceDto>
{
    public UpsertUserDataTablePreferenceDtoValidator()
    {
        RuleFor(request => request.DefinitionVersion).InclusiveBetween(1, 10_000);
        RuleFor(request => request.Revision).GreaterThan(0).When(request => request.Revision.HasValue);
        RuleFor(request => request.Settings)
            .NotNull()
            .SetValidator(new DataTablePreferenceSettingsDtoValidator());
    }
}
