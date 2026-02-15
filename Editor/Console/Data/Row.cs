#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Data
   {
      using System.Linq;
      using System.Text.RegularExpressions;

      using UnityEngine;

      /// <summary>
      /// Represents a parsed console log row with display metadata.
      /// </summary>
      internal sealed class Row
      {
         private static readonly Regex RxCollapseStrip = new Regex(
         @"\[t:\s*\d{2}:\d{2}:\d{2}\]\s*\[f:\s*\d+\]",
         RegexOptions.Compiled);

         public readonly bool IsSuccess;

         public readonly Color Color;

         public readonly LogType Type;

         public readonly string CollapseKey;

         public readonly string Stack;

         public readonly string Text;

         public Row(string text, LogType type, Color color, string stack, bool isSuccess = false)
         {
            Text = text ?? string.Empty;
            Type = type;
            Color = color;
            Stack = stack ?? string.Empty;
            IsSuccess = isSuccess;
            CollapseKey = RxCollapseStrip.Replace(Text, string.Empty).Trim();
         }
      }
   }
#endif
