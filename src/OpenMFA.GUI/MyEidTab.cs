using System;
using System.Windows.Forms;
using OpenMFA.SmartCard.OpenSC;

namespace OpenMFA.GUI;

/// <summary>
/// MyEID-specific operations tab for card initialization and management
/// </summary>
public partial class MyEidTab : UserControl
{
    private MyEidOperations? _myeid;
    private uint? _readerNumber;

    // Controls
    private GroupBox grpInitialize;
    private Label lblUserPin;
    private TextBox txtUserPin;
    private Label lblUserPuk;
    private TextBox txtUserPuk;
    private Label lblSoPin;
    private TextBox txtSoPin;
    private Label lblSoPuk;
    private TextBox txtSoPuk;
    private Label lblErasePin;
    private TextBox txtErasePin;
    private Button btnInitialize;
    private Button btnFinalize;
    private Button btnErase;

    private GroupBox grpKeyGen;
    private Label lblKeyType;
    private ComboBox cmbKeyType;
    private Label lblKeyId;
    private TextBox txtKeyId;
    private Label lblKeyLabel;
    private TextBox txtKeyLabel;
    private Label lblKeyPin;
    private TextBox txtKeyPin;
    private Button btnGenerateKey;

    private GroupBox grpCertOps;
    private Label lblCertId;
    private TextBox txtCertId;
    private Label lblCertLabel;
    private TextBox txtCertLabel;
    private Label lblCertPin;
    private TextBox txtCertPin;
    private Button btnStoreCert;
    private Button btnReadCert;
    private Button btnDeleteCert;
    private Button btnListAll;

    private TextBox txtOutput;

    public MyEidTab()
    {
        InitializeComponents();
        SetDefaultValues();
    }

    public void SetReader(uint? readerNumber)
    {
        _readerNumber = readerNumber;
        _myeid = new MyEidOperations(readerNumber);
    }

    private void InitializeComponents()
    {
        // Initialize tab
        this.Size = new System.Drawing.Size(760, 500);
        this.AutoScroll = true;

        // Initialize Card Group
        grpInitialize = new GroupBox
        {
            Text = "Card Initialization & Erasure",
            Location = new System.Drawing.Point(10, 10),
            Size = new System.Drawing.Size(740, 140)
        };

        lblUserPin = new Label { Text = "User PIN:", Location = new System.Drawing.Point(10, 25), AutoSize = true };
        txtUserPin = new TextBox { Location = new System.Drawing.Point(120, 22), Size = new System.Drawing.Size(100, 23), Text = "1111" };

        lblUserPuk = new Label { Text = "User PUK:", Location = new System.Drawing.Point(10, 55), AutoSize = true };
        txtUserPuk = new TextBox { Location = new System.Drawing.Point(120, 52), Size = new System.Drawing.Size(100, 23), Text = "1111" };

        lblSoPin = new Label { Text = "SO PIN:", Location = new System.Drawing.Point(250, 25), AutoSize = true };
        txtSoPin = new TextBox { Location = new System.Drawing.Point(340, 22), Size = new System.Drawing.Size(100, 23), Text = "12345678" };

        lblSoPuk = new Label { Text = "SO PUK:", Location = new System.Drawing.Point(250, 55), AutoSize = true };
        txtSoPuk = new TextBox { Location = new System.Drawing.Point(340, 52), Size = new System.Drawing.Size(100, 23), Text = "12345678" };

        lblErasePin = new Label { Text = "Erase SO-PIN:", Location = new System.Drawing.Point(470, 25), AutoSize = true };
        txtErasePin = new TextBox { Location = new System.Drawing.Point(565, 22), Size = new System.Drawing.Size(100, 23), Text = "12345678", PasswordChar = '*' };

        btnInitialize = new Button
        {
            Text = "Initialize Card",
            Location = new System.Drawing.Point(10, 95),
            Size = new System.Drawing.Size(150, 30)
        };
        btnInitialize.Click += BtnInitialize_Click;

        btnFinalize = new Button
        {
            Text = "Finalize Card",
            Location = new System.Drawing.Point(170, 95),
            Size = new System.Drawing.Size(150, 30)
        };
        btnFinalize.Click += BtnFinalize_Click;

        btnErase = new Button
        {
            Text = "ERASE Card",
            Location = new System.Drawing.Point(330, 95),
            Size = new System.Drawing.Size(150, 30),
            BackColor = System.Drawing.Color.DarkRed,
            ForeColor = System.Drawing.Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnErase.Click += BtnErase_Click;

        grpInitialize.Controls.AddRange(new Control[]
        {
            lblUserPin, txtUserPin, lblUserPuk, txtUserPuk,
            lblSoPin, txtSoPin, lblSoPuk, txtSoPuk,
            lblErasePin, txtErasePin,
            btnInitialize, btnFinalize, btnErase
        });

        // Key Generation Group
        grpKeyGen = new GroupBox
        {
            Text = "Key Generation",
            Location = new System.Drawing.Point(10, 160),
            Size = new System.Drawing.Size(740, 110)
        };

        lblKeyType = new Label { Text = "Key Type:", Location = new System.Drawing.Point(10, 25), AutoSize = true };
        cmbKeyType = new ComboBox
        {
            Location = new System.Drawing.Point(100, 22),
            Size = new System.Drawing.Size(150, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbKeyType.Items.AddRange(new object[]
        {
            "RSA 2048", "RSA 3072", "RSA 4096",
            "ECC P-256", "ECC P-384", "ECC P-521"
        });
        cmbKeyType.SelectedIndex = 0;

        lblKeyId = new Label { Text = "ID:", Location = new System.Drawing.Point(270, 25), AutoSize = true };
        txtKeyId = new TextBox { Location = new System.Drawing.Point(300, 22), Size = new System.Drawing.Size(50, 23), Text = "01" };

        lblKeyLabel = new Label { Text = "Label:", Location = new System.Drawing.Point(370, 25), AutoSize = true };
        txtKeyLabel = new TextBox { Location = new System.Drawing.Point(420, 22), Size = new System.Drawing.Size(150, 23), Text = "PIV AUTH" };

        lblKeyPin = new Label { Text = "PIN:", Location = new System.Drawing.Point(590, 25), AutoSize = true };
        txtKeyPin = new TextBox { Location = new System.Drawing.Point(625, 22), Size = new System.Drawing.Size(100, 23), Text = "1111" };

        btnGenerateKey = new Button
        {
            Text = "Generate Key Pair",
            Location = new System.Drawing.Point(10, 60),
            Size = new System.Drawing.Size(150, 30)
        };
        btnGenerateKey.Click += BtnGenerateKey_Click;

        grpKeyGen.Controls.AddRange(new Control[]
        {
            lblKeyType, cmbKeyType, lblKeyId, txtKeyId,
            lblKeyLabel, txtKeyLabel, lblKeyPin, txtKeyPin,
            btnGenerateKey
        });

        // Certificate Operations Group
        grpCertOps = new GroupBox
        {
            Text = "Certificate Operations",
            Location = new System.Drawing.Point(10, 280),
            Size = new System.Drawing.Size(740, 110)
        };

        lblCertId = new Label { Text = "ID:", Location = new System.Drawing.Point(10, 25), AutoSize = true };
        txtCertId = new TextBox { Location = new System.Drawing.Point(50, 22), Size = new System.Drawing.Size(50, 23), Text = "01" };

        lblCertLabel = new Label { Text = "Label:", Location = new System.Drawing.Point(120, 25), AutoSize = true };
        txtCertLabel = new TextBox { Location = new System.Drawing.Point(170, 22), Size = new System.Drawing.Size(150, 23), Text = "My Certificate" };

        lblCertPin = new Label { Text = "PIN:", Location = new System.Drawing.Point(340, 25), AutoSize = true };
        txtCertPin = new TextBox { Location = new System.Drawing.Point(380, 22), Size = new System.Drawing.Size(100, 23), Text = "1111" };

        btnStoreCert = new Button
        {
            Text = "Store Certificate",
            Location = new System.Drawing.Point(10, 60),
            Size = new System.Drawing.Size(130, 30)
        };
        btnStoreCert.Click += BtnStoreCert_Click;

        btnReadCert = new Button
        {
            Text = "Read Certificate",
            Location = new System.Drawing.Point(150, 60),
            Size = new System.Drawing.Size(130, 30)
        };
        btnReadCert.Click += BtnReadCert_Click;

        btnDeleteCert = new Button
        {
            Text = "Delete Certificate",
            Location = new System.Drawing.Point(290, 60),
            Size = new System.Drawing.Size(130, 30)
        };
        btnDeleteCert.Click += BtnDeleteCert_Click;

        btnListAll = new Button
        {
            Text = "List All Objects",
            Location = new System.Drawing.Point(430, 60),
            Size = new System.Drawing.Size(130, 30)
        };
        btnListAll.Click += BtnListAll_Click;

        grpCertOps.Controls.AddRange(new Control[]
        {
            lblCertId, txtCertId, lblCertLabel, txtCertLabel,
            lblCertPin, txtCertPin, btnStoreCert, btnReadCert,
            btnDeleteCert, btnListAll
        });

        // Output TextBox
        txtOutput = new TextBox
        {
            Location = new System.Drawing.Point(10, 400),
            Size = new System.Drawing.Size(740, 90),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new System.Drawing.Font("Consolas", 9)
        };

        // Add all controls to tab
        this.Controls.AddRange(new Control[]
        {
            grpInitialize, grpKeyGen, grpCertOps, txtOutput
        });
    }

    private void SetDefaultValues()
    {
        // Default values are set in control initialization
    }

    private void AppendOutput(string message)
    {
        if (txtOutput.InvokeRequired)
        {
            txtOutput.Invoke(new Action<string>(AppendOutput), message);
        }
        else
        {
            txtOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtOutput.SelectionStart = txtOutput.Text.Length;
            txtOutput.ScrollToCaret();
        }
    }

    private async void BtnInitialize_Click(object? sender, EventArgs e)
    {
        if (_myeid == null)
        {
            MessageBox.Show("Please detect card first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var result = MessageBox.Show(
            "WARNING: This will ERASE all data on the card!\n\nAre you sure you want to initialize the card?",
            "Confirm Initialization",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        try
        {
            btnInitialize.Enabled = false;
            AppendOutput("=== Card Initialization Started ===");
            AppendOutput("Erasing card and creating PKCS#15 structure...");
            Application.DoEvents(); // Force UI update

            var success = await _myeid.InitializeCardAsync(
                txtUserPin.Text,
                txtUserPuk.Text,
                txtSoPin.Text,
                txtSoPuk.Text);

            if (success)
                AppendOutput("✓ Card initialized successfully!");
            else
                AppendOutput("✗ Card initialization failed.");
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
            MessageBox.Show(ex.Message, "Initialization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnInitialize.Enabled = true;
            AppendOutput("=== Initialization Complete ===");
        }
    }

    private async void BtnFinalize_Click(object? sender, EventArgs e)
    {
        if (_myeid == null) return;

        try
        {
            btnFinalize.Enabled = false;
            AppendOutput("=== Card Finalization Started ===");
            AppendOutput("Locking card configuration...");
            Application.DoEvents(); // Force UI update

            await _myeid.FinalizeCardAsync();
            AppendOutput("✓ Card finalized successfully!");
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
        }
        finally
        {
            btnFinalize.Enabled = true;
            AppendOutput("=== Finalization Complete ===");
        }
    }

    private async void BtnGenerateKey_Click(object? sender, EventArgs e)
    {
        if (_myeid == null) return;

        try
        {
            btnGenerateKey.Enabled = false;
            var keyType = cmbKeyType.SelectedItem?.ToString() ?? "RSA 2048";

            AppendOutput("=== Key Generation Started ===");
            AppendOutput($"Generating {keyType} key pair...");
            AppendOutput("⏳ This may take 30-60 seconds for RSA 4096 - please wait...");
            Application.DoEvents(); // Force UI update

            string result;
            if (keyType.StartsWith("RSA"))
            {
                var keySize = int.Parse(keyType.Split(' ')[1]);
                result = await _myeid.GenerateRsaKeyAsync(
                    keySize,
                    txtKeyId.Text,
                    txtKeyId.Text,
                    txtKeyLabel.Text,
                    txtKeyPin.Text);
            }
            else
            {
                var curve = keyType switch
                {
                    "ECC P-256" => "prime256v1",
                    "ECC P-384" => "secp384r1",
                    "ECC P-521" => "secp521r1",
                    _ => "prime256v1"
                };
                result = await _myeid.GenerateEccKeyAsync(
                    curve,
                    txtKeyId.Text,
                    txtKeyId.Text,
                    txtKeyLabel.Text,
                    txtKeyPin.Text);
            }

            AppendOutput("✓ Key pair generated successfully!");
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
        }
        finally
        {
            btnGenerateKey.Enabled = true;
            AppendOutput("=== Key Generation Complete ===");
        }
    }

    private async void BtnStoreCert_Click(object? sender, EventArgs e)
    {
        if (_myeid == null) return;

        using var openDialog = new OpenFileDialog
        {
            Filter = "Certificate files (*.crt;*.cer;*.der)|*.crt;*.cer;*.der|All files (*.*)|*.*",
            Title = "Select Certificate File"
        };

        if (openDialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            btnStoreCert.Enabled = false;
            AppendOutput("=== Certificate Storage Started ===");
            AppendOutput($"Storing certificate: {Path.GetFileName(openDialog.FileName)}");
            Application.DoEvents(); // Force UI update

            await _myeid.StoreCertificateAsync(
                openDialog.FileName,
                txtCertId.Text,
                txtCertLabel.Text,
                txtCertPin.Text);
            AppendOutput("✓ Certificate stored successfully!");
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
        }
        finally
        {
            btnStoreCert.Enabled = true;
            AppendOutput("=== Certificate Storage Complete ===");
        }
    }

    private async void BtnReadCert_Click(object? sender, EventArgs e)
    {
        if (_myeid == null) return;

        using var saveDialog = new SaveFileDialog
        {
            Filter = "Certificate files (*.der)|*.der|All files (*.*)|*.*",
            DefaultExt = "der",
            FileName = $"cert_{txtCertId.Text}.der"
        };

        if (saveDialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            btnReadCert.Enabled = false;
            AppendOutput("=== Certificate Read Started ===");
            AppendOutput($"Reading certificate ID: {txtCertId.Text}");
            Application.DoEvents(); // Force UI update

            var certData = await _myeid.ReadCertificateAsync(txtCertId.Text);

            if (certData.Length > 0)
            {
                await File.WriteAllBytesAsync(saveDialog.FileName, certData);
                AppendOutput($"✓ Certificate saved to {Path.GetFileName(saveDialog.FileName)} ({certData.Length} bytes)");
            }
            else
            {
                AppendOutput("✗ No certificate found with that ID");
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
        }
        finally
        {
            btnReadCert.Enabled = true;
            AppendOutput("=== Certificate Read Complete ===");
        }
    }

    private async void BtnDeleteCert_Click(object? sender, EventArgs e)
    {
        if (_myeid == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete certificate ID {txtCertId.Text}?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        try
        {
            btnDeleteCert.Enabled = false;
            AppendOutput("=== Certificate Deletion Started ===");
            AppendOutput($"Deleting certificate ID: {txtCertId.Text}");
            Application.DoEvents(); // Force UI update

            await _myeid.DeleteCertificateAsync(txtCertId.Text, txtCertPin.Text);
            AppendOutput("✓ Certificate deleted successfully!");
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
        }
        finally
        {
            btnDeleteCert.Enabled = true;
            AppendOutput("=== Certificate Deletion Complete ===");
        }
    }

    private async void BtnListAll_Click(object? sender, EventArgs e)
    {
        if (_myeid == null) return;

        try
        {
            btnListAll.Enabled = false;
            AppendOutput("=== Card Object Listing Started ===");
            AppendOutput("Reading all objects from card...");
            Application.DoEvents(); // Force UI update

            var dump = await _myeid.DumpCardAsync();
            AppendOutput("--- Card Contents ---");
            AppendOutput(dump);
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
        }
        finally
        {
            btnListAll.Enabled = true;
            AppendOutput("=== Listing Complete ===");
        }
    }

    private async void BtnErase_Click(object? sender, EventArgs e)
    {
        if (_myeid == null)
        {
            MessageBox.Show("Please detect card first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var result = MessageBox.Show(
            "⚠️ CRITICAL WARNING ⚠️\n\n" +
            "This will COMPLETELY ERASE the card and return it to FACTORY STATE!\n\n" +
            "ALL data will be PERMANENTLY DELETED:\n" +
            "• All private keys\n" +
            "• All certificates\n" +
            "• All data objects\n" +
            "• All PIN configurations\n\n" +
            "This operation is IRREVERSIBLE!\n\n" +
            "Are you ABSOLUTELY SURE you want to erase this card?",
            "⚠️ CONFIRM CARD ERASURE ⚠️",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
            return;

        // Double confirmation
        var secondConfirm = MessageBox.Show(
            "FINAL CONFIRMATION:\n\n" +
            "Click YES to PERMANENTLY ERASE the card.\n" +
            "Click NO to cancel and keep the card as-is.\n\n" +
            "This is your last chance to cancel!",
            "Final Confirmation Required",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Stop,
            MessageBoxDefaultButton.Button2);

        if (secondConfirm != DialogResult.Yes)
        {
            AppendOutput("Card erasure cancelled by user.");
            return;
        }

        try
        {
            btnErase.Enabled = false;
            AppendOutput("=== CARD ERASURE STARTED ===");
            AppendOutput("⚠️ ERASING CARD - DO NOT REMOVE CARD! ⚠️");
            AppendOutput("Wiping all keys, certificates, and data...");
            Application.DoEvents(); // Force UI update

            // Use the erase PIN if provided, otherwise try without SO-PIN
            var erasePin = string.IsNullOrWhiteSpace(txtErasePin.Text) ? null : txtErasePin.Text;

            var success = await _myeid.EraseCardAsync(erasePin);

            if (success)
            {
                AppendOutput("✓ Card erased successfully!");
                AppendOutput("Card is now in factory state - ready for initialization.");
                MessageBox.Show(
                    "Card has been erased successfully!\n\n" +
                    "The card is now blank and ready to be initialized with new settings.",
                    "Erase Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                AppendOutput("✗ Card erasure failed.");
                MessageBox.Show(
                    "Card erasure failed!\n\n" +
                    "Possible causes:\n" +
                    "• Incorrect SO-PIN\n" +
                    "• Card is already blank\n" +
                    "• Card communication error\n\n" +
                    "Check the output log for details.",
                    "Erase Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR: {ex.Message}");
            MessageBox.Show(
                $"An error occurred during card erasure:\n\n{ex.Message}\n\n" +
                "The card may be in an unknown state. Try again or use OpenSC tools directly.",
                "Erasure Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnErase.Enabled = true;
            AppendOutput("=== ERASURE OPERATION COMPLETE ===");
        }
    }
}
