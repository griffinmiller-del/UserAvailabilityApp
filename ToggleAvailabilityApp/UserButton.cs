using System.Drawing.Drawing2D;

namespace ToggleAvailabilityApp;

public class UserButton : UserControl
{
    private readonly Label lb_Name;
    private User? _user;

    public User? User
    {
        get => _user;
        set
        {
            _user = value;
            UpdateDisplay();
        }
    }

    public UserButton()
    {
        Size = new Size(180, 100);
        MinimumSize = new Size(120, 80);
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        TabStop = true;

        lb_Name = new Label
        {
            Name = "lb_Name",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoEllipsis = true,
            Padding = new Padding(8, 4, 25, 4)
        };

        Controls.Add(lb_Name);

        Click += UserButton_Click;
        lb_Name.Click += UserButton_Click;
        Resize += (_, _) => Invalidate();
    }

    private void UserButton_Click(object? sender, EventArgs e)
    {
        if (_user is null)
            return;

        _user.IsAvailable = !_user.IsAvailable;
        UpdateDisplay();
        AvailabilityChanged?.Invoke(this, _user);
    }

    public event EventHandler<User>? AvailabilityChanged;

    private void UpdateDisplay()
    {
        lb_Name.Text = _user?.Name ?? string.Empty;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_user is null)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        const int diameter = 18;
        const int margin = 8;

        int x = ClientSize.Width - diameter - margin;
        int y = ClientSize.Height - diameter - margin;

        using var brush = new SolidBrush(
            _user.IsAvailable ? Color.LimeGreen : Color.Red);

        e.Graphics.FillEllipse(brush, x, y, diameter, diameter);

        using var outline = new Pen(Color.DimGray, 1);
        e.Graphics.DrawEllipse(outline, x, y, diameter, diameter);
    }
}
