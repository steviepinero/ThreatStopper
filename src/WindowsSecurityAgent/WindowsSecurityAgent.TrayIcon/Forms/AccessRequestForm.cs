using System.Windows.Forms;

namespace WindowsSecurityAgent.TrayIcon.Forms;

/// <summary>
/// Form for requesting access to a blocked resource
/// </summary>
public partial class AccessRequestForm : Form
{
    public string Justification { get; private set; } = string.Empty;
    public bool Submitted { get; private set; } = false;

    private readonly string _resourceType;
    private readonly string _resourceName;
    private readonly TextBox _justificationTextBox;
    private readonly Button _submitButton;
    private readonly Button _cancelButton;
    private readonly Label _infoLabel;

    public AccessRequestForm(string resourceType, string resourceName)
    {
        _resourceType = resourceType;
        _resourceName = resourceName;

        // Form setup
        Text = "ThreatStopper Request";
        Size = new Size(500, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        Icon = SystemIcons.Shield;

        // Info label
        _infoLabel = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(440, 60),
            Text = $"The {resourceType.ToLower()} \"{_resourceName}\" has been blocked by ThreatStopper security policy.\n\n" +
                   "To request access, please provide a brief explanation of why you need to use this application:",
            AutoSize = false
        };

        // Justification text box
        _justificationTextBox = new TextBox
        {
            Location = new Point(20, 90),
            Size = new Size(440, 100),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            MaxLength = 500,
            PlaceholderText = "Enter your justification here (required)..."
        };

        // Submit button
        _submitButton = new Button
        {
            Location = new Point(280, 210),
            Size = new Size(90, 30),
            Text = "Submit",
            DialogResult = DialogResult.OK
        };
        _submitButton.Click += SubmitButton_Click;

        // Cancel button
        _cancelButton = new Button
        {
            Location = new Point(380, 210),
            Size = new Size(80, 30),
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        _cancelButton.Click += CancelButton_Click;

        // Add controls
        Controls.Add(_infoLabel);
        Controls.Add(_justificationTextBox);
        Controls.Add(_submitButton);
        Controls.Add(_cancelButton);

        AcceptButton = _submitButton;
        CancelButton = _cancelButton;
    }

    private void SubmitButton_Click(object? sender, EventArgs e)
    {
        var justification = _justificationTextBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(justification))
        {
            MessageBox.Show(
                "Please provide a justification for your access request.",
                "Justification Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (justification.Length < 10)
        {
            MessageBox.Show(
                "Please provide a more detailed justification (at least 10 characters).",
                "Justification Too Short",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Justification = justification;
        Submitted = true;
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        Submitted = false;
        Close();
    }
}

