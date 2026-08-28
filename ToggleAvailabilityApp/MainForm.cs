using ToggleAvailabilityApp.Services;

namespace ToggleAvailabilityApp;

public class MainForm : Form
{
    private readonly TableLayoutPanel tlp_Users;

    private readonly AvailabilityService _availabilityService;

    // This contains ALL users, including inactive users.
    private readonly List<User> _users = [];

    private readonly Button btn_Edit;

    // --------------------------------------------------
    // UI Colors
    // --------------------------------------------------

    private static readonly Color Background =
        Color.FromArgb(12, 14, 16);

    private static readonly Color CardBackground =
        Color.FromArgb(25, 27, 30);

    private static readonly Color Yellow =
        Color.FromArgb(255, 195, 0);

    private static readonly Color TextColor =
        Color.White;

    private static readonly Color SecondaryText =
        Color.FromArgb(170, 170, 170);


    // --------------------------------------------------
    // Constructor
    // --------------------------------------------------

    public MainForm()
    {
        Text =
            "Toggle Status";

        StartPosition =
            FormStartPosition.CenterScreen;

        MinimumSize =
            new Size(800, 500);

        Size =
            new Size(1100, 700);

        BackColor =
            Background;

        ForeColor =
            TextColor;

        Font =
            new Font(
                "Segoe UI",
                10F);

        FormBorderStyle =
            FormBorderStyle.None;

        WindowState =
            FormWindowState.Maximized;

        // --------------------------------------------------
        // SignalR service
        // --------------------------------------------------

        _availabilityService =
            new AvailabilityService();

        _availabilityService.UserListReceived +=
            AvailabilityService_UserListReceived;

        _availabilityService.UserUpdated +=
            AvailabilityService_UserUpdated;

        // --------------------------------------------------
        // User grid
        // --------------------------------------------------

        tlp_Users =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    4,

                RowCount =
                    1,

                AutoScroll =
                    true,

                BackColor =
                    Background,

                Margin =
                    Padding.Empty,

                Padding =
                    new Padding(12)
            };

        // --------------------------------------------------
        // Columns
        // --------------------------------------------------

        for (int column = 0;
             column < 4;
             column++)
        {
            tlp_Users.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    25F));
        }

        // --------------------------------------------------
        // Bottom panel
        // --------------------------------------------------

        var bottomPanel =
            new Panel
            {
                Dock =
                    DockStyle.Bottom,

                Height =
                    70,

                BackColor =
                    Background,

                Padding =
                    new Padding(
                        12,
                        8,
                        12,
                        8),

                Margin =
                    Padding.Empty
            };

        // --------------------------------------------------
        // Edit button
        // --------------------------------------------------

        btn_Edit =
            new Button
            {
                Text =
                    "Edit Users",

                Width =
                    130,

                Height =
                    42,

                FlatStyle =
                    FlatStyle.Flat,

                BackColor =
                    Yellow,

                ForeColor =
                    Color.Black,

                Font =
                    new Font(
                        "Segoe UI",
                        10F,
                        FontStyle.Bold),

                Cursor =
                    Cursors.Hand,

                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Right,

                FlatAppearance =
                {
                    BorderSize = 0
                }
            };

        btn_Edit.Click +=
            Edit_Click;

        bottomPanel.Controls.Add(
            btn_Edit);

        PositionEditButton(
            bottomPanel);

        bottomPanel.Resize +=
            (_, _) =>
            {
                PositionEditButton(
                    bottomPanel);
            };

        // --------------------------------------------------
        // Add controls
        // --------------------------------------------------

        Controls.Add(
            tlp_Users);

        Controls.Add(
            bottomPanel);

        // --------------------------------------------------
        // Connect
        // --------------------------------------------------

        Load +=
            MainForm_Load;
    }


    // ------------------------------------------------------
    // Position Edit button
    // ------------------------------------------------------

    private void PositionEditButton(
        Panel panel)
    {
        btn_Edit.Location =
            new Point(
                panel.ClientSize.Width -
                btn_Edit.Width -
                12,

                (panel.ClientSize.Height -
                 btn_Edit.Height) / 2);
    }


    // ------------------------------------------------------
    // Connect to SignalR
    // ------------------------------------------------------

    private async void MainForm_Load(
        object? sender,
        EventArgs e)
    {
        try
        {
            await _availabilityService
                .ConnectAsync();

            Console.WriteLine(
                "Connected to Availability Server.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to connect to the availability server.\n\n{ex.Message}",
                "Connection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }


    // ------------------------------------------------------
    // Initial/list update from server
    // ------------------------------------------------------

    private void AvailabilityService_UserListReceived(
        List<User> users)
    {
        if (InvokeRequired)
        {
            Invoke(() =>
                AvailabilityService_UserListReceived(
                    users));

            return;
        }

        Console.WriteLine(
            $"Received user list: " +
            $"{users.Count} users.");

        UpdateUsers(
            users);
    }


    // ------------------------------------------------------
    // Update the local user list
    //
    // IMPORTANT:
    // _users contains BOTH active and inactive users.
    //
    // Only active users are displayed.
    // ------------------------------------------------------

    private void UpdateUsers(
    IEnumerable<User> users)
    {
        var incomingUsers =
            users
                .ToList();

        // --------------------------------------------------
        // The server's list is authoritative.
        //
        // Only users returned by the server belong in
        // the application's active user collection.
        // --------------------------------------------------

        var incomingIds =
            incomingUsers
                .Select(x => x.UserId)
                .ToHashSet();


        // --------------------------------------------------
        // Remove anything the server did not return.
        // --------------------------------------------------

        _users.RemoveAll(
            x =>
                !incomingIds.Contains(
                    x.UserId));


        // --------------------------------------------------
        // Add new users and update existing users.
        // --------------------------------------------------

        foreach (var incomingUser in incomingUsers)
        {
            var existingUser =
                _users.FirstOrDefault(
                    x =>
                        x.UserId ==
                        incomingUser.UserId);

            if (existingUser is null)
            {
                _users.Add(
                    CloneUser(
                        incomingUser));

                continue;
            }


            existingUser.Name =
                incomingUser.Name;

            existingUser.IsAvailable =
                incomingUser.IsAvailable;

            existingUser.Status =
                incomingUser.Status;

            existingUser.InOfficeStartTime =
                incomingUser.InOfficeStartTime;

            existingUser.TotalTimeInOffice =
                incomingUser.TotalTimeInOffice;

            existingUser.OutOfOfficeStartTime =
                incomingUser.OutOfOfficeStartTime;

            existingUser.IsActiveUser =
                incomingUser.IsActiveUser;
        }


        RefreshUserGrid();
    }


    // ------------------------------------------------------
    // Creates a complete copy of a User.
    // ------------------------------------------------------

    private static User CloneUser(
        User user)
    {
        var clone =
            new User(
                user.UserId,
                user.Name,
                user.Status,
                user.IsAvailable);

        clone.InOfficeStartTime =
            user.InOfficeStartTime;

        clone.TotalTimeInOffice =
            user.TotalTimeInOffice;

        clone.OutOfOfficeStartTime =
            user.OutOfOfficeStartTime;

        clone.IsActiveUser =
            user.IsActiveUser;

        return clone;
    }


    // ------------------------------------------------------
    // Refresh user grid
    //
    // Only IsActiveUser users receive UserButtons.
    //
    // Existing buttons are preserved whenever possible.
    // ------------------------------------------------------

    private void RefreshUserGrid()
    {
        tlp_Users.SuspendLayout();

        try
        {
            var existingButtons =
                tlp_Users.Controls
                    .OfType<UserButton>()
                    .Where(x => x.User is not null)
                    .ToDictionary(
                        x => x.User!.UserId);

            // --------------------------------------------------
            // Only active users should have buttons.
            // --------------------------------------------------

            var activeUsers =
                _users
                    .Where(x => x.IsActiveUser)
                    .ToList();

            var requiredUserIds =
                activeUsers
                    .Select(x => x.UserId)
                    .ToHashSet();

            // --------------------------------------------------
            // Remove buttons for inactive users.
            // --------------------------------------------------

            foreach (var button in existingButtons.Values)
            {
                if (!requiredUserIds.Contains(
                        button.User!.UserId))
                {
                    button.AvailabilityChanged -=
                        UserButton_AvailabilityChanged;

                    tlp_Users.Controls.Remove(
                        button);

                    button.Dispose();
                }
            }

            // --------------------------------------------------
            // Rebuild the layout positions.
            // --------------------------------------------------

            int rowCount =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        activeUsers.Count / 4.0));

            tlp_Users.RowStyles.Clear();

            tlp_Users.RowCount =
                rowCount;

            for (int row = 0;
                 row < rowCount;
                 row++)
            {
                tlp_Users.RowStyles.Add(
                    new RowStyle(
                        SizeType.Percent,
                        100F / rowCount));
            }

            // --------------------------------------------------
            // Position existing buttons and create only
            // buttons that don't already exist.
            // --------------------------------------------------

            for (int i = 0;
                 i < activeUsers.Count;
                 i++)
            {
                var user =
                    activeUsers[i];

                int row =
                    i / 4;

                int column =
                    i % 4;

                if (existingButtons.TryGetValue(
                        user.UserId,
                        out var button))
                {
                    // ------------------------------------------
                    // Existing button.
                    // ------------------------------------------

                    button.User =
                        user;

                    button.UpdateFromServer(
                        user);

                    tlp_Users.SetCellPosition(
                        button,
                        new TableLayoutPanelCellPosition(
                            column,
                            row));

                    tlp_Users.SetColumnSpan(
                        button,
                        1);

                    tlp_Users.SetRowSpan(
                        button,
                        1);
                }
                else
                {
                    // ------------------------------------------
                    // New button.
                    // ------------------------------------------

                    button =
                        new UserButton
                        {
                            User =
                                user,

                            Dock =
                                DockStyle.Fill,

                            Margin =
                                new Padding(5),

                            BorderStyle =
                                BorderStyle.None,

                            BackColor =
                                CardBackground
                        };

                    button.AvailabilityChanged +=
                        UserButton_AvailabilityChanged;

                    tlp_Users.Controls.Add(
                        button,
                        column,
                        row);
                }
            }
        }
        finally
        {
            tlp_Users.ResumeLayout(
                true);
        }
    }


    // ------------------------------------------------------
    // UserButton changed
    // ------------------------------------------------------

    private async void UserButton_AvailabilityChanged(
        object? sender,
        User user)
    {
        Console.WriteLine(
            $"{user.Name} ({user.UserId}) - " +
            $"{user.Status} - " +
            $"{(user.IsAvailable
                ? "In Office"
                : "Out")}");

        try
        {
            await _availabilityService
                .SetAvailabilityAsync(
                    user);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to update user: " +
                $"{ex.Message}");

            MessageBox.Show(
                $"Unable to update {user.Name}.\n\n{ex.Message}",
                "Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }


    // ------------------------------------------------------
    // Individual user update from server
    // ------------------------------------------------------

    private void AvailabilityService_UserUpdated(
        User updatedUser)
    {
        if (InvokeRequired)
        {
            Invoke(() =>
                AvailabilityService_UserUpdated(
                    updatedUser));

            return;
        }

        Console.WriteLine(
            $"Received user update: " +
            $"{updatedUser.Name} " +
            $"({updatedUser.UserId}) - " +
            $"{updatedUser.Status} - " +
            $"{(updatedUser.IsAvailable
                ? "Available"
                : "Unavailable")}");

        var localUser =
            _users.FirstOrDefault(
                x =>
                    x.UserId ==
                    updatedUser.UserId);

        if (localUser is null)
        {
            Console.WriteLine(
                $"User {updatedUser.UserId} " +
                $"was not found locally.");

            return;
        }

        // --------------------------------------------------
        // Update the complete local user state.
        // --------------------------------------------------

        localUser.Name =
            updatedUser.Name;

        localUser.IsAvailable =
            updatedUser.IsAvailable;

        localUser.Status =
            updatedUser.Status;

        localUser.IsActiveUser =
            updatedUser.IsActiveUser;

        localUser.InOfficeStartTime =
            updatedUser.InOfficeStartTime;

        localUser.TotalTimeInOffice =
            updatedUser.TotalTimeInOffice;

        localUser.OutOfOfficeStartTime =
            updatedUser.OutOfOfficeStartTime;

        // --------------------------------------------------
        // An inactive user should not have a button.
        // --------------------------------------------------

        if (!updatedUser.IsActiveUser)
        {
            RefreshUserGrid();

            return;
        }

        // --------------------------------------------------
        // Find the existing button.
        // --------------------------------------------------

        var button =
            tlp_Users.Controls
                .OfType<UserButton>()
                .FirstOrDefault(
                    x =>
                        x.User?.UserId ==
                        updatedUser.UserId);

        if (button is null)
        {
            // The user is active but doesn't have a button.
            // Rebuild the grid so one is created.
            RefreshUserGrid();

            return;
        }

        button.User =
            localUser;

        button.UpdateFromServer(
            updatedUser);
    }

// ------------------------------------------------------
// Edit users
// ------------------------------------------------------

private async void Edit_Click(
    object? sender,
    EventArgs e)
    {
        // --------------------------------------------------
        // Ask for administrator passcode.
        // --------------------------------------------------

        using var passcodeForm =
            new PasscodeForm();

        if (passcodeForm.ShowDialog(this) !=
            DialogResult.OK)
        {
            return;
        }


        // --------------------------------------------------
        // Authenticate the SignalR connection.
        // --------------------------------------------------

        bool authenticated =
            await _availabilityService
                .AuthenticateAdminAsync(
                    passcodeForm.Passcode);


        if (!authenticated)
        {
            MessageBox.Show(
                "The administrator passcode is incorrect.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }


        // --------------------------------------------------
        // Authentication succeeded.
        //
        // The passcode is no longer needed and is not stored.
        // The server has marked this SignalR connection as
        // administrator-authenticated.
        // --------------------------------------------------

        using var editForm =
            new EditUsersForm(
                _users
                    .Where(x => x.IsActiveUser)
                    .Select(CloneUser)
                    .ToList());

        if (editForm.ShowDialog(this) !=
            DialogResult.OK)
        {
            return;
        }


        // --------------------------------------------------
        // These are the users that currently exist locally.
        // --------------------------------------------------

        var existingUsers =
            _users
                .Where(x => x.IsActiveUser)
                .Select(CloneUser)
                .ToList();


        // --------------------------------------------------
        // These are the users returned by EditUsersForm.
        // --------------------------------------------------

        var editedUsers =
            editForm.Users
                .Select(CloneUser)
                .ToList();


        try
        {
            btn_Edit.Enabled =
                false;


            // ==================================================
            // Find deleted users
            // ==================================================

            var deletedUsers =
                existingUsers
                    .Where(
                        existing =>
                            !editedUsers.Any(
                                edited =>
                                    edited.UserId ==
                                    existing.UserId))
                    .ToList();


            foreach (var user in deletedUsers)
            {
                Console.WriteLine(
                    $"Requesting deletion of user: " +
                    $"{user.Name} ({user.UserId})");

                await _availabilityService
                    .DeleteUserAsync(
                        user.UserId);
            }


            // ==================================================
            // Find added and renamed users
            // ==================================================

            foreach (var editedUser in editedUsers)
            {
                var existingUser =
                    existingUsers.FirstOrDefault(
                        existing =>
                            existing.UserId ==
                            editedUser.UserId);


                // --------------------------------------------------
                // New user
                // --------------------------------------------------

                if (existingUser is null)
                {
                    Console.WriteLine(
                        $"Requesting addition of user: " +
                        $"{editedUser.Name}");

                    await _availabilityService
                        .AddUserAsync(
                            editedUser.Name);

                    continue;
                }


                // --------------------------------------------------
                // Existing user whose name changed
                // --------------------------------------------------

                if (!string.Equals(
                        existingUser.Name,
                        editedUser.Name,
                        StringComparison.Ordinal))
                {
                    Console.WriteLine(
                        $"Requesting rename of user: " +
                        $"{existingUser.Name} -> " +
                        $"{editedUser.Name} " +
                        $"({editedUser.UserId})");

                    await _availabilityService
                        .UpdateUserAsync(
                            editedUser.UserId,
                            editedUser.Name);
                }
            }


            Console.WriteLine(
                "User changes successfully sent " +
                "to the server.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to save the user changes to the server.\n\n{ex.Message}",
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btn_Edit.Enabled =
                true;
        }
    }




    // ------------------------------------------------------
    // Cleanup
    // ------------------------------------------------------

    protected override async void OnFormClosed(
        FormClosedEventArgs e)
    {
        try
        {
            await _availabilityService
                .DisposeAsync();
        }
        catch
        {
            // Ignore connection errors while closing.
        }

        base.OnFormClosed(e);
    }
}