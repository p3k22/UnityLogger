<p align="center">
  <strong>P3k Rich Console Logger</strong><br/>
  <em>A drop-in replacement for Unity's Console window</em>
</p>

<p align="center">
  Coloured output · background tints · virtualised scrolling · collapsible entries<br/>
  resizable stack-trace panel with clickable links · full IDE integration
</p>

---

##  Features

| Feature | Details |
|---|---|
| **Coloured log text** | Every log type has a distinct colour. All methods accept an optional `colorHex` override for custom colours. |
| **Background tints** | Errors get a red tint, warnings a yellow tint, and success logs a green tint — instantly recognisable at a glance. |
| **Alternating row backgrounds** | Even / odd rows use slightly different shades for readability. |
| **Row hover highlight** | A subtle overlay appears when the cursor hovers over a row. |
| **Virtualised scrolling** | Only visible rows are drawn, keeping performance smooth with thousands of entries (up to 5 000 buffered). |
| **Collapse mode** | Identical messages are grouped with a count badge. Timestamps and frame numbers are stripped when comparing, so the same message logged at different times still collapses. |
| **Search / filter** | Free-text search (case-insensitive) and per-type toolbar toggles (Log / Warning / Error) with live counts. |
| **Multi-select & copy** | Shift-click for range select, Ctrl/Cmd-click to toggle, Ctrl/Cmd+C to copy. Copied text includes the full stack trace for each selected entry. |
| **Double-click to open** | Opens the originating source file and line in your IDE. Prefers user project files under `Assets/`, with a fallback to the external editor. |
| **Resizable detail panel** | Drag the splitter to resize. Stack-trace lines with file references are rendered as clickable hyperlinks with hover highlighting. |
| **Auto-scroll** | Automatically scrolls to the newest log. Re-enables on entering Play mode. Disables when you scroll manually. |
| **Auto-clear** | Logs clear on entering Play mode and after script reloads. Logs are preserved when exiting Play mode so you can inspect them after stopping. |
| **Clean call stacks** | All `Debug` methods are marked with `[HideInCallstack]` so they don't clutter Unity's built-in Console stack traces. |
| **Thread-safe re-entrancy guard** | A per-thread `IsEmitting` flag prevents infinite recursion when logs are echoed back through `UnityEngine.Debug`. |

---

##  Opening the Console

**Menu:**&ensp;`Window → General → P3k's Console`

**Shortcut:**&ensp;`Ctrl+Shift+D`&ensp;(Windows)&ensp;/&ensp;`Cmd+Shift+D`&ensp;(macOS)

---

## Logging

### Static `Debug` API

The simplest way to log. Add `using Debug = P3k.UnityRichConsoleLogger.Debug;` at the top of your file to shadow Unity's built-in `Debug`:

```csharp
using Debug = P3k.UnityRichConsoleLogger.Debug;

public class Example : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Standard log");                          // white text
        Debug.LogSuccess("Operation complete!");             // green text + green tint
        Debug.LogWarning("Watch out!");                      // gold text  + yellow tint
        Debug.LogError("Something broke!");                  // red text   + red tint
        Debug.LogAssertion("Assertion failed!");             // orange text + red tint
        Debug.LogException(new System.Exception("Boom"));

        // Every method accepts an optional colorHex override:
        Debug.Log("Custom cyan", colorHex: "00FFFF");
        Debug.LogWarning("Custom pink warning", colorHex: "FF69B4");
    }
}
```

### Instance `Logger`

Use this when you want instance-based logging or custom colours:

```csharp
using Logger = P3k.UnityRichConsoleLogger.Logger;

public class Example : MonoBehaviour
{
    private Logger _logger;

    void Start()
    {
        _logger = new Logger();

        _logger.Log("basic log");               // white text
        _logger.LogSuccess("all good!");         // green text + green tint
        _logger.LogWarning("careful!");          // gold text  + yellow tint
        _logger.LogError("failed!");             // red text   + red tint

        // Custom colour (cyan)
        _logger.Log("custom colour", "00FFFF");
    }
}
```

All logs are echoed to Unity's built-in Console **and** appear in the Rich Console with colours, prefixes, and tints.

### Unity's built-in `UnityEngine.Debug`

Native `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls are automatically captured and forwarded to the Rich Console:

- **Play mode** — the `UnityLogBridge` subscribes to `Application.logMessageReceived` at `[RuntimeInitializeOnLoadMethod]`.
- **Edit mode** — the `EditorLogBridge` subscribes via `[InitializeOnLoad]` and automatically unsubscribes during Play mode to avoid duplicates.

For native calls (which lack `[CallerFilePath]` info) the class name in the log prefix is resolved from the stack trace automatically.

---

## Log Colours & Tints

| Method | Text Colour | Row Tint |
|---|---|---|
| `Log` | `#FFFFFF` white | — |
| `LogSuccess` | `#50C878` emerald green | Green |
| `LogWarning` | `#FFD700` gold | Yellow |
| `LogError` | `#FF4500` orange-red | Red |
| `LogAssertion` | `#E67E22` orange | Red |
| `LogException` | `#FF4500` orange-red | Red |

> **Tip:** Pass a `colorHex` parameter to any method to override the default text colour — e.g. `Debug.Log("hi", colorHex: "00FFFF")`.

---

##  Log Format

Every message is automatically prefixed with context:

```
[ClassName] [t: HH:mm:ss] [f: <frame>]: <message>
```

- **ClassName** — derived from the calling file path via `[CallerFilePath]`.
- **t** — local wall-clock time (`DateTime.Now`).
- **f** — Unity's current frame count (`Time.frameCount`).

For Unity's native `Debug.Log` calls (which don't carry caller info) the class name is resolved from the first user-code frame in the stack trace.

---

## Auto-Scroll Behaviour

- **Enabled by default** and always re-enabled when entering Play mode.
- **Automatically disabled** when you scroll manually with the mouse wheel, so you can browse freely.
- While disabled the log buffer is **not trimmed**, allowing you to scroll back through all entries.
- Re-enable at any time via the **Auto ↓** toolbar toggle. Re-enabling also trims any excess rows that accumulated while scrolling was paused.

---

## Clear / Reset Behaviour

The console automatically clears when:

- **Entering Play mode** — starts fresh with auto-scroll on.
- **After script reload** — domain reload / recompilation.

It does **not** clear when exiting Play mode, so you can still inspect logs after stopping the game.

You can also clear manually at any time with the **Clear** toolbar button.

---

## In-Game Usage

Subscribe to `Logger.GlobalMessageReceived` to build an in-game log overlay or on-screen console:

```csharp
using P3k.UnityRichConsoleLogger;

void OnEnable()  => Logger.GlobalMessageReceived += OnLog;
void OnDisable() => Logger.GlobalMessageReceived -= OnLog;

private void OnLog(LogEvent e)
{
    // e.Text       — full prefixed message string
    // e.ColorHex   — hex colour code (e.g. "FF4500")
    // e.Type       — UnityEngine.LogType
    // e.ClassName  — resolved caller class name
    // e.TimeLocal  — DateTime of the log
    // e.Frame      — Time.frameCount at emission
    // e.StackTrace — full stack trace string
}
```

---

## Installation

### Option 1 — Git URL

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.p3k.unityrichconsolelogger": "https://github.com/p3k22/UnityRichConsoleLogger.git"
  }
}
```

### Option 2 — Manual

Copy the `Runtime` and `Editor` folders into your project. If you are not using assembly definitions, delete the included `.asmdef` files.

---

## License

Free to use and extend in your projects.
