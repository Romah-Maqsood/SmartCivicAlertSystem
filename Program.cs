using SmartCityPulse.Data;
using SmartCityPulse.Hubs;  // ✅ ADD THIS LINE

var builder = WebApplication.CreateBuilder(args);

// ✅ MongoDB Context - Simple way
builder.Services.AddSingleton<MongoDbContext>();  // Auto-detects IConfiguration

// Add SignalR
builder.Services.AddSignalR();

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

<<<<<<< HEAD
=======
// ✅ ADD THIS - SignalR Hub Mapping (Notification ke liye)
app.MapHub<NotificationHub>("/notificationHub");

>>>>>>> 331e1f62fcb331caaab5a32e0aefa4e34ba620ab
// ✅ Route Configuration - Fixed (No duplicates)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "citizen",
    pattern: "citizen/{action=Index}/{id?}",
    defaults: new { controller = "Citizen" });

app.Run();