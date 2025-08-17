🔒 Secure Image Steganography Tool (WinForms + AES-GCM)
📖 Overview

This project is a Windows Forms application that lets you hide secret messages inside PNG images using steganography (Least Significant Bit method) and encrypt them with AES-256-GCM for strong security.
The result is a normal-looking PNG file that secretly contains encrypted text — only someone with the right password can extract and decrypt it.

✨ Features
🖼 Embed text into PNG images using LSB steganography
🔐 AES-256-GCM encryption with PBKDF2 key derivation (200,000 iterations, SHA-256)
🔑 Password-protected message extraction
📊 Capacity calculation (how many bytes the chosen image can hide)
💾 Save the stego image as a new PNG file
👩‍💻 Simple WinForms GUI — no command-line needed

🚀 How to Use
1. Embedding a message
Click Browse… to select a PNG image.
Enter a password (used for AES encryption).
Type your secret message into the text box.
Click Embed (Encrypt + Hide).
Save the new PNG with Save Stego PNG….

2. Extracting a message
Open the stego PNG with Browse….
Enter the same password you used to embed.
Click Extract (Reveal + Decrypt).
The hidden message appears in the text box.

🛠 Technical Details
Programming Language: C#
Framework: .NET 6+ (Windows Forms)
Encryption: AES-256-GCM
Key Derivation: PBKDF2 with SHA-256, 200k iterations, 16-byte salt
Steganography: LSB (Least Significant Bit) in R, G, and B channels (3 bits per pixel)
File Format: PNG only (lossless — JPEG will destroy hidden data)

📂 Project Structure
Secure_Image/
├── Form1.cs        # Main WinForms code (UI + logic)
├── Stego.cs        # Steganography (LSB embed/extract)
├── Crypto.cs       # AES-GCM + PBKDF2 encryption/decryption
└── Program.cs      # Application entry point

⚠️ Notes
Only PNG images should be used (lossless format).
If you use the wrong password, extraction will fail.
Maximum message size depends on image resolution:
capacity ≈ (width × height × 3) / 8 bytes

Example: 1024×768 image ≈ 288 KB max hidden data.
📸 Example
Input: cat.png
Hidden message: "Hello, this is a secret!"
Output: cat_stego.png → looks identical, but contains the secret text.
