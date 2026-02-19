#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Data
   {
      using System.Linq;

      /// <summary>
      /// Represents a parsed line from a stack trace.
      /// </summary>
      internal readonly struct StackLine
      {
         public readonly bool HasFile;

         public readonly int Line;

         public readonly string Display;

         public readonly string File;

         public StackLine(string display, string file = null, int line = 0)
         {
            Display = display ?? string.Empty;
            File = file;
            Line = line;
            HasFile = !string.IsNullOrEmpty(file) && line > 0;
         }
      }
   }
#endif
