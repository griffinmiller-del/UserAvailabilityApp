using System.Text.Json;
using ToggleAvailabilityApp.Services;

namespace ToggleAvailabilityApp;

public class MainForm : Form
{
    private readonly TableLayoutPanel tlp_Users;

    private readonly AvailabilityService _availabilityService;

    private readonly List<User> _users = [];

    public MainForm()
    {
        Text = "Toggle Status";

        StartPosition =
            FormStartPosition.CenterScreen;

        MinimumSize =
            new Size(700, 450);

        Size =
            new Size(1000, 600);

        BackColor =
            Color.White;

        // Create the SignalR service
        _availabilityService =
            new AvailabilityService();

        // Listen for the initial user list.
        _availabilityService.UserListReceived +=
            AvailabilityService_UserListReceived;

        // Listen for changes made by other clients.
        _availabilityService.UserUpdated +=
            AvailabilityService_UserUpdated;


        // Title
        var title = new Label
        {
            Dock = DockStyle.Top,

            Height = 60,

            Text = "Office Presence",

            TextAlign =
                ContentAlignment.MiddleCenter,

            Font =
                new Font(
                    "Segoe UI",
                    18F,
                    FontStyle.Bold),

            BackColor =
                Color.White
        };

        // User grid
        tlp_Users = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            ColumnCount = 4,

            AutoScroll = true,

            Margin = Padding.Empty,

            Padding = Padding.Empty,

            BackColor = Color.White
        };

        // Four equal-width columns
        for (int column = 0; column < 4; column++)
        {
            tlp_Users.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    25F));
        }

        Controls.Add(tlp_Users);
        Controls.Add(title);
        // Connect to the SignalR server once
        // the form has loaded.
        Load += MainForm_Load;
    }
    
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
            $"Loading {users.Count} users " +
            $"received from server.");

        _users.Clear();

        _users.AddRange(users);

        LoadUsers();
    }


    private void LoadUsers()
    {
        tlp_Users.SuspendLayout();

        tlp_Users.Controls.Clear();

        tlp_Users.RowStyles.Clear();

        int rowCount =
            (int)Math.Ceiling(
                _users.Count / 4.0);

        if (rowCount == 0)
            rowCount = 1;

        tlp_Users.RowCount =
            rowCount;

        // Make every row the same height.
        for (int row = 0;
             row < rowCount;
             row++)
        {
            tlp_Users.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100F / rowCount));
        }

        // Create a UserButton for every user.
        for (int i = 0;
             i < _users.Count;
             i++)
        {
            var user = _users[i];

            var button = new UserButton
            {
                User = user,

                Dock = DockStyle.Fill,

                Margin = new Padding(1),
                BorderStyle = BorderStyle.FixedSingle
            };

            button.AvailabilityChanged +=
                UserButton_AvailabilityChanged;

            int row = i / 4;

            int column = i % 4;

            tlp_Users.Controls.Add(
                button,
                column,
                row);
        }

        tlp_Users.ResumeLayout();
    }

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

        await _availabilityService
            .SetAvailabilityAsync(user);
    }

    private void AvailabilityService_UserUpdated(
    User updatedUser)
    {
        // SignalR receives messages on a background
        // thread. WinForms controls must be updated
        // on the UI thread.
        if (InvokeRequired)
        {
            Invoke(() =>
                AvailabilityService_UserUpdated(
                    updatedUser));

            return;
        }

        Console.WriteLine(
            $"Received update: {updatedUser.Name} " +
            $"({updatedUser.UserId}) = " +
            $"{updatedUser.Status} - " +
            $"{(updatedUser.IsAvailable
                ? "Available"
                : "Unavailable")}");

        // Find our local copy of the user.
        var localUser =
            _users.FirstOrDefault(
                x => x.UserId ==
                     updatedUser.UserId);

        if (localUser is null)
        {
            Console.WriteLine(
                $"User {updatedUser.UserId} " +
                $"was not found locally.");

            return;
        }

        // Update BOTH pieces of state.
        localUser.IsAvailable =
            updatedUser.IsAvailable;

        localUser.Status =
            updatedUser.Status;

        // Find the corresponding UserButton.
        var button =
            tlp_Users.Controls
                .OfType<UserButton>()
                .FirstOrDefault(
                    x => x.User?.UserId ==
                         updatedUser.UserId);

        if (button is null)
        {
            Console.WriteLine(
                $"UserButton for " +
                $"{updatedUser.Name} was not found.");

            return;
        }

        // Synchronize the visual state without
        // triggering another SignalR update.
        button.UpdateFromServer(
            updatedUser);
    }

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
            // Ignore errors while closing.
        }

        base.OnFormClosed(e);
    }
}