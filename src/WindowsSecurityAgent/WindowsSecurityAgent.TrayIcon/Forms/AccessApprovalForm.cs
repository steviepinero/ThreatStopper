using System.Windows.Forms;

namespace WindowsSecurityAgent.TrayIcon.Forms;

/// <summary>
/// Form for notifying user of access approval
/// </summary>
public partial class AccessApprovalForm : Form
{
    public AccessApprovalForm(string resourceType, string resourceName, DateTime? expiresAt, bool isApproved)
    {
        // Form setup
        Text = isApproved ? "ThreatStopper - Access Approved" : "ThreatStopper - Access Denied";
        Size = new Size(400, 220);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        Icon = isApproved ? SystemIcons.Information : SystemIcons.Warning;

        // Message label
        var messageLabel = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(340, 120),
            Text = isApproved 
                ? $"ThreatStopper - Access Approved\n\n" +
                  $"Your request to access \"{resourceName}\" has been APPROVED.\n\n" +
                  (expiresAt.HasValue 
                    ? $"This approval expires at {expiresAt.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}.\n\n" +
                      "You can now use this application within the approved time window."
                    : "You can now use this application.")
                : $"ThreatStopper - Access Denied\n\n" +
                  $"Your request to access \"{resourceName}\" has been DENIED.\n\n" +
                  "Please contact your administrator if you believe this is an error.",
            AutoSize = false
        };

        // OK button
        var okButton = new Button
        {
            Location = new Point(150, 150),
            Size = new Size(100, 30),
            Text = "OK",
            DialogResult = DialogResult.OK
        };

        // Add controls
        Controls.Add(messageLabel);
        Controls.Add(okButton);

        AcceptButton = okButton;
    }
}

