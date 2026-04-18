# AGENTS.md — PunyPlayer workspace

> **Self-maintenance rule:** Whenever a structural change is made to this
> repository — new files, changed build or test tooling, new agent files,
> or altered conventions — this file **must** be updated in the same commit
> to stay accurate.

---

## Repository overview

PunyPlayer is a Windows desktop application that sends walkthrough commands
to interactive fiction interpreters (or any window) line by line, with
configurable delay between commands. Up to 3 target windows can receive
commands simultaneously. A drop-down offers 9 different keystroke delivery
methods (4 background, 5 foreground/focus) so users can pick whichever
works best with their target application.

### Project structure

| Path | Description |
|---|---|
| `PunyPlayer/` | Main WPF application (C# / .NET 8, dark theme) |
| `PunyPlayer.Tests/` | xUnit test project |
| `Release/` | Published single-file executable (gitignored) |
| `.vscode/tasks.json` | VS Code build tasks (Debug, Release) |

### Source files

| File | Role |
|---|---|
| `PunyPlayer/Program.cs` | Application entry point |
| `PunyPlayer/App.xaml` / `App.xaml.cs` | WPF application + dark theme resources |
| `PunyPlayer/MainWindow.xaml` / `.cs` | Main window — UI layout, playback logic, mouse-wheel handlers for Delay/Key/Line fields |
| `PunyPlayer/TranscriptReader.cs` | Reads and parses walkthrough transcript files |
| `PunyPlayer/SendMethod.cs` | `SendMethod` enum (9 methods) and `SendMethodExtensions` — display names, focus flag |
| `PunyPlayer/TextSender.cs` | Sends keystrokes via WriteConsoleInput for console targets; dispatches to one of 9 methods for GUI targets |
| `PunyPlayer/NativeMethods.cs` | Win32 P/Invoke declarations — `EnumWindows`, `GetWindow(GW_OWNER)`, `AttachThreadInput`, `SendInput`, `keybd_event`, `VkKeyScanW`, etc. Filters phantom windows and excludes PunyPlayer itself |
| `PunyPlayer/Settings.cs` | Save/load settings to `PunyPlayer.jsonc` — includes delay, keyDelay, sendMethod, window position |

### Test files

| File | Scope |
|---|---|
| `PunyPlayer.Tests/TranscriptReaderTests.cs` | Transcript parsing, line types, clamping |
| `PunyPlayer.Tests/SettingsTests.cs` | Settings defaults, save/load round-trip, JSONC |
| `PunyPlayer.Tests/TextSenderTests.cs` | Text sender helper routines |
| `PunyPlayer.Tests/SendMethodTests.cs` | Enum coverage, display names, focus flags |
| `PunyPlayer.Tests/NativeMethodsTests.cs` | Window enumeration, title exclusion filters |

---

## Building

```powershell
# Debug task: build, copy Release/example.txt, then launch PunyPlayer.exe
# (the VS Code task handles this automatically)

# Release — single-file .exe in Release/
dotnet publish PunyPlayer -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o Release
```

VS Code tasks are defined in `.vscode/tasks.json`:
- **Debug** — builds and runs the app
- **Release** — publishes a self-contained single-file exe to `Release/`

The Debug task copies `Release/example.txt` into the Debug output folder before launching `PunyPlayer.exe`, so the app can load its default walkthrough file when started from the task.

## Testing

```powershell
dotnet test
```

---

## Walkthrough file format

- Plain text lines are sent as keystrokes to the target window
- Lines starting with `#` are comments (skipped during playback)
- `! E` / `! Enter` / `!Enter` / `!E` — sends an Enter keypress
- `! Space` / `!Space` — sends a Space keypress
- `! Delay <ms>` — pauses for the specified number of milliseconds
- Empty/whitespace-only lines are skipped
- Bang commands (`!`) do not need a space between `!` and the keyword

## Settings persistence

Settings are saved to `PunyPlayer.jsonc` (next to the executable) on exit
and restored on startup. The file uses JSON with comments describing each
field. Persisted fields include: selected windows (1–3), file path, delay,
key delay, current line, focus mode, and window position/size.

---

## Conventions

- **.NET 8** with WPF (dark theme by default), targeting `win-x64`
- **Single-file publish** for distribution
- **xUnit** for unit tests
- Test files follow `*Tests.cs` naming
- Source uses nullable reference types and implicit usings
