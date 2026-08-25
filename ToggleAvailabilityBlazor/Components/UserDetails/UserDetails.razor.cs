using Microsoft.AspNetCore.Components;
using ToggleAvailabilityBlazor.Models;
using ToggleAvailabilityBlazor.Services;

namespace ToggleAvailabilityBlazor.Components.UserDetails;

public partial class UserDetails : ComponentBase
{
    // ==================================================
    // Services
    // ==================================================

    [Inject]
    protected UserDisplayService UserDisplayService { get; set; } = null!;


    // ==================================================
    // Parameters
    // ==================================================

    [Parameter]
    public User User { get; set; } = null!;


    [Parameter]
    public List<OfficeHistory> History { get; set; } = [];


    [Parameter]
    public EventCallback OnClose { get; set; }


    // ==================================================
    // Graph State
    // ==================================================

    private GraphRange _selectedRange =
        GraphRange.Week;


    private DateOnly _customStartDate =
        DateOnly.FromDateTime(
            DateTime.Now.AddDays(-6));


    private DateOnly _customEndDate =
        DateOnly.FromDateTime(
            DateTime.Now);


    // ==================================================
    // Initialization
    // ==================================================

    protected override void OnParametersSet()
    {
        /*
         * Reset the graph to the default range whenever
         * a different user is opened.
         */
    }


    // ==================================================
    // Graph Range Changed
    // ==================================================

    private async Task OnSelectedRangeChanged(
        GraphRange range)
    {
        _selectedRange = range;

        await InvokeAsync(
            StateHasChanged);
    }


    // ==================================================
    // Custom Start Date Changed
    // ==================================================

    private async Task OnCustomStartDateChanged(
        DateOnly date)
    {
        _customStartDate = date;

        await InvokeAsync(
            StateHasChanged);
    }


    // ==================================================
    // Custom End Date Changed
    // ==================================================

    private async Task OnCustomEndDateChanged(
        DateOnly date)
    {
        _customEndDate = date;

        await InvokeAsync(
            StateHasChanged);
    }


    // ==================================================
    // Overlay Click
    // ==================================================

    private async Task HandleOverlayClick()
    {
        await Close();
    }


    // ==================================================
    // Close
    // ==================================================

    private async Task Close()
    {
        await OnClose.InvokeAsync();
    }
}