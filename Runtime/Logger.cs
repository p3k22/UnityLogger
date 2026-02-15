namespace P3k.UnityRichConsoleLogger
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using System.Linq;
   using System.Runtime.CompilerServices;

   using UnityEngine;

   /// <summary>
   ///    Rich console logger that formats messages with colour, timestamps, and
   ///    frame counts before forwarding them to Unity's console and raising
   ///    <see cref="GlobalMessageReceived" />.
   /// </summary>
   public sealed class Logger
   {
      /// <summary>
      ///    Per-thread re-entrancy counter used to detect recursive log calls
      /// </summary>
      [ThreadStatic]
      private static int _emitDepth;

      /// <summary>
      ///    Gets a value indicating whether the logger is currently emitting a
      ///    message on the calling thread. Used by bridges to avoid infinite recursion.
      /// </summary>
      public static bool IsEmitting => _emitDepth > 0;

      /// <summary>
      ///    Raised for every log message after it has been composed, regardless of
      ///    whether it is echoed to the Unity console.
      /// </summary>
      public static event Action<LogEvent> GlobalMessageReceived;

      /// <summary>
      ///    Emits a log message forwarded from a bridge without echoing it back to
      ///    the Unity console, preventing duplicate entries.
      /// </summary>
      /// <param name="text">The log message text.</param>
      /// <param name="type">The Unity log type of the message.</param>
      /// <param name="colorHex">Hex colour code used for display.</param>
      /// <param name="stackTrace">Optional stack trace associated with the message.</param>
      public static void EmitFromBridge(string text, LogType type, string colorHex = "FFFFFF", string stackTrace = null)
      {
         Emit(text, colorHex, ParseCallerFileFromStackTrace(stackTrace), type, false, stackTrace);
      }

      /// <summary>
      ///    Logs an informational message with the specified colour.
      /// </summary>
      /// <param name="text">The log message text.</param>
      /// <param name="colorHex">Hex colour code used for display.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      public void Log(string text, string colorHex = "FFFFFF", [CallerFilePath] string callerFilePath = null)
      {
         Emit(text, colorHex, callerFilePath, LogType.Log, true, CaptureUserStackFrame());
      }

      /// <summary>
      ///    Logs an error message in red-orange (<c>FF4500</c>).
      /// </summary>
      /// <param name="text">The error message text.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      public void LogError(string text, [CallerFilePath] string callerFilePath = null)
      {
         Emit(text, "FF4500", callerFilePath, LogType.Error, true, CaptureUserStackFrame());
      }

      /// <summary>
      ///    Logs a success message in emerald green (<c>50C878</c>).
      /// </summary>
      /// <param name="text">The success message text.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      public void LogSuccess(string text, [CallerFilePath] string callerFilePath = null)
      {
         Emit(text, "50C878", callerFilePath, LogType.Log, true, CaptureUserStackFrame());
      }

      /// <summary>
      ///    Logs a warning message in gold (<c>FFD700</c>).
      /// </summary>
      /// <param name="text">The warning message text.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      public void LogWarning(string text, [CallerFilePath] string callerFilePath = null)
      {
         Emit(text, "FFD700", callerFilePath, LogType.Warning, true, CaptureUserStackFrame());
      }

      /// <summary>
      ///    Emits a log message originating from a debug helper, echoing it to the
      ///    Unity console.
      /// </summary>
      /// <param name="text">The log message text.</param>
      /// <param name="colorHex">Hex colour code used for display.</param>
      /// <param name="callerFilePath">Source file path of the original caller.</param>
      /// <param name="type">The Unity log type of the message.</param>
      /// <param name="stackTrace">Stack trace associated with the message.</param>
      internal static void EmitFromDebug(
         string text,
         string colorHex,
         string callerFilePath,
         LogType type,
         string stackTrace)
      {
         Emit(text, colorHex, callerFilePath, type, true, stackTrace);
      }

      /// <summary>
      ///    Walks the call stack to find the first frame that belongs to user code
      ///    inside the project, preferring frames outside the logger namespace.
      /// </summary>
      /// <returns>A formatted string identifying the caller's method and source location.</returns>
      private static string CaptureUserStackFrame()
      {
         var st = new StackTrace(2, true);
         var frames = st.GetFrames();
         if (frames == null || frames.Length == 0)
         {
            return st.ToString();
         }

         var dataPath = Application.dataPath.Replace('\\', '/');
         string firstValidPath = null;
         var firstValidLine = 0;
         string firstValidMethod = null;

         foreach (var f in frames)
         {
            var file = f.GetFileName();
            var line = f.GetFileLineNumber();
            if (string.IsNullOrEmpty(file) || line <= 0)
            {
               continue;
            }

            var method = f.GetMethod();
            var type = method?.DeclaringType;
            var ns = type?.Namespace ?? string.Empty;
            var fullType = type != null ? type.FullName : "UnknownType";
            var methodName = method?.Name ?? "UnknownMethod";

            if (firstValidPath == null)
            {
               firstValidPath = file;
               firstValidLine = line;
               firstValidMethod = $"{fullType}.{methodName}";
            }

            // Prefer frames outside our logger namespace and inside the project
            var norm = file.Replace('\\', '/');
            var inProject = norm.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase);
            var isLoggerNs = ns.StartsWith("P3k.Unity", StringComparison.Ordinal)
                             && (ns.StartsWith("P3k.UnityLogger", StringComparison.Ordinal) || ns.StartsWith(
                                 "P3k.UnityRichConsoleLogger",
                                 StringComparison.Ordinal));

            if (inProject && !isLoggerNs)
            {
               return $"{fullType}.{methodName} in {file}:line {line}";
            }
         }

         if (firstValidPath != null && firstValidLine > 0)
         {
            return $"{firstValidMethod} in {firstValidPath}:line {firstValidLine}";
         }

         return st.ToString();
      }

      /// <summary>
      ///    Parses the first file path from a Unity-format stack trace string.
      ///    Unity stack frames look like <c>Class.Method () (at Assets/Scripts/File.cs:42)</c>.
      /// </summary>
      /// <param name="stackTrace">The Unity stack trace string.</param>
      /// <returns>The file path from the first frame, or <see langword="null" /> if not found.</returns>
      private static string ParseCallerFileFromStackTrace(string stackTrace)
      {
         if (string.IsNullOrEmpty(stackTrace))
         {
            return null;
         }

         var atIndex = stackTrace.IndexOf("(at ", StringComparison.Ordinal);
         if (atIndex < 0)
         {
            return null;
         }

         var pathStart = atIndex + 4;
         var colonIndex = stackTrace.LastIndexOf(':', stackTrace.IndexOf(')', pathStart) is var paren and >= 0 ? paren : stackTrace.Length - 1);
         if (colonIndex <= pathStart)
         {
            return null;
         }

         return stackTrace.Substring(pathStart, colonIndex - pathStart);
      }

      /// <summary>
      ///    Core emit routine that composes the final message, raises
      ///    <see cref="GlobalMessageReceived" />, and optionally echoes to the Unity
      ///    console while guarding against re-entrancy.
      /// </summary>
      /// <param name="text">The raw log message text.</param>
      /// <param name="colorHex">Hex colour code used for display.</param>
      /// <param name="callerFilePath">Source file path used to derive the class name prefix.</param>
      /// <param name="type">The Unity log type of the message.</param>
      /// <param name="echoToUnityConsole">Whether to forward the message to <see cref="UnityEngine.Debug" />.</param>
      /// <param name="stackTrace">Stack trace associated with the message.</param>
      private static void Emit(
         string text,
         string colorHex,
         string callerFilePath,
         LogType type,
         bool echoToUnityConsole,
         string stackTrace)
      {
         var className = callerFilePath != null ? Path.GetFileNameWithoutExtension(callerFilePath) : "UnknownClass";
         var time = DateTime.Now;
         var frameCount = Time.frameCount;

         var prefix = $"[{className}] [t: {time:HH:mm:ss}] [f: {frameCount}]: ";
         var composed = prefix + (text ?? string.Empty);

         var evt = new LogEvent(composed, colorHex, type, className, time, frameCount, stackTrace);

         GlobalMessageReceived?.Invoke(evt);

         if (!echoToUnityConsole)
         {
            return;
         }

         try
         {
            _emitDepth++;
            switch (type)
            {
               case LogType.Error:
               case LogType.Assert:
               case LogType.Exception:
                  UnityEngine.Debug.LogError(composed);
                  break;
               case LogType.Warning:
                  UnityEngine.Debug.LogWarning(composed);
                  break;
               default:
                  UnityEngine.Debug.Log(composed);
                  break;
            }
         }
         finally
         {
            _emitDepth--;
         }
      }
   }
}