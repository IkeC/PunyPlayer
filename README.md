# PunyPlayer

A Windows desktop tool that plays walkthrough transcripts into interactive
fiction interpreter windows (or any application), sending commands line by
line with configurable timing.

## Features

- Select up to **3 target windows** to receive commands simultaneously
- **Focus Mode** — activates the target window before each command and uses
  `SendKeys` with a configurable per-key delay, for apps that ignore
  background input (e.g. GUI interpreters)
- Dark theme by default, DPI-aware (WPF)
- Load walkthrough files with commands, comments, and special directives
- Configurable **line delay** between commands and **key delay** within a command
- **Mouse-wheel scrolling** on the Delay (step 100), Key delay (step 5), and
  Line (step 1) fields for quick adjustment
- Line-by-line progress tracking with live preview
- Settings automatically saved and restored between sessions (including
  window position)
- Single portable `.exe` — no installer needed
- Phantom and system windows are filtered out of the window list

## Installation

Download `PunyPlayer.exe` from the Release folder, or build from source:

```powershell
dotnet publish PunyPlayer -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o Release
```

## Usage

1. Start **PunyPlayer**
2. Click **Refresh** to list open windows, then select up to 3 target windows
3. Enter the path to a walkthrough file (or use **Browse...**)
4. Set the **Delay** between commands (milliseconds) and **Key** delay for
   Focus Mode
5. Optionally adjust the starting **Line** number
6. Toggle **Focus Mode** when the target window requires foreground input
7. Click **Run** to begin playback; click **Stop** to pause

## Walkthrough file format

```text
# This is a comment (ignored during playback)
north
! Enter
look
! E
! Delay 2000
take lamp
```

| Syntax | Meaning |
|---|---|
| `# ...` | Comment — skipped during playback |
| `! Enter` or `! E` | Press the Enter key |
| `! Space` | Press the Space key |
| `! Delay <ms>` | Pause for the given number of milliseconds |
| _(any other text)_ | Sent as keystrokes to the target window |

Empty and whitespace-only lines are skipped.  Bang commands (`!`) do not
need a space between `!` and the keyword (e.g. `!Enter` works).

## Building from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
# Run in debug mode
dotnet run --project PunyPlayer

# Run tests
dotnet test

# Build release executable
dotnet publish PunyPlayer -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o Release
```

## Settings

PunyPlayer saves its state to `PunyPlayer.jsonc` next to the executable.
The file is human-readable JSON with comments describing each field:

```jsonc
{
  // The title of the first selected target window
  "selectedWindow1": "dfrotz - dust.z5",
  // The title of the second selected target window (optional)
  "selectedWindow2": "",
  // The title of the third selected target window (optional)
  "selectedWindow3": "",
  // Path to the walkthrough/transcript file
  "filePath": "walkthrough.txt",
  // Delay in milliseconds between sending commands
  "delay": 1000,
  // Delay in milliseconds between individual keystrokes (Focus Mode)
  "keyDelay": 30,
  // The next line number to play (1-based)
  "currentLine": 1,
  // Whether Focus Mode is enabled
  "focusMode": true
}
```

## License

See [LICENSE](LICENSE).
