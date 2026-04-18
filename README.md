# PunyPlayer

A Windows desktop tool that plays walkthrough transcripts into interactive
fiction interpreter windows (or any application), sending commands line by
line with configurable timing.

https://github.com/user-attachments/assets/c61243db-b08b-4f67-a84c-e81234af3b02

## Features

- Select up to **2 target windows** to receive commands simultaneously
- **9 keystroke delivery methods** selectable via a drop-down, covering
  background (no focus) and foreground (focus) approaches:

  | Method | Focus? | Description |
  |---|---|---|
  | PostMessage | No | Async WM_KEYDOWN + WM_CHAR + WM_KEYUP via PostMessage |
  | SendMessage | No | Sync WM_KEYDOWN + WM_CHAR + WM_KEYUP via SendMessage |
  | Post WM_CHAR only | No | Async WM_CHAR only via PostMessage |
  | Send WM_CHAR only | No | Sync WM_CHAR only via SendMessage |
  | SendKeys (focus) | Yes | System.Windows.Forms.SendKeys.SendWait — journal-hook based |
  | SendInput Unicode (focus) | Yes | SendInput with KEYEVENTF_UNICODE — hardware-level Unicode |
  | SendInput VK (focus) | Yes | SendInput with virtual-key codes + scan codes |
  | SendInput Scan (focus) | Yes | SendInput with KEYEVENTF_SCANCODE — raw hardware scan codes |
  | keybd_event (focus) | Yes | Legacy keybd_event API — sometimes bypasses UIPI |

- Dark theme by default, DPI-aware (WPF)
- Load walkthrough files with commands, comments, and special directives
- Configurable **line delay** between commands and **key delay** between
  individual keystrokes
- **Mouse-wheel scrolling** on the Delay (step 100), Key delay (step 5), and
  Line (step 1) fields for quick adjustment
- Line-by-line progress tracking with live preview
- Settings automatically saved and restored between sessions (including
  window position and selected method)
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
4. Set the **Delay** between commands (milliseconds) and **Key** delay
   between individual keystrokes
5. Optionally adjust the starting **Line** number
6. Choose a **send method** from the drop-down — try different methods
   if the target application does not respond to keystrokes
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
| `! Space` or `! S` | Press the Space key |
| `! Delay <ms>` | Pause for the given number of milliseconds |
| `! WIN="Agon": exec playgame.txt` | If window title contains "Agon", send the line "exec playgame.txt" |
| `! EXEC="C:\Emu\dfrotz.exe"` | Start dfrotz.exe |
| `! SWAP(y,z)` | Swap characters `y` ↔ `z` (case-insensitive) in all following lines |
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
  // Keystroke delivery method (e.g. PostMessage, SendKeys, InputVK)
  "sendMethod": "PostMessage"
}
```

## License

See [LICENSE](LICENSE).
