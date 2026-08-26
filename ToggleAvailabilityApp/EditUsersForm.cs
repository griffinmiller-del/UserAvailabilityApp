namespace ToggleAvailabilityApp;

public partial class EditUsersForm : Form
{
    private readonly List<User> _users;

    private readonly ListBox lst_Users;

    private readonly TextBox txt_Name;

    private readonly Button btn_Save;

    private readonly Button btn_Add;

    private readonly Button btn_Delete;

    private readonly Button btn_Cancel;

    // --------------------------------------------------
    // Public list returned to MainForm
    // --------------------------------------------------

    public List<User> Users =>
        _users
            .Select(CloneUser)
            .ToList();

    // --------------------------------------------------
    // Constructor
    // --------------------------------------------------

    public EditUsersForm(
        List<User> users)
    {
        // Make copies so pressing Cancel does not
        // modify the MainForm's actual user list.
        _users =
            users
                .Select(CloneUser)
                .ToList();

        Text =
            "Edit Users";

        StartPosition =
            FormStartPosition.CenterParent;

        Size =
            new Size(500, 500);

        MinimumSize =
            new Size(500, 500);

        BackColor =
            Color.White;

        // --------------------------------------------------
        // User list
        // --------------------------------------------------

        lst_Users =
            new ListBox
            {
                Dock =
                    DockStyle.Fill,

                Font =
                    new Font(
                        "Segoe UI",
                        14F),

                Margin =
                    Padding.Empty
            };

        lst_Users.SelectedIndexChanged +=
            Users_SelectedIndexChanged;

        // --------------------------------------------------
        // Name textbox
        // --------------------------------------------------

        txt_Name =
            new TextBox
            {
                Dock =
                    DockStyle.Top,

                Height =
                    40,

                Font =
                    new Font(
                        "Segoe UI",
                        14F),

                Margin =
                    new Padding(10)
            };

        // --------------------------------------------------
        // Save button
        // --------------------------------------------------

        btn_Save =
            new Button
            {
                Text =
                    "Save",

                Width =
                    100,

                Height =
                    40,

                Font =
                    new Font(
                        "Segoe UI",
                        12F),

                Cursor =
                    Cursors.Hand,

                Enabled =
                    false
            };

        // --------------------------------------------------
        // Add button
        // --------------------------------------------------

        btn_Add =
            new Button
            {
                Text =
                    "Add",

                Width =
                    100,

                Height =
                    40,

                Font =
                    new Font(
                        "Segoe UI",
                        12F),

                Cursor =
                    Cursors.Hand
            };

        // --------------------------------------------------
        // Delete button
        // --------------------------------------------------

        btn_Delete =
            new Button
            {
                Text =
                    "Delete",

                Width =
                    100,

                Height =
                    40,

                Font =
                    new Font(
                        "Segoe UI",
                        12F),

                Cursor =
                    Cursors.Hand,

                Enabled =
                    false
            };

        // --------------------------------------------------
        // Cancel button
        // --------------------------------------------------

        btn_Cancel =
            new Button
            {
                Text =
                    "Cancel",

                Width =
                    100,

                Height =
                    40,

                Font =
                    new Font(
                        "Segoe UI",
                        12F),

                Cursor =
                    Cursors.Hand
            };

        // --------------------------------------------------
        // Events
        // --------------------------------------------------

        btn_Save.Click +=
            Save_Click;

        btn_Add.Click +=
            Add_Click;

        btn_Delete.Click +=
            Delete_Click;

        btn_Cancel.Click +=
            Cancel_Click;

        // --------------------------------------------------
        // Bottom button panel
        // --------------------------------------------------

        var buttonPanel =
            new FlowLayoutPanel
            {
                Dock =
                    DockStyle.Bottom,

                Height =
                    60,

                FlowDirection =
                    FlowDirection.LeftToRight,

                Padding =
                    new Padding(10),

                BackColor =
                    Color.White
            };

        buttonPanel.Controls.Add(
            btn_Add);

        buttonPanel.Controls.Add(
            btn_Save);

        buttonPanel.Controls.Add(
            btn_Delete);

        buttonPanel.Controls.Add(
            btn_Cancel);

        // --------------------------------------------------
        // Main layout
        // --------------------------------------------------

        Controls.Add(
            lst_Users);

        Controls.Add(
            txt_Name);

        Controls.Add(
            buttonPanel);

        RefreshUserList();
    }

    /// <summary>
    /// Handles refreshing the user list
    /// </summary>
    private void RefreshUserList()
    {
        lst_Users.BeginUpdate();

        try
        {
            lst_Users.Items.Clear();

            foreach (var user in _users)
            {
                lst_Users.Items.Add(user);
            }

            lst_Users.DisplayMember =
                nameof(User.Name);
        }
        finally
        {
            lst_Users.EndUpdate();
        }

        UpdateButtonState();
    }

    // --------------------------------------------------
    // Update button state
    // --------------------------------------------------

    private void UpdateButtonState()
    {
        bool hasUser =
            lst_Users.SelectedItem is User;

        btn_Save.Enabled = true;

        btn_Delete.Enabled =
            hasUser;
    }

    /// <summary>
    /// handles when the selected user index is changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Users_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (lst_Users.SelectedItem
            is not User user)
        {
            txt_Name.Clear();

            UpdateButtonState();

            return;
        }

        txt_Name.Text =
            user.Name;

        txt_Name.SelectAll();

        UpdateButtonState();
    }

    /// <summary>
    /// Handles when the Save button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Save_Click(
        object? sender,
        EventArgs e)
    {
        // If a user is currently selected, save any
        // name changes made in the textbox first.
        if (lst_Users.SelectedItem
            is User user)
        {
            string name =
                txt_Name.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Please enter a name.",
                    "Invalid Name",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txt_Name.Focus();

                return;
            }

            bool duplicate =
                _users.Any(
                    x =>
                        x != user &&
                        string.Equals(
                            x.Name.Trim(),
                            name,
                            StringComparison.OrdinalIgnoreCase));

            if (duplicate)
            {
                MessageBox.Show(
                    "A user with that name already exists.",
                    "Duplicate User",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txt_Name.Focus();

                return;
            }

            user.Name =
                name;
        }

        // The entire _users list is now ready to be
        // returned to MainForm.
        DialogResult =
            DialogResult.OK;
    }

    /// <summary>
    /// handles when the Add button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Add_Click(
        object? sender,
        EventArgs e)
    {
        string name =
            txt_Name.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                "Enter the new user's name.",
                "Invalid Name",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            txt_Name.Focus();

            return;
        }

        bool duplicate =
            _users.Any(
                x =>
                    string.Equals(
                        x.Name.Trim(),
                        name,
                        StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            MessageBox.Show(
                "A user with that name already exists.",
                "Duplicate User",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        // --------------------------------------------------
        // Temporary ID
        //
        // The server will replace this with the correct
        // ID when the complete list is saved.
        // --------------------------------------------------

        int temporaryId =
            _users.Count == 0
                ? 1
                : _users.Max(
                    x => x.UserId) + 1;

        var newUser =
            new User(
                temporaryId,
                name,
                Status.InOffice,
                true);

        _users.Add(
            newUser);

        RefreshUserList();

        lst_Users.SelectedItem =
            newUser;

        txt_Name.Focus();
        txt_Name.SelectAll();
    }

    /// <summary>
    /// handles when the delete button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Delete_Click(
        object? sender,
        EventArgs e)
    {
        if (lst_Users.SelectedItem
            is not User user)
        {
            return;
        }

        var result =
            MessageBox.Show(
                $"Are you sure you want to delete " +
                $"{user.Name}?",

                "Delete User",

                MessageBoxButtons.YesNo,

                MessageBoxIcon.Warning);

        if (result !=
            DialogResult.Yes)
        {
            return;
        }

        _users.Remove(
            user);

        txt_Name.Clear();

        RefreshUserList();
    }

    // --------------------------------------------------
    // Cancel
    // --------------------------------------------------

    private void Cancel_Click(
        object? sender,
        EventArgs e)
    {
        DialogResult =
            DialogResult.Cancel;
    }

    /// <summary>
    /// Creates a clone of a user
    /// </summary>
    /// <param name="user">The user to create a clone of</param>
    /// <returns>The clone of the user</returns>
    private static User CloneUser(
        User user)
    {
        return new User(
            user.UserId,
            user.Name,
            user.Status,
            user.IsAvailable);
    }
}