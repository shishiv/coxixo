namespace Coxixo.Forms;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    // Status panel
    private Panel pnlStatus;
    private Label lblStatusIcon;
    private Label lblStatusText;
    private Label lblLatency;

    // Hotkey
    private Label lblHotkey;
    private TextBox txtHotkey;

    // Azure Endpoint
    private Label lblEndpoint;
    private TextBox txtEndpoint;

    // API Key
    private Label lblApiKey;
    private TextBox txtApiKey;

    // Deployment
    private Label lblDeployment;
    private TextBox txtDeployment;

    // Buttons
    private Button btnTestConnection;
    private Button btnSave;
    private Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // Status Panel
        pnlStatus = new Panel();
        lblStatusIcon = new Label();
        lblStatusText = new Label();
        lblLatency = new Label();

        // Hotkey controls
        lblHotkey = new Label();
        txtHotkey = new TextBox();

        // Endpoint controls
        lblEndpoint = new Label();
        txtEndpoint = new TextBox();

        // API Key controls
        lblApiKey = new Label();
        txtApiKey = new TextBox();

        // Deployment controls
        lblDeployment = new Label();
        txtDeployment = new TextBox();

        // Buttons
        btnTestConnection = new Button();
        btnSave = new Button();
        btnCancel = new Button();

        this.SuspendLayout();

        // pnlStatus
        pnlStatus.Location = new Point(12, 12);
        pnlStatus.Size = new Size(280, 60);
        pnlStatus.Name = "pnlStatus";

        // lblStatusIcon
        lblStatusIcon.AutoSize = true;
        lblStatusIcon.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblStatusIcon.Location = new Point(10, 15);
        lblStatusIcon.Name = "lblStatusIcon";
        lblStatusIcon.Text = "\u25CB";

        // lblStatusText
        lblStatusText.AutoSize = true;
        lblStatusText.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblStatusText.Location = new Point(35, 15);
        lblStatusText.Name = "lblStatusText";
        lblStatusText.Text = "Checking...";

        // lblLatency
        lblLatency.AutoSize = true;
        lblLatency.Font = new Font("Segoe UI", 8F);
        lblLatency.Location = new Point(35, 35);
        lblLatency.Name = "lblLatency";
        lblLatency.Text = "Latency: --ms";
        lblLatency.Visible = false;

        pnlStatus.Controls.Add(lblStatusIcon);
        pnlStatus.Controls.Add(lblStatusText);
        pnlStatus.Controls.Add(lblLatency);

        // lblHotkey
        lblHotkey.AutoSize = true;
        lblHotkey.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblHotkey.Location = new Point(12, 85);
        lblHotkey.Name = "lblHotkey";
        lblHotkey.Text = "HOTKEY";

        // txtHotkey
        txtHotkey.Location = new Point(12, 105);
        txtHotkey.Size = new Size(280, 25);
        txtHotkey.Name = "txtHotkey";
        txtHotkey.ReadOnly = true;
        txtHotkey.Cursor = Cursors.Hand;
        txtHotkey.Enter += TxtHotkey_Enter;
        txtHotkey.Leave += TxtHotkey_Leave;
        txtHotkey.KeyDown += TxtHotkey_KeyDown;

        // lblEndpoint
        lblEndpoint.AutoSize = true;
        lblEndpoint.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblEndpoint.Location = new Point(12, 145);
        lblEndpoint.Name = "lblEndpoint";
        lblEndpoint.Text = "AZURE ENDPOINT";

        // txtEndpoint
        txtEndpoint.Location = new Point(12, 165);
        txtEndpoint.Size = new Size(280, 25);
        txtEndpoint.Name = "txtEndpoint";

        // lblApiKey
        lblApiKey.AutoSize = true;
        lblApiKey.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblApiKey.Location = new Point(12, 205);
        lblApiKey.Name = "lblApiKey";
        lblApiKey.Text = "API KEY";

        // txtApiKey
        txtApiKey.Location = new Point(12, 225);
        txtApiKey.Size = new Size(280, 25);
        txtApiKey.Name = "txtApiKey";
        txtApiKey.UseSystemPasswordChar = true;

        // lblDeployment
        lblDeployment.AutoSize = true;
        lblDeployment.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblDeployment.Location = new Point(12, 265);
        lblDeployment.Name = "lblDeployment";
        lblDeployment.Text = "DEPLOYMENT NAME";

        // txtDeployment
        txtDeployment.Location = new Point(12, 285);
        txtDeployment.Size = new Size(280, 25);
        txtDeployment.Name = "txtDeployment";

        // btnTestConnection
        btnTestConnection.Location = new Point(12, 320);
        btnTestConnection.Size = new Size(110, 25);
        btnTestConnection.Name = "btnTestConnection";
        btnTestConnection.Text = "Test Connection";
        btnTestConnection.Click += BtnTestConnection_Click;

        // btnCancel
        btnCancel.Location = new Point(127, 350);
        btnCancel.Size = new Size(80, 30);
        btnCancel.Name = "btnCancel";
        btnCancel.Text = "Cancel";
        btnCancel.Click += BtnCancel_Click;

        // btnSave
        btnSave.Location = new Point(212, 350);
        btnSave.Size = new Size(80, 30);
        btnSave.Name = "btnSave";
        btnSave.Text = "Save";
        btnSave.Click += BtnSave_Click;

        // SettingsForm
        this.ClientSize = new Size(304, 391);
        this.Controls.Add(pnlStatus);
        this.Controls.Add(lblHotkey);
        this.Controls.Add(txtHotkey);
        this.Controls.Add(lblEndpoint);
        this.Controls.Add(txtEndpoint);
        this.Controls.Add(lblApiKey);
        this.Controls.Add(txtApiKey);
        this.Controls.Add(lblDeployment);
        this.Controls.Add(txtDeployment);
        this.Controls.Add(btnTestConnection);
        this.Controls.Add(btnCancel);
        this.Controls.Add(btnSave);
        this.Name = "SettingsForm";
        this.Text = "Coxixo Settings";

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
