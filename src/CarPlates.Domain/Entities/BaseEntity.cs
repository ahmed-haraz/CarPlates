using SQLite;

namespace CarPlates.Domain.Entities;

public abstract class BaseEntity
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public void MarkAsUpdated() => UpdatedAt = DateTime.UtcNow;
    public void MarkAsDeleted() => IsDeleted = true;
}
