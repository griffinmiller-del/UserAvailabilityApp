using Microsoft.EntityFrameworkCore;
using ToggleAvailability.Server.Data;
using ToggleAvailability.Server.Hubs;
using ToggleAvailability.Server.Services;

var builder =
    WebApplication.CreateBuilder(args);


// ==================================================
// Database
// ==================================================

builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseSqlite(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));


// ==================================================
// Application Services
// ==================================================

builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<OfficeHistoryStore>();


// ==================================================
// SignalR
// ==================================================

builder.Services.AddSignalR();


// ==================================================
// Authentication
// ==================================================

builder.Services.AddSingleton<
    AdminAuthenticationService>();


// ==================================================
// Background Services
// ==================================================

builder.Services.AddHostedService<
    OfficeHistoryStartupService>();

builder.Services.AddHostedService<
    OfficeHistoryMidnightService>();


var app =
    builder.Build();


// ==================================================
// SignalR Hub
// ==================================================

app.MapHub<AvailabilityHub>(
    "/availability");


app.Run();