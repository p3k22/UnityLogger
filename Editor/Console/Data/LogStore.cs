#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Data
   {
      using P3k.UnityRichConsoleLogger.Editor.Console.Utilities;

      using System;
      using System.Collections.Generic;
      using System.Linq;

      using UnityEngine;

      /// <summary>
      /// Stores parsed log rows and related counts for the console view.
      /// </summary>
      internal sealed class LogStore
      {
         private const int MAX_ROWS = 5000;

         private readonly List<Row> _rows = new List<Row>(1024);

         /// <summary>
         /// Gets a value indicating whether the store should be trimmed to the maximum size.
         /// </summary>
         public bool NeedsTrim => _rows.Count > MAX_ROWS;

         /// <summary>
         /// Gets the current log rows.
         /// </summary>
         public IReadOnlyList<Row> Rows => _rows;

         /// <summary>
         /// Gets the count of error entries.
         /// </summary>
         public int CountErr { get; private set; }

         /// <summary>
         /// Gets the count of normal log entries.
         /// </summary>
         public int CountLog { get; private set; }

         /// <summary>
         /// Gets the count of warning entries.
         /// </summary>
         public int CountWarn { get; private set; }

         /// <summary>
         /// Adds a log event to the store and updates counters.
         /// </summary>
         /// <param name="e">The log event to add.</param>
         public void Add(LogEvent e)
         {
            var color = Color.white;
            if (!string.IsNullOrWhiteSpace(e.ColorHex))
            {
               ColorUtility.TryParseHtmlString("#" + e.ColorHex.Trim().TrimStart('#'), out color);
            }

            var text = StackTraceParser.PatchClassNameIfUnknown(e);
            var isSuccess = e.Type == LogType.Log && e.ColorHex.Equals("50C878", StringComparison.OrdinalIgnoreCase);
            _rows.Add(new Row(text, e.Type, color, e.StackTrace, isSuccess));

            switch (e.Type)
            {
               case LogType.Warning:
                  CountWarn++;
                  break;
               case LogType.Error:
               case LogType.Assert:
               case LogType.Exception:
                  CountErr++;
                  break;
               default:
                  CountLog++;
                  break;
            }
         }

         /// <summary>
         /// Clears all stored rows and resets counters.
         /// </summary>
         public void Clear()
         {
            _rows.Clear();
            CountLog = CountWarn = CountErr = 0;
         }

         /// <summary>
         /// Trims the store to the maximum number of rows if needed.
         /// </summary>
         public void TrimIfNeeded()
         {
            if (_rows.Count > MAX_ROWS)
            {
               _rows.RemoveRange(0, _rows.Count - MAX_ROWS);
            }
         }
      }
   }
#endif
