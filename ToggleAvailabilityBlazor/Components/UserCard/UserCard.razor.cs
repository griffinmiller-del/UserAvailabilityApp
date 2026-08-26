using Microsoft.AspNetCore.Components;
using ToggleAvailabilityBlazor.Models;
using ToggleAvailabilityBlazor.Services;

namespace ToggleAvailabilityBlazor.Components.UserCard;

public partial class UserCard : ComponentBase
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
    public EventCallback<User> OnClick { get; set; }


    /// <summary>
    /// Handles when the user card is clicked
    /// </summary>
    /// <returns></returns>
    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(User);
    }
}