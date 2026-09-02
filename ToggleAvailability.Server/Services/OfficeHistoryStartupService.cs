using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ToggleAvailability.Server.Services;

public class OfficeHistoryStartupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;


    public OfficeHistoryStartupService(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory =
            scopeFactory;
    }


    // ==================================================
    // Start
    // ==================================================

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        using IServiceScope scope =
            _scopeFactory.CreateScope();


        var initializer =
            scope.ServiceProvider
                .GetRequiredService<OfficeHistoryInitializer>();


        await initializer.InitializeAsync(
            cancellationToken);
    }


    // ==================================================
    // Stop
    // ==================================================

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}