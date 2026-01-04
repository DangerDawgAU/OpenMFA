using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenMFA.SmartCard.OpenSC;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.GUI
{
    public partial class MainForm : Form
    {
        private OpenScContext? _context;
        private uint? _currentSlotId;

        public MainForm()
        {
            InitializeComponent();
            InitializeComboBoxes();
        }

        private void InitializeComboBoxes()
        {
            // Initialize slot combo boxes
            var slots = new[]
            {
                new { Display = "9A - PIV Authentication", Value = PivSlot.Authentication },
                new { Display = "9C - Digital Signature", Value = PivSlot.Signature },
                new { Display = "9D - Key Management", Value = PivSlot.KeyManagement },
                new { Display = "9E - Card Authentication", Value = PivSlot.CardAuthentication }
            };

            comboBoxSlot.DisplayMember = "Display";
            comboBoxSlot.ValueMember = "Value";
            comboBoxSlot.DataSource = slots.ToList();

            comboBoxKeySlot.DisplayMember = "Display";
            comboBoxKeySlot.ValueMember = "Value";
            comboBoxKeySlot.DataSource = slots.Select(s => new { s.Display, s.Value }).ToList();

            // Initialize algorithm combo box
            var algorithms = new[]
            {
                new { Display = "RSA 2048", Value = PivAlgorithm.Rsa2048 },
                new { Display = "RSA 1024", Value = PivAlgorithm.Rsa1024 },
                new { Display = "ECC P-256", Value = PivAlgorithm.EccP256 },
                new { Display = "ECC P-384", Value = PivAlgorithm.EccP384 }
            };

            comboBoxAlgorithm.DisplayMember = "Display";
            comboBoxAlgorithm.ValueMember = "Value";
            comboBoxAlgorithm.DataSource = algorithms.ToList();
        }

        private void AppendOutput(string message)
        {
            if (textBoxOutput.InvokeRequired)
            {
                textBoxOutput.Invoke(new Action<string>(AppendOutput), message);
            }
            else
            {
                textBoxOutput.AppendText(message + Environment.NewLine);
                textBoxOutput.SelectionStart = textBoxOutput.Text.Length;
                textBoxOutput.ScrollToCaret();
            }
        }

        private void ClearOutput()
        {
            textBoxOutput.Clear();
        }

        private async void BtnDetect_Click(object sender, EventArgs e)
        {
            ClearOutput();
            AppendOutput("Detecting card readers and smart cards...");

            try
            {
                _context = new OpenScContext();

                // List readers
                AppendOutput("\n--- Card Readers ---");
                var readers = await _context.ListReadersAsync();
                if (readers.Any())
                {
                    foreach (var reader in readers)
                    {
                        AppendOutput($"Reader: {reader}");
                    }
                }
                else
                {
                    AppendOutput("No card readers found.");
                }

                // List slots
                AppendOutput("\n--- Available Slots ---");
                var slots = await _context.GetSlotsAsync();
                if (slots.Any())
                {
                    foreach (var slot in slots)
                    {
                        AppendOutput($"Slot {slot.SlotId}: {slot.TokenLabel}");
                        if (slot.TokenPresent)
                        {
                            AppendOutput($"  Token present: Yes");
                            _currentSlotId = slot.SlotId;
                        }
                    }
                }
                else
                {
                    AppendOutput("No slots found.");
                }

                AppendOutput("\nDetection completed.");
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }

        private async void BtnInfo_Click(object sender, EventArgs e)
        {
            ClearOutput();
            AppendOutput("Retrieving card information...");

            try
            {
                if (_context == null)
                {
                    _context = new OpenScContext();
                }

                var slot = await _context.GetFirstSlotWithTokenAsync();
                if (slot == null)
                {
                    AppendOutput("\nNo card found. Please insert a PIV card and click 'Detect Card' first.");
                    return;
                }

                _currentSlotId = slot.SlotId;
                AppendOutput($"\n--- Card Information ---");
                AppendOutput($"Slot ID: {slot.SlotId}");
                AppendOutput($"Token Label: {slot.TokenLabel}");
                AppendOutput($"Token Present: {slot.TokenPresent}");
                AppendOutput("\nCard info retrieved successfully.");
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }

        private async void BtnList_Click(object sender, EventArgs e)
        {
            ClearOutput();
            AppendOutput("Listing certificates on card...");

            try
            {
                if (!_currentSlotId.HasValue)
                {
                    AppendOutput("\nNo card detected. Please click 'Detect Card' first.");
                    return;
                }

                using var pivCard = new PivCardOpenSc(_currentSlotId.Value);

                AppendOutput("\n--- Certificates on Card ---");
                var slots = new[]
                {
                    PivSlot.Authentication,
                    PivSlot.Signature,
                    PivSlot.KeyManagement,
                    PivSlot.CardAuthentication
                };

                foreach (var slot in slots)
                {
                    try
                    {
                        var certData = await pivCard.GetCertificateAsync(slot);
                        if (certData != null && certData.Length > 0)
                        {
                            AppendOutput($"Slot {slot:X}: Certificate found ({certData.Length} bytes)");
                        }
                    }
                    catch
                    {
                        AppendOutput($"Slot {slot:X}: No certificate");
                    }
                }

                AppendOutput("\nListing completed.");
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }

        private async void BtnRead_Click(object sender, EventArgs e)
        {
            if (comboBoxSlot.SelectedValue == null)
            {
                MessageBox.Show("Please select a slot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var slot = (PivSlot)comboBoxSlot.SelectedValue;

            ClearOutput();
            AppendOutput($"Reading certificate from slot {slot:X}...");

            try
            {
                if (!_currentSlotId.HasValue)
                {
                    AppendOutput("\nNo card detected. Please click 'Detect Card' first.");
                    return;
                }

                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Certificate files (*.crt;*.cer;*.der)|*.crt;*.cer;*.der|All files (*.*)|*.*",
                    DefaultExt = "crt",
                    FileName = $"cert_{slot:X}.crt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using var pivCard = new PivCardOpenSc(_currentSlotId.Value);
                    var certData = await pivCard.GetCertificateAsync(slot);

                    if (certData == null || certData.Length == 0)
                    {
                        AppendOutput("\nNo certificate found in this slot.");
                        return;
                    }

                    await File.WriteAllBytesAsync(saveDialog.FileName, certData);
                    AppendOutput($"\nCertificate saved to: {saveDialog.FileName}");
                    AppendOutput($"Size: {certData.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }

        private async void BtnWrite_Click(object sender, EventArgs e)
        {
            if (comboBoxSlot.SelectedValue == null)
            {
                MessageBox.Show("Please select a slot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var slot = (PivSlot)comboBoxSlot.SelectedValue;

            using var openDialog = new OpenFileDialog
            {
                Filter = "Certificate files (*.crt;*.cer;*.der)|*.crt;*.cer;*.der|All files (*.*)|*.*",
                Title = "Select Certificate File"
            };

            if (openDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            ClearOutput();
            AppendOutput($"Writing certificate to slot {slot:X}...");

            try
            {
                if (!_currentSlotId.HasValue)
                {
                    AppendOutput("\nNo card detected. Please click 'Detect Card' first.");
                    return;
                }

                var certData = await File.ReadAllBytesAsync(openDialog.FileName);
                AppendOutput($"Certificate file size: {certData.Length} bytes");

                using var pivCard = new PivCardOpenSc(_currentSlotId.Value);
                await pivCard.PutCertificateAsync(slot, certData);

                AppendOutput($"\nCertificate written successfully to slot {slot:X}.");
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (comboBoxSlot.SelectedValue == null)
            {
                MessageBox.Show("Please select a slot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var slot = (PivSlot)comboBoxSlot.SelectedValue;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the certificate in slot {slot:X}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            ClearOutput();
            AppendOutput($"Deleting certificate from slot {slot:X}...");

            try
            {
                if (!_currentSlotId.HasValue)
                {
                    AppendOutput("\nNo card detected. Please click 'Detect Card' first.");
                    return;
                }

                using var pivCard = new PivCardOpenSc(_currentSlotId.Value);
                await pivCard.DeleteCertificateAsync(slot);

                AppendOutput($"\nCertificate deleted successfully from slot {slot:X}.");
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }

        private async void BtnGenerateKey_Click(object sender, EventArgs e)
        {
            if (comboBoxKeySlot.SelectedValue == null || comboBoxAlgorithm.SelectedValue == null)
            {
                MessageBox.Show("Please select both slot and algorithm.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var slot = (PivSlot)comboBoxKeySlot.SelectedValue;
            var algorithm = (PivAlgorithm)comboBoxAlgorithm.SelectedValue;

            var result = MessageBox.Show(
                $"Generate a {algorithm} key pair in slot {slot:X}?\n\nThis operation may take several seconds.",
                "Confirm Key Generation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            ClearOutput();
            AppendOutput($"Generating {algorithm} key pair in slot {slot:X}...");
            AppendOutput("This may take a few moments, please wait...");

            try
            {
                if (!_currentSlotId.HasValue)
                {
                    AppendOutput("\nNo card detected. Please click 'Detect Card' first.");
                    return;
                }

                using var pivCard = new PivCardOpenSc(_currentSlotId.Value);
                var publicKey = await pivCard.GenerateKeyPairAsync(slot, algorithm);

                if (publicKey != null && publicKey.Length > 0)
                {
                    AppendOutput($"\nKey pair generated successfully!");
                    AppendOutput($"Public key size: {publicKey.Length} bytes");
                    AppendOutput($"Slot: {slot:X}");
                    AppendOutput($"Algorithm: {algorithm}");
                }
                else
                {
                    AppendOutput("\nKey generation completed but no public key data returned.");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}");
            }
        }
    }
}
