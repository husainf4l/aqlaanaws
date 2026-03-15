using AqlaAwsS3Manager.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AqlaAwsS3Manager.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditService(AppDbContext db, IHttpContextAccessor http, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _http = http;
        _userManager = userManager;
    }

    public async Task LogAsync(string action, string? resourceType = null, string? resourceKey = null, string? details = null, CancellationToken cancellationToken = default)
    {
        var user = _http.HttpContext?.User;
        var userId = user != null ? _userManager.GetUserId(user) : null;
        if (string.IsNullOrEmpty(userId)) userId = "anonymous";

        var ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        _db.AuditLog.Add(new AuditEntry
        {
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceKey = resourceKey != null && resourceKey.Length > 2000 ? resourceKey[..2000] : resourceKey,
            IpAddress = ip,
            Details = details != null && details.Length > 2000 ? details[..2000] : details
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
