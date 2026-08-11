using global::Api.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.Infrastructure.EntityFramework;

public class AuditableEntityInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
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
        var utcNow = DateTime.UtcNow;

        // 2. Filter for only the things we care about
        var entries = context.ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Set Timestamp
                entry.Entity.CreatedAtUtc = utcNow;

                // Set Author (The "CreateUser" Logic)
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