#if UNITY_EDITOR
   namespace P3k.UnityRichConsoleLogger.Editor.Console.Utilities
   {
      using P3k.UnityRichConsoleLogger.Editor.Console.Data;

      using System;
      using System.IO;
      using System.Linq;

      using UnityEditor;

      using UnityEditorInternal;

      using UnityEngine;

      using Object = UnityEngine.Object;

      /// <summary>
      /// Opens source files referenced by stack traces at specific lines.
      /// </summary>
      internal static class SourceNavigator
      {
         /// <summary>
         /// Opens a file path at a specific line in the editor.
         /// </summary>
         /// <param name="path">The file path to open.</param>
         /// <param name="line">The line number to navigate to.</param>
         public static void OpenPathAtLine(string path, int line)
         {
            if (string.IsNullOrEmpty(path) || line <= 0)
            {
               return;
            }

            var assetPath = path;

            // Convert absolute paths to relative Assets/ paths when possible
            if (Path.IsPathRooted(assetPath))
            {
               var dataPath = Application.dataPath.Replace('\\', '/');
               var norm = assetPath.Replace('\\', '/');
               if (norm.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
               {
                  assetPath = "Assets" + norm.Substring(dataPath.Length);
               }
            }

            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null)
            {
               AssetDatabase.OpenAsset(obj, line);
               return;
            }

            // Fallback: ask the OS / external editor
            try
            {
               InternalEditorUtility.OpenFileAtLineExternal(path, line);
            }
            catch
            {
               // Silently fail if the external editor cannot be launched.
            }
         }

         /// <summary>
         /// Attempts to open the first relevant stack trace row in the IDE.
         /// </summary>
         /// <param name="row">The row containing the stack trace.</param>
         public static void TryOpenRowInIde(Row row)
         {
            if (string.IsNullOrEmpty(row.Stack))
            {
               return;
            }

            var lines = row.Stack.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

            string fallbackPath = null;
            var fallbackLine = 0;

            foreach (var line in lines)
            {
               if (!StackTraceParser.TryExtractFileRef(line, out var path, out var ln))
               {
                  continue;
               }

               // Prefer user project code
               if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || path.StartsWith(
                   "Assets\\",
                   StringComparison.OrdinalIgnoreCase))
               {
                  OpenPathAtLine(path, ln);
                  return;
               }

               if (fallbackPath == null)
               {
                  fallbackPath = path;
                  fallbackLine = ln;
               }
            }

            if (fallbackPath != null)
            {
               OpenPathAtLine(fallbackPath, fallbackLine);
            }
         }
      }
   }
#endif