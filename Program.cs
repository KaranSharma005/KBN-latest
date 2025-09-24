using KBN.Data;
using KBN.DependenciesMgt;
using KBN.Models;
using KBN.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddNotyf(config => {
    config.DurationInSeconds = 2;
    config.IsDismissable = true;
    config.Position = NotyfPosition.TopRight;
});

// Change from AddRazorPages to AddControllersWithViews
builder.Services.AddControllersWithViews();

builder.Services.Configure<EmailSettingsModal>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.RegisterServices();
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Change to controller action
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseNotyf();
app.UseRouting();

app.UseAuthentication(); // Add this - important for Identity
app.UseAuthorization();

// Map controllers instead of Razor Pages
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Did}/{action=DIDs}");

// Keep Identity UI as Razor Pages
app.MapRazorPages();

app.Run();