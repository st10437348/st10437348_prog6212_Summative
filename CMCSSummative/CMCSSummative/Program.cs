using CMCSSummative.Data;
using CMCSSummative.Services;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(o => { o.Cookie.HttpOnly = true; o.IdleTimeout = TimeSpan.FromHours(4); });

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(conn));

builder.Services.Configure<ApprovalRulesOptions>(builder.Configuration.GetSection("ApprovalRules"));

builder.Services.AddScoped<ClaimValidationService>();

builder.Services.AddSingleton<FileEncryptionService>();
builder.Services.AddScoped<PdfReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
