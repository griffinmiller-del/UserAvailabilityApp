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


    // ==================================================
    // Click
    // ==================================================

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(User);
    }
}