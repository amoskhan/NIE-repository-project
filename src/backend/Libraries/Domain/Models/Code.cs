namespace Domain.Models;

//NOTE: Use this model for maintaining codes needed for your application
//Try not to change the properties mostly until for dire need to keep it universal across projects
public class Code : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Type { get; set; } = default!;

    public string? Description { get; set; }

    public string DisplayName { get; set; } = default!;
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
