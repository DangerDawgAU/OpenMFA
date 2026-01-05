using System;
using System.Windows.Forms;
using OpenMFA.SmartCard.MyEid;

namespace OpenMFA.GUI;

/// <summary>
/// Simplified GUI for MyEID card setup for Windows smart card logon
/// </summary>
public partial class WindowsLogonForm : Form
{
    private MyEidCard? _card;
    private uint? _readerNumber;

    // UI Controls - initialized in InitializeComponent()
    private GroupBox grpReader = null!;
    private Button btnDetect = null!;
    private Label lblReaderStatus = null!;
    private TextBox txtReaderStatus = null!;

    private GroupBox grpInitialize = null!;
    private Label lblUserPin = null!;
    private TextBox txtUserPin = null!;
    private Label lblUserPuk = null!;
    private TextBox txtUserPuk = null!;
    private Label lblSoPin = null!;
    private TextBox txtSoPin = null!;
    private Label lblSoPuk = null!;
    private TextBox txtSoPuk = null!;
    private Button btnInitialize = null!;
    private Button btnErase = null!;

    private GroupBox grpKeys = null!;
    private Label lblKeySize = null!;
    private ComboBox cmbKeySize = null!;
    private Label lblKeyPin = null!;
    private TextBox txtKeyPin = null!;
    private Button btnGenerateKey = null!;

    private GroupBox grpCertificate = null!;
    private Label lblCertPin = null!;
    private TextBox txtCertPin = null!;
    private Button btnImportCert = null!;
    private Button btnExportCert = null!;
    private Button btnVerifyCard = null!;

    private TextBox txtOutput = null!;

    public WindowsLogonForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "MyEID - Windows Smart Card Logon Setup";
        this.Size = new System.Drawing.Size(900, 700);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new System.Drawing.Size(900, 700);

        int yPos = 10;

        // Reader Detection Group
        grpReader = new GroupBox
        {
            Text = "1. Detect Card Reader",
            Location = new System.Drawing.Point(10, yPos),
            Size = new System.Drawing.Size(860, 80)
        };

        btnDetect = new Button
        {
            Text = "Detect Reader && Card",
            Location = new System.Drawing.Point(10, 25),
            Size = new System.Drawing.Size(150, 35)
        };
        btnDetect.Click += BtnDetect_Click;

        lblReaderStatus = new Label
        {
            Text = "Status:",
            Location = new System.Drawing.Point(170, 30),
            AutoSize = true
        };

        txtReaderStatus = new TextBox
        {
            Location = new System.Drawing.Point(220, 27),
            Size = new System.Drawing.Size(620, 23),
            ReadOnly = true,
            Text = "No reader detected"
        };

        grpReader.Controls.AddRange(new Control[] { btnDetect, lblReaderStatus, txtReaderStatus });

        yPos += 90;

        // Initialize Card Group
        grpInitialize = new GroupBox
        {
            Text = "2. Initialize Card (Erases all data!)",
            Location = new System.Drawing.Point(10, yPos),
            Size = new System.Drawing.Size(860, 110)
        };

        lblUserPin = new Label { Text = "User PIN:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(100, 20) };
        txtUserPin = new TextBox { Location = new System.Drawing.Point(110, 22), Size = new System.Drawing.Size(80, 23), Text = "1111" };

        lblUserPuk = new Label { Text = "User PUK:", Location = new System.Drawing.Point(210, 25), Size = new System.Drawing.Size(100, 20) };
        txtUserPuk = new TextBox { Location = new System.Drawing.Point(310, 22), Size = new System.Drawing.Size(80, 23), Text = "111111" };

        lblSoPin = new Label { Text = "SO PIN:", Location = new System.Drawing.Point(410, 25), Size = new System.Drawing.Size(100, 20) };
        txtSoPin = new TextBox { Location = new System.Drawing.Point(510, 22), Size = new System.Drawing.Size(100, 23), Text = "12345678" };

        lblSoPuk = new Label { Text = "SO PUK:", Location = new System.Drawing.Point(630, 25), Size = new System.Drawing.Size(100, 20) };
        txtSoPuk = new TextBox { Location = new System.Drawing.Point(730, 22), Size = new System.Drawing.Size(100, 23), Text = "12345678" };

        btnInitialize = new Button
        {
            Text = "Initialize Card",
            Location = new System.Drawing.Point(10, 60),
            Size = new System.Drawing.Size(150, 35),
            Enabled = false
        };
        btnInitialize.Click += BtnInitialize_Click;

        btnErase = new Button
        {
            Text = "ERASE Card",
            Location = new System.Drawing.Point(170, 60),
            Size = new System.Drawing.Size(150, 35),
            BackColor = System.Drawing.Color.DarkRed,
            ForeColor = System.Drawing.Color.White,
            Enabled = false
        };
        btnErase.Click += BtnErase_Click;

        grpInitialize.Controls.AddRange(new Control[]
        {
            lblUserPin, txtUserPin, lblUserPuk, txtUserPuk,
            lblSoPin, txtSoPin, lblSoPuk, txtSoPuk,
            btnInitialize, btnErase
        });

        yPos += 120;

        // Key Generation Group
        grpKeys = new GroupBox
        {
            Text = "3. Generate Authentication Key (Slot 9A)",
            Location = new System.Drawing.Point(10, yPos),
            Size = new System.Drawing.Size(860, 80)
        };

        lblKeySize = new Label { Text = "Key Size:", Location = new System.Drawing.Point(10, 30), AutoSize = true };
        cmbKeySize = new ComboBox
        {
            Location = new System.Drawing.Point(80, 27),
            Size = new System.Drawing.Size(120, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbKeySize.Items.AddRange(new object[] { "RSA 2048", "RSA 3072", "RSA 4096" });
        cmbKeySize.SelectedIndex = 0;

        lblKeyPin = new Label { Text = "PIN:", Location = new System.Drawing.Point(220, 30), AutoSize = true };
        txtKeyPin = new TextBox { Location = new System.Drawing.Point(260, 27), Size = new System.Drawing.Size(100, 23), Text = "1111" };

        btnGenerateKey = new Button
        {
            Text = "Generate Key Pair",
            Location = new System.Drawing.Point(380, 23),
            Size = new System.Drawing.Size(150, 35),
            Enabled = false
        };
        btnGenerateKey.Click += BtnGenerateKey_Click;

        grpKeys.Controls.AddRange(new Control[]
        {
            lblKeySize, cmbKeySize, lblKeyPin, txtKeyPin, btnGenerateKey
        });

        yPos += 90;

        // Certificate Group
        grpCertificate = new GroupBox
        {
            Text = "4. Certificate Operations",
            Location = new System.Drawing.Point(10, yPos),
            Size = new System.Drawing.Size(860, 80)
        };

        lblCertPin = new Label { Text = "PIN:", Location = new System.Drawing.Point(10, 30), AutoSize = true };
        txtCertPin = new TextBox { Location = new System.Drawing.Point(50, 27), Size = new System.Drawing.Size(100, 23), Text = "1111" };

        btnImportCert = new Button
        {
            Text = "Import Certificate",
            Location = new System.Drawing.Point(170, 23),
            Size = new System.Drawing.Size(150, 35),
            Enabled = false
        };
        btnImportCert.Click += BtnImportCert_Click;

        btnExportCert = new Button
        {
            Text = "Export Certificate",
            Location = new System.Drawing.Point(330, 23),
            Size = new System.Drawing.Size(150, 35),
            Enabled = false
        };
        btnExportCert.Click += BtnExportCert_Click;

        btnVerifyCard = new Button
        {
            Text = "Verify Card Setup",
            Location = new System.Drawing.Point(490, 23),
            Size = new System.Drawing.Size(150, 35),
            Enabled = false
        };
        btnVerifyCard.Click += BtnVerifyCard_Click;

        grpCertificate.Controls.AddRange(new Control[]
        {
            lblCertPin, txtCertPin, btnImportCert, btnExportCert, btnVerifyCard
        });

        yPos += 90;

        // Output Log
        var lblOutput = new Label
        {
            Text = "Output Log:",
            Location = new System.Drawing.Point(10, yPos),
            AutoSize = true
        };

        txtOutput = new TextBox
        {
            Location = new System.Drawing.Point(10, yPos + 25),
            Size = new System.Drawing.Size(860, 200),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new System.Drawing.Font("Consolas", 9)
        };

        // Add all controls to form
        this.Controls.AddRange(new Control[]
        {
            grpReader, grpInitialize, grpKeys, grpCertificate, lblOutput, txtOutput
        });

        // Make form resizable
        this.SizeChanged += (s, e) =>
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                ResizeControls();
            }
        };
    }

    private void ResizeControls()
    {
        int width = this.ClientSize.Width - 30;

        grpReader.Width = width;
        grpInitialize.Width = width;
        grpKeys.Width = width;
        grpCertificate.Width = width;
        txtOutput.Width = width;

        txtReaderStatus.Width = width - 230;

        // Adjust output height
        int outputY = grpCertificate.Bottom + 35;
        int availableHeight = this.ClientSize.Height - outputY - 20;
        if (availableHeight > 100)
        {
            txtOutput.Location = new System.Drawing.Point(10, outputY);
            txtOutput.Height = availableHeight;
        }
    }

    private void Log(string message)
    {
        if (txtOutput.InvokeRequired)
        {
            txtOutput.Invoke(() => Log(message));
            return;
        }

        txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        txtOutput.SelectionStart = txtOutput.Text.Length;
        txtOutput.ScrollToCaret();
    }

    private async void BtnDetect_Click(object? sender, EventArgs e)
    {
        btnDetect.Enabled = false;
        try
        {
            Log("Detecting card readers...");
            var readerNum = await CardReader.GetFirstReaderWithCardAsync(Log);

            if (readerNum.HasValue)
            {
                _readerNumber = readerNum;
                _card = new MyEidCard(_readerNumber);
                _card.SetLogger(Log);

                var readers = await CardReader.DetectReadersAsync(Log);
                var reader = readers.FirstOrDefault(r => r.Number == readerNum);

                txtReaderStatus.Text = $"Reader {readerNum}: {reader?.Name ?? "Unknown"} - Card present";
                txtReaderStatus.BackColor = System.Drawing.Color.LightGreen;

                Log($"✓ Reader detected: {reader?.Name}");

                // Enable operations
                btnInitialize.Enabled = true;
                btnErase.Enabled = true;
                btnGenerateKey.Enabled = true;
                btnImportCert.Enabled = true;
                btnExportCert.Enabled = true;
                btnVerifyCard.Enabled = true;
            }
            else
            {
                txtReaderStatus.Text = "No card detected";
                txtReaderStatus.BackColor = System.Drawing.Color.LightCoral;
                Log("✗ No card reader with card found");
            }
        }
        catch (Exception ex)
        {
            Log($"✗ Error: {ex.Message}");
            txtReaderStatus.Text = $"Error: {ex.Message}";
            txtReaderStatus.BackColor = System.Drawing.Color.LightCoral;
        }
        finally
        {
            btnDetect.Enabled = true;
        }
    }

    private async void BtnInitialize_Click(object? sender, EventArgs e)
    {
        if (_card == null)
        {
            MessageBox.Show("Please detect card first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var result = MessageBox.Show(
            "WARNING: This will ERASE all data on the card!\n\n" +
            "This will:\n" +
            "• Delete all keys and certificates\n" +
            "• Set new PIN/PUK codes\n" +
            "• Prepare card for Windows logon\n\n" +
            "Continue?",
            "Confirm Card Initialization",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        btnInitialize.Enabled = false;
        try
        {
            Log("=== Initializing Card ===");
            await _card.InitializeAsync(
                txtUserPin.Text,
                txtUserPuk.Text,
                txtSoPin.Text,
                txtSoPuk.Text);
            Log("✓ Card initialized successfully!");
            MessageBox.Show("Card initialized successfully!\n\nYou can now generate a key pair.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"✗ Initialization failed: {ex.Message}");
            MessageBox.Show($"Initialization failed:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnInitialize.Enabled = true;
        }
    }

    private void BtnErase_Click(object? sender, EventArgs e)
    {
        if (!_readerNumber.HasValue)
        {
            MessageBox.Show("Please detect card first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var result = MessageBox.Show(
            "⚠️ CRITICAL WARNING ⚠️\n\n" +
            "This will COMPLETELY ERASE the card!\n\n" +
            "IMPORTANT: You need the CURRENT SO-PIN that's on the card.\n" +
            "This is NOT the new SO-PIN you want to set - it's the one that was\n" +
            "used when the card was last initialized.\n\n" +
            "A terminal window will open where you must enter the CURRENT SO-PIN.\n\n" +
            "Common default SO-PINs to try:\n" +
            "  • 12345678 (OpenSC default)\n" +
            "  • 00000000 (some manufacturers)\n" +
            "  • 3537363231383830 (hex ASCII for '57621880')\n\n" +
            "If you don't know the SO-PIN and all defaults fail, the card\n" +
            "may need to be factory reset by the manufacturer.\n\n" +
            "ALL data will be PERMANENTLY DELETED.\n\n" +
            "Are you ABSOLUTELY SURE?",
            "Confirm Card Erasure",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Stop,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes) return;

        try
        {
            Log("=== Erasing Card (Interactive) ===");
            Log($"Opening terminal window...");
            Log($"IMPORTANT: You need to enter the CURRENT SO-PIN on the card");
            Log($"Try common defaults if you don't know it:");
            Log($"  • 12345678 (OpenSC default)");
            Log($"  • 00000000 (some manufacturers)");
            Log($"  • 3537363231383830 (hex ASCII)");

            // Launch pkcs15-init in an interactive terminal
            var pkcs15Init = @"C:\Program Files\OpenSC Project\OpenSC\tools\pkcs15-init.exe";
            var args = $"--erase-card --reader {_readerNumber}";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K \"\"{pkcs15Init}\" {args}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var process = System.Diagnostics.Process.Start(psi);

            Log("✓ Terminal window opened");

            MessageBox.Show(
                "A terminal window has opened.\n\n" +
                "IMPORTANT: Enter the CURRENT SO-PIN (not the new one)\n\n" +
                "Try these common defaults:\n" +
                "  1. 12345678 (most common)\n" +
                "  2. 00000000\n" +
                "  3. 3537363231383830\n\n" +
                "If 'invalid length' error appears, the PIN format might be wrong.\n" +
                "If 'incorrect PIN' appears, try another default.\n\n" +
                "⚠️ LOCKOUT WARNING:\n" +
                "After ~5 failed SO-PIN attempts, the card will automatically\n" +
                "factory reset itself, becoming blank/uninitialized.\n" +
                "If this happens, you can re-initialize without needing SO-PIN.\n\n" +
                "Close the terminal window when done and click OK.",
                "Interactive Erase - Enter CURRENT SO-PIN",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            Log($"✗ Failed to launch erase tool: {ex.Message}");
            MessageBox.Show($"Failed to launch erase tool:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnGenerateKey_Click(object? sender, EventArgs e)
    {
        if (_card == null) return;

        var keySize = cmbKeySize.SelectedItem?.ToString()?.Replace("RSA ", "") ?? "2048";

        btnGenerateKey.Enabled = false;
        try
        {
            Log($"=== Generating RSA {keySize} Key Pair ===");
            Log("⏳ This may take 30-90 seconds, please wait...");
            Application.DoEvents();

            await _card.GenerateAuthenticationKeyAsync(txtKeyPin.Text, int.Parse(keySize));

            Log($"✓ Key pair generated successfully in slot 9A!");
            MessageBox.Show($"RSA {keySize} key pair generated in slot 9A (Authentication)!\n\nYou can now import a certificate.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"✗ Key generation failed: {ex.Message}");
            MessageBox.Show($"Key generation failed:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnGenerateKey.Enabled = true;
        }
    }

    private async void BtnImportCert_Click(object? sender, EventArgs e)
    {
        if (_card == null) return;

        using var dialog = new OpenFileDialog
        {
            Filter = "Certificate files (*.der;*.crt;*.cer)|*.der;*.crt;*.cer|All files (*.*)|*.*",
            Title = "Select Certificate to Import"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        btnImportCert.Enabled = false;
        try
        {
            Log($"=== Importing Certificate ===");
            var certData = await File.ReadAllBytesAsync(dialog.FileName);
            Log($"Certificate file: {Path.GetFileName(dialog.FileName)} ({certData.Length} bytes)");

            await _card.StoreCertificateAsync(certData, txtCertPin.Text);

            Log("✓ Certificate imported successfully to slot 9A!");
            MessageBox.Show("Certificate imported successfully!\n\nCard is ready for Windows logon.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"✗ Import failed: {ex.Message}");
            MessageBox.Show($"Certificate import failed:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnImportCert.Enabled = true;
        }
    }

    private async void BtnExportCert_Click(object? sender, EventArgs e)
    {
        if (_card == null) return;

        using var dialog = new SaveFileDialog
        {
            Filter = "Certificate files (*.der)|*.der|All files (*.*)|*.*",
            DefaultExt = "der",
            FileName = "certificate.der"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        btnExportCert.Enabled = false;
        try
        {
            Log("=== Exporting Certificate ===");
            var certData = await _card.ReadCertificateAsync();

            if (certData.Length == 0)
            {
                Log("✗ No certificate found on card");
                MessageBox.Show("No certificate found on card.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await File.WriteAllBytesAsync(dialog.FileName, certData);
            Log($"✓ Certificate exported: {Path.GetFileName(dialog.FileName)} ({certData.Length} bytes)");
            MessageBox.Show($"Certificate exported successfully!\n\n{dialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"✗ Export failed: {ex.Message}");
            MessageBox.Show($"Certificate export failed:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExportCert.Enabled = true;
        }
    }

    private async void BtnVerifyCard_Click(object? sender, EventArgs e)
    {
        if (_card == null) return;

        btnVerifyCard.Enabled = false;
        try
        {
            Log("=== Verifying Card Setup ===");

            var objects = await _card.ListObjectsAsync();
            Log("Card contents:");
            Log(objects);

            Log("✓ Verification complete - check output above");
        }
        catch (Exception ex)
        {
            Log($"✗ Verification failed: {ex.Message}");
        }
        finally
        {
            btnVerifyCard.Enabled = true;
        }
    }
}
