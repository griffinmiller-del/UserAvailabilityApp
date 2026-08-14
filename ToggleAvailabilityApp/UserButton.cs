using System.Drawing;
using System.Drawing.Drawing2D;

namespace ToggleAvailabilityApp;

public class UserButton : UserControl
{
    private readonly Label lb_Name;

    private readonly Button btn_Break;
    private readonly Button btn_Meeting;
    private readonly Button btn_OtherSide;
    private readonly Button btn_Other;

    private Status? _selectedStatus;

    private User? _user;

    public User? User
    {
        get => _user;

        set
        {
            _user = value;

            if (_user is null)
                return;

            lb_Name.Text = _user.Name;

            UpdateDisplay();
        }
    }

    public event EventHandler<User>? AvailabilityChanged;

    public UserButton()
    {
        Margin = Padding.Empty;
        Padding = Padding.Empty;

        BackColor = Color.White;
        BorderStyle = BorderStyle.None;



        // --------------------------------------------------
        // Name
        // --------------------------------------------------

        lb_Name = new Label
        {
            Dock = DockStyle.Fill,

            TextAlign = ContentAlignment.MiddleCenter,

            Font = new Font(
                "Segoe UI",
                18F,
                FontStyle.Regular),

            BackColor = Color.White,

            Margin = Padding.Empty,

            Padding = Padding.Empty,

            Cursor = Cursors.Hand
        };

        lb_Name.Click += Name_Click;

        // --------------------------------------------------
        // Bottom status buttons
        // --------------------------------------------------

        btn_Break = CreateStatusButton(
            "Break",
            Status.Break);

        btn_Meeting = CreateStatusButton(
            "Meeting",
            Status.Meeting);

        btn_OtherSide = CreateStatusButton(
            "Other Side",
            Status.OtherSide);

        btn_Other = CreateStatusButton(
            "Other",
            Status.Other);

        // --------------------------------------------------
        // Bottom button panel
        // --------------------------------------------------

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,

            Height = 70,

            ColumnCount = 4,
            RowCount = 1,

            Margin = Padding.Empty,
            Padding = Padding.Empty,

            BackColor = Color.White
        };

        statusPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                25F));

        statusPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                25F));

        statusPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                25F));

        statusPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                25F));

        statusPanel.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100F));

        statusPanel.Controls.Add(
            btn_Break,
            0,
            0);

        statusPanel.Controls.Add(
            btn_Meeting,
            1,
            0);

        statusPanel.Controls.Add(
            btn_OtherSide,
            2,
            0);

        statusPanel.Controls.Add(
            btn_Other,
            3,
            0);

        // --------------------------------------------------
        // Main layout
        // --------------------------------------------------

        Controls.Add(lb_Name);
        Controls.Add(statusPanel);

        // Make the name area fill everything above
        // the status buttons.
        lb_Name.BringToFront();

        // Paint the availability indicator.
        lb_Name.Paint += Name_Paint;

        Resize += (_, _) => Invalidate();
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var pen = new Pen(
            Color.Black,
            2);

        e.Graphics.DrawRectangle(
            pen,
            0,
            0,
            ClientSize.Width - 1,
            ClientSize.Height - 1);
    }

    private Button CreateStatusButton(
        string text,
        Status status)
    {
        var button = new Button
        {
            Text = text,

            Dock = DockStyle.Fill,

            Margin = Padding.Empty,
            Padding = Padding.Empty,

            FlatStyle = FlatStyle.Flat,

            Font = new Font(
                "Segoe UI",
                12F,
                FontStyle.Regular),

            BackColor = Color.White,

            ForeColor = Color.Black,
            Cursor = Cursors.Hand,

            Tag = status
        };

        button.FlatAppearance.BorderColor =
            Color.Black;

        button.FlatAppearance.BorderSize = 1;

        button.Click += StatusButton_Click;

        return button;
    }

    private void Name_Click(
    object? sender,
    EventArgs e)
    {
        _selectedStatus = null;

        if (_user is not null)
        {
            _user.IsAvailable = true;
            _user.Status = Status.InOffice;

            AvailabilityChanged?.Invoke(
                this,
                _user);
        }

        UpdateDisplay();
    }

    private void StatusButton_Click(
    object? sender,
    EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not Status status)
            return;

        // Clicking the currently selected status
        // clears it and returns the user to In Office.
        if (_selectedStatus == status)
        {
            _selectedStatus = null;

            if (_user is not null)
            {
                _user.IsAvailable = true;
                _user.Status = Status.InOffice;

                AvailabilityChanged?.Invoke(
                    this,
                    _user);
            }
        }
        else
        {
            _selectedStatus = status;

            if (_user is not null)
            {
                _user.IsAvailable = false;
                _user.Status = status;

                AvailabilityChanged?.Invoke(
                    this,
                    _user);
            }
        }
        Parent?.Focus();
        UpdateDisplay();
    }

    public void UpdateFromServer(User user)
    {
        if (_user is null)
            return;

        if (_user.UserId != user.UserId)
            return;

        _user.Name =
            user.Name;

        _user.IsAvailable =
            user.IsAvailable;

        _user.Status =
            user.Status;

        // In Office = no selected status.
        if (user.IsAvailable ||
            user.Status == Status.InOffice)
        {
            _selectedStatus = null;
        }
        else
        {
            // Select the status received from
            // the SignalR server.
            _selectedStatus = user.Status;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        UpdateStatusButton(
            btn_Break,
            Status.Break);

        UpdateStatusButton(
            btn_Meeting,
            Status.Meeting);

        UpdateStatusButton(
            btn_OtherSide,
            Status.OtherSide);

        UpdateStatusButton(
            btn_Other,
            Status.Other);

        lb_Name.Invalidate();
    }

    private void UpdateStatusButton(
        Button button,
        Status status)
    {
        if (_selectedStatus == status)
        {
            // Selected status
            button.BackColor =
                Color.LightGray;

            button.FlatAppearance.BorderColor =
                Color.Black;

            button.FlatAppearance.BorderSize =
                3;
        }
        else
        {
            // Unselected status
            button.BackColor =
                Color.White;

            button.FlatAppearance.BorderSize =
                1;
        }
    }
    private void Name_Paint(
    object? sender,
    PaintEventArgs e)
    {
        bool isAvailable =
            _user?.IsAvailable ?? true;

        Color indicatorColor =
            isAvailable
                ? Color.LimeGreen
                : Color.Red;

        const int diameter = 18;
        const int rightMargin = 10;
        const int bottomMargin = 10;

        int x =
            lb_Name.ClientSize.Width
            - diameter
            - rightMargin;

        int y =
            lb_Name.ClientSize.Height
            - diameter
            - bottomMargin;

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        using var brush =
            new SolidBrush(indicatorColor);

        e.Graphics.FillEllipse(
            brush,
            x,
            y,
            diameter,
            diameter);
    }


}