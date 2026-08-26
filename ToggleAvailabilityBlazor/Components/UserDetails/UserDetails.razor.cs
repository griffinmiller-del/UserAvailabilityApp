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

    private bool _closing;
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


    /// <summary>
    /// Handles when the range for the graph has been changed
    /// </summary>
    /// <param name="range">The new range chosen</param>
    /// <returns></returns>
    private async Task OnSelectedRangeChanged(
        GraphRange range)
    {
        _selectedRange = range;

        await InvokeAsync(
            StateHasChanged);
    }


    /// <summary>
    /// Handles when the start date is changed for the custom date range
    /// </summary>
    /// <param name="date">The start date chosen</param>
    /// <returns></returns>
    private async Task OnCustomStartDateChanged(
        DateOnly date)
    {
        _customStartDate = date;

        await InvokeAsync(
            StateHasChanged);
    }


    /// <summary>
    /// Handles when the end date is changed for the custom date range
    /// </summary>
    /// <param name="date">The end date chosen</param>
    /// <returns></returns>
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


    /// <summary>
    /// Handles closing user details
    /// </summary>
    /// <returns></returns>
    private async Task Close()
    {
        if (_closing)
        {
            return;
        }


        _closing = true;

        StateHasChanged();


        // Match the CSS animation duration.
        await Task.Delay(200);


        await OnClose.InvokeAsync();
    }
}