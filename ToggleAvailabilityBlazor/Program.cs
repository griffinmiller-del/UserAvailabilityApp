using ToggleAvailabilityBlazor.Components;
using ToggleAvailabilityBlazor.Components.Graph;
using ToggleAvailabilityBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<UserDisplayService>();
builder.Services.AddScoped<OfficeHistoryGraphService>();
builder.Services.AddSingleton<AvailabilityService>();
builder.WebHost.UseUrls(
    "http://10.10.101.12:5036");
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();