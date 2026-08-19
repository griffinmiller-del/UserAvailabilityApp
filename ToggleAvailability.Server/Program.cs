using ToggleAvailability.Server.Hubs;
using ToggleAvailability.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHostedService<
    OfficeHistoryMidnightService>();
var app = builder.Build();

app.MapHub<AvailabilityHub>(
    "/availability");

app.Run();