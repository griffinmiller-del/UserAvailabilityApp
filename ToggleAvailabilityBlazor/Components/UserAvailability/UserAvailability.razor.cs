using Microsoft.AspNetCore.Components;
using ToggleAvailabilityBlazor.Models;
using ToggleAvailabilityBlazor.Services;

namespace ToggleAvailabilityBlazor.Components.UserAvailability;

public partial class UserAvailability : ComponentBase, IDisposable
{
    // ==================================================
    // Services
    // ==================================================

    [Inject]
    private AvailabilityService AvailabilityService { get; set; } = default!;


    // ==================================================
    // State
    // ==================================================

    private User? _selectedUser;

    private List<User> _users = [];

    private List<OfficeHistory> _userHistory = [];

    private bool _disposed;

    private bool _initialized;


    // ==================================================
    // Timer
    // ==================================================

    private PeriodicTimer? _refreshTimer;

    private CancellationTokenSource? _refreshCancellation;


    // ==================================================
    // Initialization
    // ==================================================

    protected override async Task OnInitializedAsync()
    {
        if (_disposed || _initialized)
        {
            return;
        }

        _initialized = true;


        AvailabilityService.UserUpdated +=
            OnUserUpdated;

        AvailabilityService.UsersChanged +=
            OnUsersChanged;


        try
        {
            await AvailabilityService.ConnectAsync();


            if (_disposed)
            {
                return;
            }


            _users =
                AvailabilityService.Users
                    .Select(CloneUser)
                    .ToList();


            StartRefreshTimer();
        }
        catch (ObjectDisposedException)
        {
            // Component/circuit was disposed while connecting.
        }
    }


    // ==================================================
    // Start Refresh Timer
    // ==================================================

    private void StartRefreshTimer()
    {
        if (_disposed)
        {
            return;
        }


        _refreshCancellation =
            new CancellationTokenSource();


        _refreshTimer =
            new PeriodicTimer(
                TimeSpan.FromSeconds(1));


        _ = RefreshTimerLoop(
            _refreshCancellation.Token);
    }


    // ==================================================
    // Refresh Timer Loop
    // ==================================================

    private async Task RefreshTimerLoop(
        CancellationToken cancellationToken)
    {
        if (_refreshTimer is null)
        {
            return;
        }


        try
        {
            while (
                await _refreshTimer.WaitForNextTickAsync(
                    cancellationToken))
            {
                if (_disposed)
                {
                    return;
                }


                await InvokeAsync(() =>
                {
                    if (_disposed)
                    {
                        return;
                    }


                    StateHasChanged();
                });
            }
        }
        catch (
            OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (
            ObjectDisposedException)
        {
            // Component/circuit was disposed.
        }
    }


    // ==================================================
    // Users Changed
    // ==================================================

    private async Task OnUsersChanged()
    {
        if (_disposed)
        {
            return;
        }


        List<User> users =
            AvailabilityService.Users
                .Select(CloneUser)
                .ToList();


        try
        {
            await InvokeAsync(() =>
            {
                if (_disposed)
                {
                    return;
                }


                _users = users;

                UpdateSelectedUser();

                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // Renderer/circuit was disposed.
        }
    }


    // ==================================================
    // User Updated
    // ==================================================

    private async Task OnUserUpdated(
        User user)
    {
        if (_disposed)
        {
            return;
        }


        List<User> users =
            AvailabilityService.Users
                .Select(CloneUser)
                .ToList();


        try
        {
            await InvokeAsync(() =>
            {
                if (_disposed)
                {
                    return;
                }


                _users = users;

                UpdateSelectedUser();

                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // Renderer/circuit was disposed.
        }
    }


    // ==================================================
    // Update Selected User
    // ==================================================

    private void UpdateSelectedUser()
    {
        if (_selectedUser is null)
        {
            return;
        }


        User? updatedUser =
            _users.FirstOrDefault(
                x => x.UserId == _selectedUser.UserId);


        if (updatedUser is not null)
        {
            _selectedUser =
                CloneUser(updatedUser);
        }
    }


    // ==================================================
    // Open User Details
    // ==================================================

    private async Task OpenUserDetails(
        User user)
    {
        if (_disposed)
        {
            return;
        }


        _selectedUser =
            CloneUser(user);


        try
        {
            _userHistory =
                await AvailabilityService.GetUserHistory(
                    user.UserId);


            if (_disposed)
            {
                return;
            }


            await InvokeAsync(
                StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            // Component/circuit was disposed.
        }
    }


    // ==================================================
    // Close User Details
    // ==================================================

    private void CloseUserDetails()
    {
        if (_disposed)
        {
            return;
        }


        _selectedUser = null;

        _userHistory = [];


        StateHasChanged();
    }


    // ==================================================
    // Clone User
    // ==================================================

    private static User CloneUser(
        User user)
    {
        return new User
        {
            UserId =
                user.UserId,

            Name =
                user.Name,

            IsAvailable =
                user.IsAvailable,

            Status =
                user.Status,

            InOfficeStartTime =
                user.InOfficeStartTime,

            TotalTimeInOffice =
                user.TotalTimeInOffice
        };
    }


    // ==================================================
    // Dispose
    // ==================================================

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }


        _disposed = true;


        AvailabilityService.UserUpdated -=
            OnUserUpdated;

        AvailabilityService.UsersChanged -=
            OnUsersChanged;


        _refreshCancellation?.Cancel();

        _refreshTimer?.Dispose();

        _refreshCancellation?.Dispose();


        _selectedUser = null;

        _users.Clear();

        _userHistory.Clear();
    }
}