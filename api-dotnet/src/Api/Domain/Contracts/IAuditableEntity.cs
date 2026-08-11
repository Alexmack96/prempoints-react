namespace Api.Domain.Contracts;

public interface IAuditableEntity
{
    Guid Id { get; set; }
    DateTime CreatedAtUtc { get; set; }
    DateTime LastModifiedUtc { get; set; }
    Guid CreatedBy { get; set; }
    Guid LastModifiedBy { get; set; }
}