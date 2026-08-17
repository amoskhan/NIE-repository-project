using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

//NOTE: Only inherit this into your model directly if you don't need timestamp fields,
//If not TimestampedEntity will already inherit this
public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
}
