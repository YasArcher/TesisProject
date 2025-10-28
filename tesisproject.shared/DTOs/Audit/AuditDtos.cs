using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Audit;

public sealed class AuditEntryDto
{
    public int Id { get; set; }
    public DateTime WhenUtc { get; set; }
    public string UserId { get; set; } = default!;
    public string Action { get; set; } = default!; // Create/Update/Delete/Login/Logout/Export/etc
    public string Entity { get; set; } = default!; // Article, User, etc
    public string? Key { get; set; }              // Id afectado
    public string? Before { get; set; }           // JSON (opcional)
    public string? After { get; set; }            // JSON (opcional)
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}