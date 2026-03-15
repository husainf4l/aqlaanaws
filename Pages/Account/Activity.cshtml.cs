using AqlaAwsS3Manager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AqlaAwsS3Manager.Pages.Account;

[Authorize]
public class ActivityModel : PageModel
{
    private readonly AppDbContext _db;

    public ActivityModel(AppDbContext db) => _db = db;

    public List<AuditEntry> Entries { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }

    public async Task OnGetAsync(int p = 1)
    {
        PageNumber = Math.Max(1, p);
        TotalCount = await _db.AuditLog.CountAsync();
        Entries = await _db.AuditLog
            .OrderByDescending(e => e.At)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
