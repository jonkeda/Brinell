    

# Setting Up VS Code Insiders with GitHub Copilot

This guide walks you through setting up Visual Studio Code Insiders with GitHub Copilot and essential extensions for the PVE workshop.

---

## 1. Create a GitHub Account

If you don't already have a GitHub account, you'll need to create one to use GitHub Copilot and sync your settings.

### Sign Up

1. Go to [GitHub.com](https://github.com)
2. Click **Sign up** in the top-right corner
3. Enter your email address and click **Continue**
4. Create a password (at least 15 characters, or 8 characters with a number and lowercase letter)
5. Choose a username (this will be visible to others)
6. Complete the CAPTCHA verification
7. Click **Create account**

### Verify Your Email

1. Check your email inbox for a message from GitHub
2. Click the verification link or enter the code provided
3. Complete any additional setup prompts

### Set Up Your Profile (Optional)

1. Add a profile picture
2. Set your display name
3. Add a bio if desired

---

## 2. Request a GitHub Copilot License

Before you can use GitHub Copilot, you need access to a license.

### For Individual Users

1. Go to [GitHub Copilot](https://github.com/features/copilot)
2. Click **Start free trial** or **Get Copilot**
3. Sign in with your GitHub account
4. Follow the subscription setup

### For Enterprise/Organization Users

1. Contact your IT department or GitHub administrator
2. Request to be added to your organization's GitHub Copilot Business license
3. Once approved, you'll receive an email confirmation
4. Accept the invitation in your GitHub settings

### Verify Your License

1. Go to [GitHub Settings → Copilot](https://github.com/settings/copilot)
2. Confirm your subscription status shows as **Active**

---

## 3. Install Visual Studio Code Insiders

VS Code Insiders gives you access to the latest features before they're released to the stable version.

### Download

1. Go to [VS Code Insiders Download Page](https://code.visualstudio.com/insiders/)
2. Download the version for your operating system:
   - **Windows**: `.exe` installer
   - **macOS**: `.dmg` file
   - **Linux**: `.deb` or `.rpm` package

### Install

- **Windows**: Run the installer, accept defaults
- **macOS**: Drag to Applications folder
- **Linux**: Use your package manager or double-click the downloaded file

### First Launch

1. Open VS Code Insiders
2. Sign in with your GitHub account (you'll need this for Copilot)
3. Accept any initial prompts for settings sync (optional)

---

## 4. Install Required Extensions

### 4.1 Markdown All in One

Essential for working with Markdown files efficiently.

**Install:**

1. Open Extensions view: `Ctrl+Shift+X`
2. Search for **"Markdown All in One"**
3. Click **Install** on the extension by Yu Zhang

**Features:**

- Keyboard shortcuts for formatting
- Table of contents generation
- Auto-preview
- List editing helpers

**Extension ID:** `yzhang.markdown-all-in-one`

---

### 4.2 Markdown Editor (zaaack)

A WYSIWYG Markdown editor that provides a more visual editing experience.

**Install:**

1. Open Extensions view: `Ctrl+Shift+X`
2. Search for **"Markdown Editor"** by zaaack
3. Click **Install**

**Features:**

- WYSIWYG editing mode
- Real-time preview
- Easy formatting toolbar
- Support for tables, images, and code blocks

**Usage:**

1. Right-click on any `.md` file
2. Select **"Open with Markdown Editor"**
3. Or use the command palette: `Ctrl+Shift+P` → "Markdown Editor: Open"

**Extension ID:** `zaaack.markdown-editor`

---

### 4.3 C# Dev Kit

Complete C# development environment for .NET projects.

**Install:**

1. Open Extensions view: `Ctrl+Shift+X`
2. Search for **"C# Dev Kit"**
3. Click **Install** on the extension by Microsoft

**Includes:**

- IntelliSense for C#
- Debugging support
- Project management
- Test explorer

**Prerequisites:**

- .NET SDK installed ([Download .NET](https://dotnet.microsoft.com/download))

**Extension ID:** `ms-dotnettools.csdevkit`

---

## 5. Quick Install via Command Line

You can install all extensions at once using the command line:

```powershell
# Install extensions for VS Code Insiders
code-insiders --install-extension yzhang.markdown-all-in-one
code-insiders --install-extension zaaack.markdown-editor
code-insiders --install-extension ms-dotnettools.csdevkit
```

---

## 6. Verify Your Setup

### Checklist

- [ ] GitHub account created and verified
- [ ] VS Code Insiders installed and running
- [ ] Signed in with GitHub account
- [ ] GitHub Copilot license active
- [ ] Copilot extension showing "Ready" (installed by default)
- [ ] Markdown All in One extension installed
- [ ] Markdown Editor (zaaack) extension installed
- [ ] C# Dev Kit extension installed

### Test Copilot

1. Create a new file: `test.cs`
2. Type: `// function to calculate fibonacci`
3. Press `Enter` and wait for Copilot suggestion
4. Press `Tab` to accept or `Esc` to dismiss

### Test Copilot Chat

1. Open Copilot Chat: `Ctrl+Shift+I` or click the chat icon
2. Type: "How would you approach building a simple calculator?"
3. Verify you get a response

---

## 7. Recommended Settings

Add these to your `settings.json` for a better experience:

```json
{
    "editor.inlineSuggest.enabled": true,
    "github.copilot.enable": {
        "*": true,
        "markdown": true,
        "plaintext": true
    },
    "markdown.extension.toc.updateOnSave": true,
    "editor.formatOnSave": true
}
```

**To open settings.json:**

1. Press `Ctrl+Shift+P`
2. Type "Preferences: Open User Settings (JSON)"
3. Add the settings above

---

## Troubleshooting

### Copilot not suggesting code

- Check if you're signed in to GitHub
- Verify your license is active
- Restart VS Code Insiders
- Check the Copilot status in the bottom bar

### Extensions not installing

- Check your internet connection
- Try installing from the VS Code Marketplace website
- Run VS Code Insiders as administrator (Windows)

### C# not working

- Ensure .NET SDK is installed: `dotnet --version`
- Restart VS Code after installing C# Dev Kit

---

## Need Help?

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [VS Code Documentation](https://code.visualstudio.com/docs)
- [C# Dev Kit Guide](https://code.visualstudio.com/docs/csharp/get-started)
