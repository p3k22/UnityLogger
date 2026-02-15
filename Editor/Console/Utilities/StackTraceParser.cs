#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Utilities
   {
      using P3k.UnityRichConsoleLogger.Editor.Console.Data;

      using System;
      using System.Collections.Generic;
      using System.Linq;
      using System.Text.RegularExpressions;

      /// <summary>
      /// Parses stack traces into structured lines and extracts file references.
      /// </summary>
      internal static class StackTraceParser
      {
         private static readonly Regex RxCSharp = new(
         @"\bin\s+(.*?):line\s+(\d+)",
         RegexOptions.Compiled | RegexOptions.IgnoreCase);

         private static readonly Regex RxUnity = new(@"\(at\s+(.*?):(\d+)\)", RegexOptions.Compiled);

         /// <summary>
         /// Parses stack trace lines from a log row.
         /// </summary>
         /// <param name="row">The log row containing a stack trace.</param>
         /// <returns>The parsed stack trace lines.</returns>
         public static List<StackLine> ParseStackLines(Row row)
         {
            var result = new List<StackLine>(8);
            if (string.IsNullOrEmpty(row.Stack))
            {
               return result;
            }

            var lines = row.Stack.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in lines)
            {
               var trimmed = raw.Trim();
               if (string.IsNullOrEmpty(trimmed))
               {
                  continue;
               }

               if (TryExtractFileRef(trimmed, out var path, out var ln))
               {
                  result.Add(new StackLine(trimmed, path, ln));
               }
               else
               {
                  result.Add(new StackLine(trimmed));
               }
            }

            return result;
         }

         /// <summary>
         /// Replaces unknown class names in a log event when possible.
         /// </summary>
         /// <param name="e">The log event to patch.</param>
         /// <returns>The patched log text.</returns>
         public static string PatchClassNameIfUnknown(LogEvent e)
         {
            var txt = e.Text ?? string.Empty;

            if (!string.Equals(e.ClassName, "UnknownClass", StringComparison.Ordinal))
            {
               return txt;
            }

            var resolved = ResolveClassNameFromStack(e.StackTrace);
            if (string.IsNullOrEmpty(resolved) || resolved == "Unknown")
            {
               return txt;
            }

            const string Token = "[UnknownClass]";
            var idx = txt.IndexOf(Token, StringComparison.Ordinal);
            if (idx >= 0)
            {
               return txt.Remove(idx, Token.Length).Insert(idx, $"[{resolved}]");
            }

            return $"[{resolved}] {txt}";
         }

         /// <summary>
         /// Attempts to extract a file path and line number from a stack trace line.
         /// </summary>
         /// <param name="text">The stack trace line.</param>
         /// <param name="path">The extracted path.</param>
         /// <param name="line">The extracted line number.</param>
         /// <returns>True if a reference was found; otherwise, false.</returns>
         public static bool TryExtractFileRef(string text, out string path, out int line)
         {
            // Unity style:  (at Assets/Scripts/Foo.cs:42)
            var m = RxUnity.Match(text);
            if (m.Success && int.TryParse(m.Groups[2].Value, out line))
            {
               path = m.Groups[1].Value.Trim();
               return true;
            }

            // C# style:  in C:\...\Foo.cs:line 42
            m = RxCSharp.Match(text);
            if (m.Success && int.TryParse(m.Groups[2].Value, out line))
            {
               path = m.Groups[1].Value.Trim();
               return true;
            }

            path = null;
            line = 0;
            return false;
         }

         /// <summary>
         /// Resolves a class name from the first stack trace line.
         /// </summary>
         /// <param name="stack">The stack trace text.</param>
         /// <returns>The resolved class name, or "Unknown".</returns>
         private static string ResolveClassNameFromStack(string stack)
         {
            if (string.IsNullOrEmpty(stack))
            {
               return "Unknown";
            }

            var lines = stack.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
               return "Unknown";
            }

            var first = lines[0];
            if (first.StartsWith("at ", StringComparison.Ordinal))
            {
               first = first[3..];
            }

            var inIdx = first.IndexOf(" in ", StringComparison.Ordinal);
            if (inIdx > 0)
            {
               first = first[..inIdx];
            }

            var parenIdx = first.IndexOf('(');
            if (parenIdx > 0)
            {
               first = first[..parenIdx];
            }

            var lastDot = first.LastIndexOf('.');
            return lastDot > 0 ? first[..lastDot].Trim() : "Unknown";
         }
      }
   }
#endif
