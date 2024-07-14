using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace DTN.EFCore.AdvancedExtensions.Logging
{
    public class AuditTrailManager
    {
        private List<AuditEntry> _auditEntries = new List<AuditEntry>();

        public void CaptureChanges(ChangeTracker changeTracker)
        {
            foreach (var entry in changeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    var auditEntry = new AuditEntry
                    {
                        EntityName = entry.Entity.GetType().Name,
                        Action = entry.State.ToString(),
                        Timestamp = DateTime.UtcNow,
                        Changes = new Dictionary<string, object>()
                    };

                    foreach (var property in entry.Properties)
                    {
                        if (entry.State == EntityState.Added || property.IsModified)
                        {
                            auditEntry.Changes[property.Metadata.Name] = new
                            {
                                OldValue = entry.State == EntityState.Added ? null : property.OriginalValue,
                                NewValue = property.CurrentValue
                            };
                        }
                    }

                    _auditEntries.Add(auditEntry);
                }
            }
        }

        public async Task SaveAuditTrailAsync(DbContext context)
        {
            if (!_auditEntries.Any()) return;

            foreach (var auditEntry in _auditEntries)
            {
                context.Add(new AuditLog
                {
                    EntityName = auditEntry.EntityName,
                    Action = auditEntry.Action,
                    Timestamp = auditEntry.Timestamp,
                    Changes = JsonSerializer.Serialize(auditEntry.Changes)
                });
            }

            await context.SaveChangesAsync();
            _auditEntries.Clear();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(DbContext context, string entityName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = context.Set<AuditLog>().AsQueryable();

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(log => log.EntityName == entityName);
            }

            if (startDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(log => log.Timestamp <= endDate.Value);
            }

            return await query.OrderByDescending(log => log.Timestamp).ToListAsync();
        }
    }

    public class AuditEntry
    {
        public string EntityName { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Changes { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string EntityName { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string Changes { get; set; }
    }
}
