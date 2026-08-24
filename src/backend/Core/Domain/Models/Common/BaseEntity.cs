using System.ComponentModel.DataAnnotations;
using Domain.Identifiers;

namespace Domain.Models;

//NOTE: Only inherit this into your model directly if you don't need timestamp fields,
//If not TimestampedEntity will already inherit this
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Uuid7.New();
}
