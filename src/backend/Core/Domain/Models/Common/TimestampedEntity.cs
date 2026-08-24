using System.Text.Json.Serialization;

namespace Domain.Models;

//Inherit this in your model, if your model needs the below columns to be added to your table
//If not use BaseEntity
public abstract class TimestampedEntity : BaseEntity
{
    [JsonIgnore]
    public DateTime? CreatedOn { get; set; }

    [JsonIgnore]
    public string? CreatedBy { get; set; }

    [JsonIgnore]
    public DateTime? UpdatedOn { get; set; }

    [JsonIgnore]
    public string? UpdatedBy { get; set; }
}
