namespace P3k.UnityRichConsoleLogger
{
   using System;
   using System.Linq;
   using System.Runtime.CompilerServices;

   using UnityEngine;

   /// <summary>
   ///    Drop-in replacement for <see cref="UnityEngine.Debug" /> that routes all
   ///    messages through the rich console <see cref="Logger" />, automatically
   ///    capturing caller information for each log entry.
   /// </summary>
   public static class Debug
   {
      /// <summary>Hex colour used for assertion messages.</summary>
      private const string HEX_ASSERT = "E67E22";

      /// <summary>Hex colour used for error and exception messages.</summary>
      private const string HEX_ERROR = "FF4500";

      /// <summary>Hex colour used for standard log messages.</summary>
      private const string HEX_LOG = "FFFFFF";

      /// <summary>Hex colour used for success messages.</summary>
      private const string HEX_SUCCESS = "50C878";

      /// <summary>Hex colour used for warning messages.</summary>
      private const string HEX_WARNING = "FFD700";

      /// <summary>
      ///    Logs an informational message, defaulting to white.
      /// </summary>
      /// <param name="message">The message object to log.</param>
      /// <param name="colorHex">Optional hex colour override. Defaults to white (<c>FFFFFF</c>) when <see langword="null" />.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      /// <param name="callerLine">Automatically captured source line number of the caller.</param>
      [HideInCallstack]
      [MethodImpl(MethodImplOptions.NoInlining)]
      public static void Log(
         object message,
         string colorHex = null,
         [CallerFilePath] string callerFilePath = null,
         [CallerLineNumber] int callerLine = 0)
      {
         Logger.EmitFromDebug(
         ToStringSafe(message),
         colorHex ?? HEX_LOG,
         callerFilePath,
         LogType.Log,
         FormatCaller(callerFilePath, callerLine));
      }

      /// <summary>
      ///    Logs an assertion message, defaulting to orange (<c>E67E22</c>).
      /// </summary>
      /// <param name="message">The message object to log.</param>
      /// <param name="colorHex">Optional hex colour override. Defaults to orange (<c>E67E22</c>) when <see langword="null" />.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      /// <param name="callerLine">Automatically captured source line number of the caller.</param>
      [HideInCallstack]
      [MethodImpl(MethodImplOptions.NoInlining)]
      public static void LogAssertion(
         object message,
         string colorHex = null,
         [CallerFilePath] string callerFilePath = null,
         [CallerLineNumber] int callerLine = 0)
      {
         Logger.EmitFromDebug(
         ToStringSafe(message),
         colorHex ?? HEX_ASSERT,
         callerFilePath,
         LogType.Assert,
         FormatCaller(callerFilePath, callerLine));
      }

      /// <summary>
      ///    Logs an error message, defaulting to red-orange (<c>FF4500</c>).
      /// </summary>
      /// <param name="message">The message object to log.</param>
      /// <param name="colorHex">Optional hex colour override. Defaults to red-orange (<c>FF4500</c>) when <see langword="null" />.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      /// <param name="callerLine">Automatically captured source line number of the caller.</param>
      [HideInCallstack]
      [MethodImpl(MethodImplOptions.NoInlining)]
      public static void LogError(
         object message,
         string colorHex = null,
         [CallerFilePath] string callerFilePath = null,
         [CallerLineNumber] int callerLine = 0)
      {
         Logger.EmitFromDebug(
         ToStringSafe(message),
         colorHex ?? HEX_ERROR,
         callerFilePath,
         LogType.Error,
         FormatCaller(callerFilePath, callerLine));
      }

      /// <summary>
      ///    Logs an exception message, defaulting to red-orange (<c>FF4500</c>),
      ///    using the exception's full string representation as the stack trace when
      ///    available.
      /// </summary>
      /// <param name="exception">The exception to log.</param>
      /// <param name="colorHex">Optional hex colour override. Defaults to red-orange (<c>FF4500</c>) when <see langword="null" />.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      /// <param name="callerLine">Automatically captured source line number of the caller.</param>
      [HideInCallstack]
      [MethodImpl(MethodImplOptions.NoInlining)]
      public static void LogException(
         Exception exception,
         string colorHex = null,
         [CallerFilePath] string callerFilePath = null,
         [CallerLineNumber] int callerLine = 0)
      {
         var text = exception?.Message ?? "Exception";
         var stack = exception?.ToString() ?? FormatCaller(callerFilePath, callerLine);
         Logger.EmitFromDebug(text, colorHex ?? HEX_ERROR, callerFilePath, LogType.Exception, stack);
      }

      /// <summary>
      ///    Logs a success message, defaulting to emerald green (<c>50C878</c>).
      /// </summary>
      /// <param name="message">The message object to log.</param>
      /// <param name="colorHex">Optional hex colour override. Defaults to emerald green (<c>50C878</c>) when <see langword="null" />.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      /// <param name="callerLine">Automatically captured source line number of the caller.</param>
      [HideInCallstack]
      [MethodImpl(MethodImplOptions.NoInlining)]
      public static void LogSuccess(
         object message,
         string colorHex = null,
         [CallerFilePath] string callerFilePath = null,
         [CallerLineNumber] int callerLine = 0)
      {
         Logger.EmitFromDebug(
         ToStringSafe(message),
         colorHex ?? HEX_SUCCESS,
         callerFilePath,
         LogType.Log,
         FormatCaller(callerFilePath, callerLine));
      }

      /// <summary>
      ///    Logs a warning message, defaulting to gold (<c>FFD700</c>).
      /// </summary>
      /// <param name="message">The message object to log.</param>
      /// <param name="colorHex">Optional hex colour override. Defaults to gold (<c>FFD700</c>) when <see langword="null" />.</param>
      /// <param name="callerFilePath">Automatically captured source file path of the caller.</param>
      /// <param name="callerLine">Automatically captured source line number of the caller.</param>
      [HideInCallstack]
      [MethodImpl(MethodImplOptions.NoInlining)]
      public static void LogWarning(
         object message,
         string colorHex = null,
         [CallerFilePath] string callerFilePath = null,
         [CallerLineNumber] int callerLine = 0)
      {
         Logger.EmitFromDebug(
         ToStringSafe(message),
         colorHex ?? HEX_WARNING,
         callerFilePath,
         LogType.Warning,
         FormatCaller(callerFilePath, callerLine));
      }

      /// <summary>
      ///    Formats the caller's file path and line number into a stack-trace-style string.
      /// </summary>
      /// <param name="filePath">The source file path of the caller.</param>
      /// <param name="line">The source line number of the caller.</param>
      /// <returns>
      ///    A formatted string such as <c>in File.cs:line 42</c>, or <see cref="string.Empty" /> if the path is null or
      ///    empty.
      /// </returns>
      private static string FormatCaller(string filePath, int line)
      {
         if (string.IsNullOrEmpty(filePath))
         {
            return string.Empty;
         }

         return $"in {filePath}:line {line}";
      }

      /// <summary>
      ///    Returns the string representation of <paramref name="obj" />, or the
      ///    literal <c>"null"</c> if the reference is <see langword="null" />.
      /// </summary>
      /// <param name="obj">The object to convert.</param>
      /// <returns>The string representation, or <c>"null"</c>.</returns>
      private static string ToStringSafe(object obj)
      {
         return obj == null ? "null" : obj.ToString();
      }
   }
}
