namespace HBOperations.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
