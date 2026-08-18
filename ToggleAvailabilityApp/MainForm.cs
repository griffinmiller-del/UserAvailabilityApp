using ToggleAvailabilityApp.Services;

namespace ToggleAvailabilityApp;

public class MainForm : Form
{
    private readonly TableLayoutPanel tlp_Users;

    private readonly AvailabilityService _availabilityService;

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
        //
        // No title/header.
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

        ReplaceUsers(
            users);
    }


    // ------------------------------------------------------
    // Replace local user list
    // ------------------------------------------------------

    private void ReplaceUsers(
        IEnumerable<User> users)
    {
        _users.Clear();

        _users.AddRange(
            users.Select(CloneUser));

        LoadUsers();
    }


    // ------------------------------------------------------
    // Clone user
    // ------------------------------------------------------

    private static User CloneUser(
        User user)
    {
        return new User(
            user.UserId,
            user.Name,
            user.Status,
            user.IsAvailable);
    }


    // ------------------------------------------------------
    // Build user grid
    // ------------------------------------------------------

    private void LoadUsers()
    {
        tlp_Users.SuspendLayout();

        try
        {
            tlp_Users.Controls.Clear();

            tlp_Users.RowStyles.Clear();

            int rowCount =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        _users.Count / 4.0));

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

            for (int i = 0;
                 i < _users.Count;
                 i++)
            {
                var user =
                    _users[i];

                var button =
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

                int row =
                    i / 4;

                int column =
                    i % 4;

                tlp_Users.Controls.Add(
                    button,
                    column,
                    row);
            }
        }
        finally
        {
            tlp_Users.ResumeLayout();
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
    // Individual user updated by server
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

        localUser.Name =
            updatedUser.Name;

        localUser.IsAvailable =
            updatedUser.IsAvailable;

        localUser.Status =
            updatedUser.Status;

        var button =
            tlp_Users.Controls
                .OfType<UserButton>()
                .FirstOrDefault(
                    x =>
                        x.User?.UserId ==
                        updatedUser.UserId);

        if (button is null)
        {
            Console.WriteLine(
                $"UserButton for " +
                $"{updatedUser.Name} " +
                $"was not found.");

            return;
        }

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
        using var editForm =
            new EditUsersForm(
                _users);

        if (editForm.ShowDialog(this) !=
            DialogResult.OK)
        {
            return;
        }

        var updatedUsers =
            editForm.Users
                .Select(CloneUser)
                .ToList();

        Console.WriteLine(
            $"Saving {updatedUsers.Count} " +
            $"users to server...");

        try
        {
            btn_Edit.Enabled =
                false;

            await _availabilityService
                .UpdateUserListAsync(
                    updatedUsers);

            Console.WriteLine(
                "User list successfully sent " +
                "to the server.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to save the user list to the server.\n\n{ex.Message}",
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