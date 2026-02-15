namespace P3k.UnityRichConsoleLogger
{
   using System;

   using UnityEngine;

   public sealed class LogEvent
   {
      public LogEvent(
         string text,
         string colorHex,
         LogType type,
         string className,
         DateTime timeLocal,
         int frame,
         string stackTrace)
      {
         Text = text ?? string.Empty;
         ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "FFFFFF" : colorHex.Trim().TrimStart('#');
         Type = type;
         ClassName = string.IsNullOrEmpty(className) ? "UnknownClass" : className;
         TimeLocal = timeLocal;
         Frame = frame;
         StackTrace = stackTrace ?? string.Empty;
      }

      public string ClassName { get; }

      public string ColorHex { get; }

      public int Frame { get; }

      public string StackTrace { get; }

      public string Text { get; }

      public DateTime TimeLocal { get; }

      public LogType Type { get; }
   }
}
