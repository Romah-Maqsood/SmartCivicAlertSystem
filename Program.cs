using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;
using SmartCityPulse.Services;



var builder = WebApplication.CreateBuilder(args);

// ==================== MongoDB ====================
builder.Services.AddSingleton<MongoDbContext>();


builder.Services.AddScoped<AICommentService>();

// ==================== AI Services (Citizen) ====================
builder.Services.AddScoped<AIVisionService>();

// ==================== Admin RAG Chatbot ====================
builder.Services.AddScoped<RAGService>();
builder.Services.AddScoped<GeminiService>();

// ==================== HttpClient for external APIs ====================
builder.Services.AddHttpClient();

// ==================== SignalR ====================
builder.Services.AddSignalR();

// ==================== Background Services ====================
builder.Services.AddHostedService<UnassignedIncidentService>();

// ==================== Session ====================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==================== MVC ====================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==================== Middleware ====================
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

// ==================== SignalR Hub ====================
app.MapHub<NotificationHub>("/notificationHub");

// ==================== Routing ====================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "citizen",
    pattern: "citizen/{action=Index}/{id?}",
    defaults: new { controller = "Citizen" });

app.Run();