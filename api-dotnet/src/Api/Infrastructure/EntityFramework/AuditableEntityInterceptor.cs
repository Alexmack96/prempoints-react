using global::Api.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.Infrastructure.EntityFramework;

public class AuditableEntityInterceptor(ICurrentUserService currentUserService, TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        // 1. Get the current User ID (Context might be null in background jobs)
        var userId = currentUserService.UserId;
        var utcNow = clock.GetUtcNow().UtcDateTime;

        // 2. Filter for only the things we care about
        var entries = context.ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                GuardAgainstEmptyId(entry);

                // Set Timestamp
                entry.Entity.CreatedAtUtc = utcNow;

                // Set Author. A handler may already have set this — UserProvisioner
                // points a new user at their own id — so only fill a blank.
                // If the Handler ALREADY set this (e.g. to the new user's own ID), 
                // this will be non-empty, so we skip it.
                // If the Handler left it empty (CreateTeam), we fill it.
                if (entry.Entity.CreatedBy == Guid.Empty && userId.HasValue)
                {
                    entry.Entity.CreatedBy = userId.Value;
                }

                // Initialize LastModified defaults
                entry.Entity.LastModifiedUtc = utcNow;
                if (entry.Entity.LastModifiedBy == Guid.Empty && userId.HasValue)
                {
                    entry.Entity.LastModifiedBy = userId.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Update Timestamp
                entry.Entity.LastModifiedUtc = utcNow;

                // Update Author
                if (userId.HasValue)
                {
                    entry.Entity.LastModifiedBy = userId.Value;
                }

                // Protect Creation Data
                // Ensure no one accidentally overwrites who created it or when
                entry.Property(p => p.CreatedAtUtc).IsModified = false;
                entry.Property(p => p.CreatedBy).IsModified = false;
            }
        }
    }

    /// <summary>
    /// Fails an insert whose primary key was never set.
    /// <para>
    /// OnModelCreating marks Guid ids on auditable entities
    /// ValueGenerated.Never, so nothing downstream will fill one in: EF sends
    /// Guid.Empty and SQL Server accepts it. The first such row per table
    /// succeeds and the second collides on the primary key, which surfaces far
    /// from the handler that forgot the id. Five handlers had this bug.
    /// </para>
    /// <para>
    /// This is the one place every insert passes through, so catching it here
    /// covers handlers that do not exist yet — which a test enumerating today's
    /// handlers would not.
    /// </para>
    /// </summary>
    private static void GuardAgainstEmptyId(EntityEntry<IAuditableEntity> entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");

        if (idProperty?.CurrentValue is Guid id && id == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"{entry.Entity.GetType().Name} was added with an empty Id. Auditable entities " +
                "have ValueGenerated.Never, so the handler must set it — use " +
                "Guid.CreateVersion7(clock.GetUtcNow()).");
        }
    }
}
public interface ICurrentUserService
{
    Guid? UserId { get; }
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var idClaim = httpContextAccessor.HttpContext?.User?.FindFirst("InternalUserId")?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}