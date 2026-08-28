namespace ToggleAvailabilityApp;

public class PasscodeForm : Form
{
    private readonly TextBox txt_Passcode;
    private readonly Button btn_Continue;
    private readonly Button btn_Cancel;


    public string Passcode =>
        txt_Passcode.Text;


    public PasscodeForm()
    {
        Text =
            "Administrator Access";

        StartPosition =
            FormStartPosition.CenterParent;

        FormBorderStyle =
            FormBorderStyle.FixedDialog;

        MaximizeBox =
            false;

        MinimizeBox =
            false;

        ShowInTaskbar =
            false;

        ClientSize =
            new Size(
                380,
                190);

        BackColor =
            Color.FromArgb(
                25,
                27,
                30);

        ForeColor =
            Color.White;

        Font =
            new Font(
                "Segoe UI",
                10F);


        // --------------------------------------------------
        // Title
        // --------------------------------------------------

        var lbl_Title =
            new Label
            {
                Text =
                    "Administrator Access",

                AutoSize =
                    true,

                Font =
                    new Font(
                        "Segoe UI",
                        14F,
                        FontStyle.Bold),

                ForeColor =
                    Color.White,

                Location =
                    new Point(
                        25,
                        20)
            };


        // --------------------------------------------------
        // Description
        // --------------------------------------------------

        var lbl_Description =
            new Label
            {
                Text =
                    "Enter the administrator passcode to edit users.",

                AutoSize =
                    true,

                ForeColor =
                    Color.FromArgb(
                        170,
                        170,
                        170),

                Location =
                    new Point(
                        25,
                        58)
            };


        // --------------------------------------------------
        // Passcode textbox
        // --------------------------------------------------

        txt_Passcode =
            new TextBox
            {
                Location =
                    new Point(
                        25,
                        88),

                Width =
                    330,

                Height =
                    30,

                UseSystemPasswordChar =
                    true,

                BackColor =
                    Color.FromArgb(
                        12,
                        14,
                        16),

                ForeColor =
                    Color.White,

                BorderStyle =
                    BorderStyle.FixedSingle
            };


        // --------------------------------------------------
        // Continue button
        // --------------------------------------------------

        btn_Continue =
            new Button
            {
                Text =
                    "Continue",

                Width =
                    100,

                Height =
                    32,

                Location =
                    new Point(
                        155,
                        135),

                BackColor =
                    Color.FromArgb(
                        255,
                        195,
                        0),

                ForeColor =
                    Color.Black,

                FlatStyle =
                    FlatStyle.Flat,

                DialogResult =
                    DialogResult.OK
            };

        btn_Continue.FlatAppearance.BorderSize =
            0;


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
                    32,

                Location =
                    new Point(
                        260,
                        135),

                BackColor =
                    Color.FromArgb(
                        50,
                        52,
                        55),

                ForeColor =
                    Color.White,

                FlatStyle =
                    FlatStyle.Flat,

                DialogResult =
                    DialogResult.Cancel
            };

        btn_Cancel.FlatAppearance.BorderSize =
            0;


        // --------------------------------------------------
        // Enter key
        // --------------------------------------------------

        AcceptButton =
            btn_Continue;

        CancelButton =
            btn_Cancel;


        Controls.Add(
            lbl_Title);

        Controls.Add(
            lbl_Description);

        Controls.Add(
            txt_Passcode);

        Controls.Add(
            btn_Continue);

        Controls.Add(
            btn_Cancel);


        Shown +=
            (_, _) =>
            {
                txt_Passcode.Focus();
            };
    }
}
