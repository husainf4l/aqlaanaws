using Amazon.Extensions.NETCore.Setup;
using AqlaAwsS3Manager.Data;
using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

// Load local development secrets from .env (git-ignored).
// This is intended for local dev; in production use real environment variables / secret manager.
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection(AwsOptions.SectionName));
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserS3CredentialStore, DataProtectionCredentialStore>();
builder.Services.AddScoped<IUserS3ClientFactory, UserS3ClientFactory>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IEmailSender, LogOnlyEmailSender>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

// Upload size limit: 5 GB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024;
});

// Rate limiting: 60 requests/minute per user/IP on mutating operations
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("upload", config =>
    {
        config.PermitLimit = 20;
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 4;
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0;
    });
    options.AddSlidingWindowLimiter("delete", config =>
    {
        config.PermitLimit = 60;
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 4;
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "font-src 'self'; " +
        "img-src 'self' data: blob:; " +
        "frame-src blob:; " +
        "connect-src 'self';";
    await next();
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var status = report.Status.ToString();
        var result = System.Text.Json.JsonSerializer.Serialize(new { status, totalDuration = report.TotalDuration.TotalMilliseconds });
        await context.Response.WriteAsync(result);
    }
});
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "db"
});
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
