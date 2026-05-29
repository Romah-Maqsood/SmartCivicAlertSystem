//using SmartCityPulse.Data;
//using SmartCityPulse.Hubs;

//var builder = WebApplication.CreateBuilder(args);

//// MongoDB Context
//builder.Services.AddSingleton<MongoDbContext>();

//// Add SignalR
//builder.Services.AddSignalR();

//// Add Session support
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// Add MVC
//builder.Services.AddControllersWithViews();

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();

//app.UseSession();
//app.UseAuthorization();

//// SignalR Hub Mapping
//app.MapHub<NotificationHub>("/notificationHub");

//// Route Configuration
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapControllerRoute(
//    name: "citizen",
//    pattern: "citizen/{action=Index}/{id?}",
//    defaults: new { controller = "Citizen" });

//app.Run();



using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;

// FORCE the correct content root path BEFORE creating the builder
var contentRoot = @"D:\Documents\SmartCityPulse\SmartCityPulse";

// Set environment variable to override the path
Environment.SetEnvironmentVariable("ASPNETCORE_CONTENTROOT", contentRoot);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    WebRootPath = Path.Combine(contentRoot, "wwwroot")
});

// MongoDB Context
builder.Services.AddSingleton<MongoDbContext>();

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

// Configure the HTTP request pipeline
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

// SignalR Hub Mapping
app.MapHub<NotificationHub>("/notificationHub");

// Route Configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "citizen",
    pattern: "citizen/{action=Index}/{id?}",
    defaults: new { controller = "Citizen" });

app.Run();