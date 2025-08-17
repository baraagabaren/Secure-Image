using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Secure_Image
{
    public partial class Form1 : Form
    {
        // UI
        TextBox txtImagePath, txtPassword, txtMessage;
        Button btnBrowse, btnEmbed, btnExtract, btnSave;
        PictureBox preview;
        Label lblCap;

        Bitmap loadedBitmap;   // original
        Bitmap stegoBitmap;    // modified
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sneders, EventArgs s)
        {
            Text = "WinStego - AES-GCM Text-in-PNG";
            MinimumSize = new Size(860, 600);
            InitUI();
        }

        void InitUI()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 32, 12, 12) };
            Controls.Add(pnl);

            txtImagePath = new TextBox { PlaceholderText = "Choose a PNG…", ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            txtImagePath.Width = pnl.Width - 120;  // initial width
            btnBrowse = new Button { Text = "Browse…" };
            btnBrowse.Click += BrowseImage;

            var fileRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false
            };
            fileRow.Controls.AddRange(new Control[] { txtImagePath, btnBrowse });
            pnl.Controls.Add(fileRow);

            // Password row
            txtPassword = new TextBox
            {
                PlaceholderText = "Password (for AES-GCM)",
                UseSystemPasswordChar = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = pnl.Width - 40
            };
            var passRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
            passRow.Controls.Add(txtPassword);
            pnl.Controls.Add(passRow);

            // Message box
            txtMessage = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Width = pnl.Width - 40,
                Height = 200
            };
            var msgRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
            msgRow.Controls.Add(txtMessage);
            pnl.Controls.Add(msgRow);

            // Buttons row
            btnEmbed = new Button { Text = "Embed (Encrypt + Hide)", AutoSize = true };
            btnExtract = new Button { Text = "Extract (Reveal + Decrypt)", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
            btnSave = new Button { Text = "Save Stego PNG…", AutoSize = true, Margin = new Padding(8, 0, 0, 0), Enabled = false };

            btnEmbed.Click += DoEmbed;
            btnExtract.Click += DoExtract;
            btnSave.Click += DoSave;

            var btnRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0)
            };
            btnRow.Controls.AddRange(new Control[] { btnEmbed, btnExtract, btnSave });
            pnl.Controls.Add(btnRow);

            // Capacity label
            lblCap = new Label { Text = "Capacity: –", AutoSize = true, Margin = new Padding(0, 6, 0, 8) };
            pnl.Controls.Add(lblCap);

            // Preview
            preview = new PictureBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill   // << this makes it expand with form
            };
            pnl.Controls.Add(preview);
        }


        void BrowseImage(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "PNG Images|*.png",
                Title = "Open PNG"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtImagePath.Text = ofd.FileName;
                loadedBitmap?.Dispose();
                stegoBitmap?.Dispose();
                btnSave.Enabled = false;

                loadedBitmap = new Bitmap(ofd.FileName);
                preview.Image = loadedBitmap;

                var capBytes = Stego.CapacityBytes(loadedBitmap);
                lblCap.Text = $"Capacity (approx): {capBytes:N0} bytes (using 3 LSBs per pixel)";
            }
        }

        void DoEmbed(object? sender, EventArgs e)
        {
            if (loadedBitmap == null) { MessageBox.Show("Load a PNG first."); return; }
            if (string.IsNullOrEmpty(txtPassword.Text)) { MessageBox.Show("Enter a password."); return; }
            if (string.IsNullOrEmpty(txtMessage.Text)) { MessageBox.Show("Enter a message to hide."); return; }

            try
            {
                // 1) Encrypt message
                var plaintext = Encoding.UTF8.GetBytes(txtMessage.Text);
                var payload = Crypto.MakeEncryptedPayload(plaintext, txtPassword.Text);

                // 2) Check capacity
                var capacity = Stego.CapacityBytes(loadedBitmap);
                if (payload.Length + 4 > capacity) // +4 for the total length prefix
                {
                    MessageBox.Show($"Message too large for this image.\n" +
                                    $"Needs ~{payload.Length + 4:N0} bytes, image can hold ~{capacity:N0} bytes.");
                    return;
                }

                // 3) Embed (prefix total length (UInt32, big-endian) + payload)
                var beLen = BitConverter.GetBytes((UInt32)payload.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(beLen);
                var final = new byte[4 + payload.Length];
                Buffer.BlockCopy(beLen, 0, final, 0, 4);
                Buffer.BlockCopy(payload, 0, final, 4, payload.Length);

                stegoBitmap?.Dispose();
                stegoBitmap = Stego.EmbedBytes(loadedBitmap, final);
                preview.Image = stegoBitmap;
                btnSave.Enabled = true;

                MessageBox.Show("Embedded successfully. Click 'Save Stego PNG…' to write the file.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Embed failed:\n" + ex.Message);
            }
        }

        void DoExtract(object? sender, EventArgs e)
        {
            if (preview.Image == null)
            {
                MessageBox.Show("Load or embed an image first.");
                return;
            }
            if (string.IsNullOrEmpty(txtPassword.Text)) { MessageBox.Show("Enter the password used to embed."); return; }

            try
            {
                var bmp = (Bitmap)preview.Image;
                // Read first 4 bytes (big-endian length)
                var header = Stego.ExtractBytes(bmp, 4);
                var beLen = (byte[])header.Clone();
                if (BitConverter.IsLittleEndian) Array.Reverse(beLen);
                var totalLen = BitConverter.ToUInt32(beLen, 0);

                var payload = Stego.ExtractBytes(bmp, 4 + (int)totalLen);
                var onlyPayload = new byte[totalLen];
                Buffer.BlockCopy(payload, 4, onlyPayload, 0, (int)totalLen);

                var plaintext = Crypto.DecryptPayload(onlyPayload, txtPassword.Text);
                txtMessage.Text = Encoding.UTF8.GetString(plaintext);

                MessageBox.Show("Extracted and decrypted successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Extract failed:\n" + ex.Message);
            }
        }

        void DoSave(object? sender, EventArgs e)
        {
            if (stegoBitmap == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "PNG Images|*.png",
                Title = "Save Stego PNG",
                FileName = Path.GetFileNameWithoutExtension(txtImagePath.Text) + "_stego.png"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                stegoBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show("Saved.");
            }
        }
    }
}  
