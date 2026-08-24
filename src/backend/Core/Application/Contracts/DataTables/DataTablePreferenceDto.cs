namespace Application.Contracts;

public sealed class DataTablePreferenceFilterDto
{
    public string Key { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
}

public sealed class DataTablePreferenceSettingsDto
{
    public int PageSize { get; set; } = 20;
    public List<DataTableSortDto> Sorts { get; set; } = [];
    public List<DataTablePreferenceFilterDto> Filters { get; set; } = [];
    public DateTimeOffset? FilterReminderAcknowledgedAtUtc { get; set; }
    public List<string> ColumnOrder { get; set; } = [];
    public List<string> HiddenColumns { get; set; } = [];
    public string Density { get; set; } = "comfortable";
    public string Appearance { get; set; } = "elevated";
}

public sealed class UpsertUserDataTablePreferenceDto
{
    public int DefinitionVersion { get; set; } = 1;
    public int? Revision { get; set; }
    public DataTablePreferenceSettingsDto Settings { get; set; } = new();
}

public sealed class UserDataTablePreferenceDto
{
    public string TableKey { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public int Revision { get; set; }
    public DataTablePreferenceSettingsDto Settings { get; set; } = new();
    public bool RepairRequired { get; set; }
    public List<string> RepairReasons { get; set; } = [];
}
