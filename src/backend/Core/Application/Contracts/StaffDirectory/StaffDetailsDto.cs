using System.Text.Json.Serialization;

namespace Application.Contracts;

public class StaffDetailsDto
{
    public string WorkerId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string DepartmentDescription { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime? JoiningDate { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class StaffDetailsResponseDto
{
    [JsonPropertyName("a_WorkerId")]
    public string WorkerId { get; set; } = string.Empty;

    [JsonPropertyName("a_StaffName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("a_StaffDept")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("a_StaffDeptDesc")]
    public string DepartmentDescription { get; set; } = string.Empty;

    [JsonPropertyName("a_Userid")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("a_Email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("a_Designation")]
    public string Designation { get; set; } = string.Empty;

    [JsonPropertyName("a_JoiningDate")]
    public DateTime? JoiningDate { get; set; }

    [JsonPropertyName("a_Title")]
    public string Title { get; set; } = string.Empty;
}
