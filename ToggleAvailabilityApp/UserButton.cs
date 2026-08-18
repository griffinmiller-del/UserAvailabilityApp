using System.Drawing;
using System.Drawing.Drawing2D;

namespace ToggleAvailabilityApp;

public class UserButton : UserControl
{
    private readonly Label lb_Name;
    private readonly Label lb_SelectReason;

    private readonly Button btn_Location;
    private readonly Button btn_PTO;
    private readonly Button btn_Lunch;
    private readonly Button btn_WFH;
    private readonly Button btn_GoneForTheDay;
    private readonly Button btn_ClientMeeting;
    private readonly Button btn_Conference;

    private readonly Button btn_Appointment;
    private readonly Button btn_Chicago;
    private readonly Button btn_NewYork;
    private readonly Button btn_Colombia;
    private readonly Button btn_Peru;
    private readonly Button btn_Philippines;
    private readonly Button btn_Italy;
    private readonly TableLayoutPanel mainLayout;
    private readonly TableLayoutPanel reasonPanel;

    private Status? _selectedStatus;

    private User? _user;

    private bool _showReasonSelection;


    // --------------------------------------------------
    // UI Colors
    // --------------------------------------------------

    private static readonly Color CardBackground =
        Color.FromArgb(242, 240, 234);

    private static readonly Color TextColor =
        Color.FromArgb(30, 31, 33);

    private static readonly Color SecondaryText =
        Color.FromArgb(95, 96, 98);

    private static readonly Color ReasonBackground =
        Color.FromArgb(232, 230, 224);

    private static readonly Color ReasonHover =
        Color.FromArgb(222, 220, 214);

    private static readonly Color ReasonPressed =
        Color.FromArgb(207, 205, 199);

    private static readonly Color ReasonBorder =
        Color.FromArgb(205, 203, 197);

    private static readonly Color AvailableColor =
        Color.FromArgb(46, 204, 113);

    private static readonly Color UnavailableColor =
        Color.FromArgb(231, 76, 60);

    private static readonly Color OutOfOfficeBackground =
    Color.FromArgb(225, 223, 217);
    private const int CornerRadius = 10;


    // --------------------------------------------------
    // User
    // --------------------------------------------------

    public User? User
    {
        get => _user;

        set
        {
            _user = value;

            if (_user is null)
                return;

            lb_Name.Text =
                _user.Name;

            if (_user.IsAvailable ||
                _user.Status == Status.InOffice)
            {
                _selectedStatus = null;
                _showReasonSelection = false;
            }
            else
            {
                _selectedStatus =
                    _user.Status;

                _showReasonSelection = false;
            }

            UpdateDisplay();
        }
    }


    // --------------------------------------------------
    // Events
    // --------------------------------------------------

    public event EventHandler<User>? AvailabilityChanged;


    // --------------------------------------------------
    // Constructor
    // --------------------------------------------------

    public UserButton()
    {
        Margin =
            Padding.Empty;

        Padding =
            Padding.Empty;

        BackColor =
            CardBackground;

        ForeColor =
            TextColor;

        DoubleBuffered =
            true;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);


        // --------------------------------------------------
        // Main layout
        // --------------------------------------------------

        mainLayout =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    1,

                RowCount =
                    2,

                Margin =
                    Padding.Empty,

                Padding =
                    Padding.Empty,

                BackColor =
                    Color.Transparent
            };

        mainLayout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100F));

        // Header/name.
        mainLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                50F));

        // Content.
        mainLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100F));


        // --------------------------------------------------
        // Name
        // --------------------------------------------------

        lb_Name =
            new Label
            {
                Dock =
                    DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleCenter,

                Font =
                    new Font(
                        "Segoe UI",
                        18F,
                        FontStyle.Regular),

                ForeColor =
                    TextColor,

                BackColor =
                    Color.Transparent,

                Margin =
                    Padding.Empty,

                Padding =
                    new Padding(
                        12,
                        0,
                        12,
                        0),

                Cursor =
                    Cursors.Hand
            };

        lb_Name.Click +=
            Name_Click;

        lb_Name.Paint +=
            Name_Paint;


        // --------------------------------------------------
        // Select Reason label
        // --------------------------------------------------

        lb_SelectReason =
            new Label
            {
                Dock =
                    DockStyle.Fill,

                Text =
                    "Select Reason",

                TextAlign =
                    ContentAlignment.MiddleCenter,

                Font =
                    new Font(
                        "Segoe UI",
                        14F,
                        FontStyle.Regular),

                ForeColor =
                    SecondaryText,

                BackColor =
                    Color.Transparent,

                Margin =
                    Padding.Empty,

                Padding =
                    Padding.Empty
            };


        // --------------------------------------------------
        // Reason buttons
        // --------------------------------------------------

        btn_PTO =
            CreateReasonButton(
                "PTO",
                Status.PTO);

        btn_Lunch =
            CreateReasonButton(
                "Lunch",
                Status.Lunch);

        btn_WFH =
            CreateReasonButton(
                "WFH",
                Status.WFH);

        btn_GoneForTheDay =
            CreateReasonButton(
                "Gone for the Day",
                Status.GoneForTheDay);

        btn_ClientMeeting =
            CreateReasonButton(
                "Client Meeting",
                Status.ClientMeeting);

        btn_Conference =
            CreateReasonButton(
                "Conference",
                Status.Conference);

        btn_Appointment =
            CreateReasonButton(
                "Appointment",
                Status.Appointment);

        btn_Location =
            CreateReasonButton(
                "Location...",
                null);

        btn_Chicago =
            CreateReasonButton(
                "Chicago",
                Status.Chicago);

        btn_NewYork =
            CreateReasonButton(
                "New York",
                Status.NewYork);

        btn_Colombia =
            CreateReasonButton(
                "Colombia",
                Status.Colombia);

        btn_Peru =
            CreateReasonButton(
                "Peru",
                Status.Peru);

        btn_Philippines =
            CreateReasonButton(
                "Philippines",
                Status.Philippines);

        btn_Italy =
            CreateReasonButton(
                "Italy",
                Status.Italy);

        // --------------------------------------------------
        // Reason panel
        // --------------------------------------------------

        reasonPanel =
            new TableLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    2,

                RowCount =
                    4,

                Margin =
                    Padding.Empty,

                Padding =
                    new Padding(
                        10,
                        4,
                        10,
                        10),

                BackColor =
                    Color.Transparent
            };

        reasonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50F));

        reasonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50F));

        for (int i = 0; i < 4; i++)
        {
            reasonPanel.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    25F));
        }


        // --------------------------------------------------
        // Populate the initial reason grid
        // --------------------------------------------------

        ShowMainReasons();


        // --------------------------------------------------
        // Add buttons
        // --------------------------------------------------

        reasonPanel.Controls.Add(
            btn_PTO,
            0,
            0);

        reasonPanel.Controls.Add(
            btn_Lunch,
            1,
            0);

        reasonPanel.Controls.Add(
            btn_WFH,
            0,
            1);

        reasonPanel.Controls.Add(
            btn_GoneForTheDay,
            1,
            1);

        reasonPanel.Controls.Add(
            btn_ClientMeeting,
            0,
            2);

        reasonPanel.Controls.Add(
            btn_Conference,
            1,
            2);

        reasonPanel.Controls.Add(
            btn_Appointment,
            0,
            3);


        // --------------------------------------------------
        // Add controls
        // --------------------------------------------------

        mainLayout.Controls.Add(
            lb_Name,
            0,
            0);

        mainLayout.Controls.Add(
            lb_SelectReason,
            0,
            0);

        mainLayout.Controls.Add(
            reasonPanel,
            0,
            1);

        Controls.Add(
            mainLayout);

        UpdateDisplay();
    }

    private void ShowMainReasons()
    {
        reasonPanel.SuspendLayout();

        reasonPanel.Controls.Clear();

        reasonPanel.ColumnCount = 2;
        reasonPanel.RowCount = 4;

        reasonPanel.ColumnStyles.Clear();
        reasonPanel.RowStyles.Clear();

        reasonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50F));

        reasonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50F));

        for (int i = 0; i < 4; i++)
        {
            reasonPanel.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    25F));
        }


        // Reset all buttons before displaying them.
        ResetReasonButtons();


        reasonPanel.Controls.Add(
            btn_PTO,
            0,
            0);

        reasonPanel.Controls.Add(
            btn_Lunch,
            1,
            0);

        reasonPanel.Controls.Add(
            btn_WFH,
            0,
            1);

        reasonPanel.Controls.Add(
            btn_GoneForTheDay,
            1,
            1);

        reasonPanel.Controls.Add(
            btn_ClientMeeting,
            0,
            2);

        reasonPanel.Controls.Add(
            btn_Conference,
            1,
            2);

        reasonPanel.Controls.Add(
            btn_Appointment,
            0,
            3);

        reasonPanel.Controls.Add(
            btn_Location,
            1,
            3);

        reasonPanel.ResumeLayout();
    }

    private void ResetReasonButtons()
    {
        ResetReasonButton(btn_PTO);
        ResetReasonButton(btn_Lunch);
        ResetReasonButton(btn_WFH);
        ResetReasonButton(btn_GoneForTheDay);
        ResetReasonButton(btn_ClientMeeting);
        ResetReasonButton(btn_Conference);
        ResetReasonButton(btn_Appointment);
        ResetReasonButton(btn_Location);

        ResetReasonButton(btn_Chicago);
        ResetReasonButton(btn_NewYork);
        ResetReasonButton(btn_Colombia);
        ResetReasonButton(btn_Peru);
        ResetReasonButton(btn_Philippines);
        ResetReasonButton(btn_Italy);
    }

    private static void ResetReasonButton(
    Button button)
    {
        button.Enabled =
            true;

        button.BackColor =
            ReasonBackground;

        button.ForeColor =
            TextColor;

        button.FlatAppearance.BorderColor =
            ReasonBorder;

        button.FlatAppearance.BorderSize =
            1;

        button.FlatAppearance.MouseOverBackColor =
            ReasonHover;

        button.FlatAppearance.MouseDownBackColor =
            ReasonPressed;
    }

    // --------------------------------------------------
    // Paint rounded card
    // --------------------------------------------------

    // --------------------------------------------------
    // Paint rounded card
    // --------------------------------------------------

    protected override void OnPaint(
        PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        Rectangle bounds =
            new Rectangle(
                0,
                0,
                ClientSize.Width - 1,
                ClientSize.Height - 1);

        using GraphicsPath path =
            CreateRoundedRectangle(
                bounds,
                CornerRadius);


        // --------------------------------------------------
        // Use a slightly darker background when the
        // user is out of office.
        // --------------------------------------------------

        bool isOutOfOffice =
            _user is not null &&
            !_user.IsAvailable &&
            _user.Status != Status.InOffice;

        Color background =
            isOutOfOffice
                ? OutOfOfficeBackground
                : CardBackground;


        using var brush =
            new SolidBrush(
                background);

        e.Graphics.FillPath(
            brush,
            path);


        // --------------------------------------------------
        // Card border
        // --------------------------------------------------

        using var pen =
            new Pen(
                Color.FromArgb(
                    215,
                    213,
                    207),
                1F);

        e.Graphics.DrawPath(
            pen,
            path);
    }


    // --------------------------------------------------
    // Rounded rectangle
    // --------------------------------------------------

    private static GraphicsPath CreateRoundedRectangle(
        Rectangle bounds,
        int radius)
    {
        var path =
            new GraphicsPath();

        int diameter =
            radius * 2;

        path.AddArc(
            bounds.X,
            bounds.Y,
            diameter,
            diameter,
            180,
            90);

        path.AddArc(
            bounds.Right - diameter,
            bounds.Y,
            diameter,
            diameter,
            270,
            90);

        path.AddArc(
            bounds.Right - diameter,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);

        path.AddArc(
            bounds.X,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            90,
            90);

        path.CloseFigure();

        return path;
    }


    // --------------------------------------------------
    // Create reason button
    // --------------------------------------------------

    private Button CreateReasonButton(
    string text,
    Status? status)
    {
        var button =
            new Button
            {
                Text =
                    text,

                Dock =
                    DockStyle.Fill,

                Margin =
                    new Padding(
                        4),

                Padding =
                    Padding.Empty,

                FlatStyle =
                    FlatStyle.Flat,

                Font =
                    new Font(
                        "Segoe UI",
                        12F,
                        FontStyle.Regular),

                BackColor =
                    ReasonBackground,

                ForeColor =
                    TextColor,

                Cursor =
                    Cursors.Hand,

                Tag =
                    status,

                UseVisualStyleBackColor =
                    false
            };

        button.FlatAppearance.BorderSize =
            1;

        button.FlatAppearance.BorderColor =
            ReasonBorder;

        button.FlatAppearance.MouseOverBackColor =
            ReasonHover;

        button.FlatAppearance.MouseDownBackColor =
            ReasonPressed;

        button.Click +=
            ReasonButton_Click;

        return button;
    }

    private void ShowLocationReasons()
    {
        reasonPanel.SuspendLayout();

        reasonPanel.Controls.Clear();

        reasonPanel.ColumnCount = 2;
        reasonPanel.RowCount = 3;

        reasonPanel.ColumnStyles.Clear();
        reasonPanel.RowStyles.Clear();

        reasonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50F));

        reasonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50F));

        for (int i = 0; i < 3; i++)
        {
            reasonPanel.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    33.3333F));
        }


        ResetReasonButtons();


        reasonPanel.Controls.Add(
            btn_Chicago,
            0,
            0);

        reasonPanel.Controls.Add(
            btn_NewYork,
            1,
            0);

        reasonPanel.Controls.Add(
            btn_Colombia,
            0,
            1);

        reasonPanel.Controls.Add(
            btn_Peru,
            1,
            1);

        reasonPanel.Controls.Add(
            btn_Philippines,
            0,
            2);

        reasonPanel.Controls.Add(
            btn_Italy,
            1,
            2);

        reasonPanel.ResumeLayout();
    }


    // --------------------------------------------------
    // Name clicked
    // --------------------------------------------------

    private void Name_Click(
        object? sender,
        EventArgs e)
    {
        if (_user is null)
            return;


        // --------------------------------------------------
        // In Office -> open reason selection
        // --------------------------------------------------

        if (_user.IsAvailable &&
            _user.Status == Status.InOffice)
        {
            _showReasonSelection =
                !_showReasonSelection;

            if (_showReasonSelection)
            {
                // Always start on the main reason screen.
                ShowMainReasons();
            }

            UpdateDisplay();

            return;
        }


        // --------------------------------------------------
        // Currently out -> return to In Office
        // --------------------------------------------------

        _selectedStatus =
            null;

        _showReasonSelection =
            false;

        _user.IsAvailable =
            true;

        _user.Status =
            Status.InOffice;

        AvailabilityChanged?.Invoke(
            this,
            _user);

        UpdateDisplay();

        Parent?.Focus();
    }


    // --------------------------------------------------
    // Reason selected
    // --------------------------------------------------

    // --------------------------------------------------
    // Reason selected
    // --------------------------------------------------

    private void ReasonButton_Click(
    object? sender,
    EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (_user is null)
            return;


        // --------------------------------------------------
        // Location button
        // --------------------------------------------------

        if (button == btn_Location)
        {
            ShowLocationReasons();

            return;
        }


        // --------------------------------------------------
        // A real status was selected
        // --------------------------------------------------

        if (button.Tag is not Status status)
            return;


        _selectedStatus =
            status;

        _showReasonSelection =
            false;

        _user.IsAvailable =
            false;

        _user.Status =
            status;


        // --------------------------------------------------
        // Notify server
        // --------------------------------------------------

        AvailabilityChanged?.Invoke(
            this,
            _user);


        // --------------------------------------------------
        // Return to normal UserButton view
        // --------------------------------------------------

        UpdateDisplay();

        Parent?.Focus();
    }


    // --------------------------------------------------
    // Server update
    // --------------------------------------------------

    public void UpdateFromServer(
        User user)
    {
        if (_user is null)
            return;

        if (_user.UserId !=
            user.UserId)
            return;

        _user.Name =
            user.Name;

        _user.IsAvailable =
            user.IsAvailable;

        _user.Status =
            user.Status;

        lb_Name.Text =
            user.Name;

        if (user.IsAvailable ||
            user.Status == Status.InOffice)
        {
            _selectedStatus =
                null;
        }
        else
        {
            _selectedStatus =
                user.Status;
        }

        _showReasonSelection =
            false;

        UpdateDisplay();
    }


    // --------------------------------------------------
    // Update display
    // --------------------------------------------------

    // --------------------------------------------------
    // Update display
    // --------------------------------------------------

    private void UpdateDisplay()
    {
        bool isSelectingReason =
            _showReasonSelection &&
            _user is not null &&
            _user.IsAvailable &&
            _user.Status == Status.InOffice;


        // --------------------------------------------------
        // Normal UserButton state
        // --------------------------------------------------

        if (!isSelectingReason)
        {
            lb_Name.Visible =
                true;

            lb_SelectReason.Visible =
                false;

            reasonPanel.Visible =
                false;

            mainLayout.RowStyles[0] =
                new RowStyle(
                    SizeType.Percent,
                    100F);

            mainLayout.RowStyles[1] =
                new RowStyle(
                    SizeType.Absolute,
                    0F);
        }


        // --------------------------------------------------
        // Reason selection state
        // --------------------------------------------------

        else
        {
            lb_Name.Visible =
                false;

            lb_SelectReason.Visible =
                true;

            reasonPanel.Visible =
                true;

            mainLayout.RowStyles[0] =
                new RowStyle(
                    SizeType.Absolute,
                    50F);

            mainLayout.RowStyles[1] =
                new RowStyle(
                    SizeType.Percent,
                    100F);
        }


        // Force layout refresh.
        mainLayout.PerformLayout();

        lb_Name.Invalidate();

        lb_SelectReason.Invalidate();

        reasonPanel.Invalidate();

        Invalidate();
    }


    // --------------------------------------------------
    // Name / availability indicator
    // --------------------------------------------------

    private void Name_Paint(
        object? sender,
        PaintEventArgs e)
    {
        if (!lb_Name.Visible)
            return;

        if (_user is null)
            return;


        bool isAvailable =
            _user.IsAvailable ||
            _user.Status == Status.InOffice;


        Color indicatorColor =
            isAvailable
                ? AvailableColor
                : UnavailableColor;


        const int diameter = 12;

        const int rightMargin = 14;

        const int bottomMargin = 14;

        const int reasonRightMargin = 12;


        // --------------------------------------------------
        // Status indicator position
        // --------------------------------------------------

        int indicatorX =
            lb_Name.ClientSize.Width -
            diameter -
            rightMargin;

        int indicatorY =
            lb_Name.ClientSize.Height -
            diameter -
            bottomMargin;


        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;


        // --------------------------------------------------
        // Draw reason when unavailable
        // --------------------------------------------------

        if (!isAvailable)
        {
            string reason =
                GetReasonText(
                    _user.Status);

            using var reasonFont =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Regular);

            using var reasonBrush =
                new SolidBrush(
                    SecondaryText);


            SizeF textSize =
                e.Graphics.MeasureString(
                    reason,
                    reasonFont);


            // Place the reason immediately to the
            // left of the status indicator.
            float reasonX =
                indicatorX -
                reasonRightMargin -
                textSize.Width;

            float reasonY =
                indicatorY +
                (diameter -
                 textSize.Height) / 2F;


            e.Graphics.DrawString(
                reason,
                reasonFont,
                reasonBrush,
                reasonX,
                reasonY);
        }


        // --------------------------------------------------
        // Indicator outline
        // --------------------------------------------------

        Color indicatorOutline =
            isAvailable
                ? CardBackground
                : OutOfOfficeBackground;


        using var outlinePen =
            new Pen(
                indicatorOutline,
                2F);

        e.Graphics.DrawEllipse(
            outlinePen,
            indicatorX - 1,
            indicatorY - 1,
            diameter + 2,
            diameter + 2);


        // --------------------------------------------------
        // Indicator
        // --------------------------------------------------

        using var brush =
            new SolidBrush(
                indicatorColor);

        e.Graphics.FillEllipse(
            brush,
            indicatorX,
            indicatorY,
            diameter,
            diameter);
    }

    // --------------------------------------------------
    // Get display text for status
    // --------------------------------------------------

    private static string GetReasonText(
    Status status)
    {
        return status switch
        {
            Status.PTO =>
                "PTO",

            Status.Lunch =>
                "Lunch",

            Status.WFH =>
                "WFH",

            Status.GoneForTheDay =>
                "Gone for the Day",

            Status.ClientMeeting =>
                "Client Meeting",

            Status.Conference =>
                "Conference",

            Status.Appointment =>
                "Appointment",

            Status.Chicago =>
                "Chicago",

            Status.NewYork =>
                "New York",

            Status.Colombia =>
                "Colombia",

            Status.Peru =>
                "Peru",

            Status.Philippines =>
                "Philippines",

            Status.Italy =>
                "Italy",

            _ =>
                string.Empty
        };
    }
}