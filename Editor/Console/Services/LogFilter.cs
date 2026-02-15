#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Services
   {
      using P3k.UnityRichConsoleLogger.Editor.Console.Data;

      using System;
      using System.Collections.Generic;
      using System.Linq;

      using UnityEngine;

      /// <summary>
      /// Filters log rows based on type, search text, and collapse settings.
      /// </summary>
      internal sealed class LogFilter
      {
         private readonly Dictionary<string, int> _collapseCount = new(256);

         private readonly List<int> _visible = new(1024);

         /// <summary>
         /// Gets the indexes of visible rows after filtering.
         /// </summary>
         public IReadOnlyList<int> Visible => _visible;

         /// <summary>
         /// Gets or sets a value indicating whether duplicate rows are collapsed.
         /// </summary>
         public bool Collapse { get; set; }

         /// <summary>
         /// Gets a value indicating whether the filter needs rebuilding.
         /// </summary>
         public bool IsDirty { get; private set; } = true;

         /// <summary>
         /// Gets a value indicating whether error rows are visible.
         /// </summary>
         public bool ShowErr { get; set; } = true;

         /// <summary>
         /// Gets a value indicating whether log rows are visible.
         /// </summary>
         public bool ShowLog { get; set; } = true;

         /// <summary>
         /// Gets a value indicating whether warning rows are visible.
         /// </summary>
         public bool ShowWarn { get; set; } = true;

         /// <summary>
         /// Gets the current search text applied to rows.
         /// </summary>
         public string SearchText { get; set; } = string.Empty;

         /// <summary>
         /// Clears the current filter state.
         /// </summary>
         public void Clear()
         {
            _visible.Clear();
            _collapseCount.Clear();
            IsDirty = true;
         }

         /// <summary>
         /// Marks the filter as needing a rebuild.
         /// </summary>
         public void MarkDirty()
         {
            IsDirty = true;
         }

         /// <summary>
         /// Rebuilds the visible row list based on the current settings.
         /// </summary>
         /// <param name="rows">The rows to filter.</param>
         public void Rebuild(IReadOnlyList<Row> rows)
         {
            _visible.Clear();
            _collapseCount.Clear();

            for (var i = 0; i < rows.Count; i++)
            {
               var row = rows[i];

               if (!IsTypeVisible(row.Type))
               {
                  continue;
               }

               if (!string.IsNullOrEmpty(SearchText)
                   && row.Text.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) < 0)
               {
                  continue;
               }

               if (Collapse)
               {
                  if (_collapseCount.ContainsKey(row.CollapseKey))
                  {
                     _collapseCount[row.CollapseKey]++;
                     continue;
                  }

                  _collapseCount[row.CollapseKey] = 1;
               }

               _visible.Add(i);
            }

            IsDirty = false;
         }

         /// <summary>
         /// Tries to get the collapse count for a given key.
         /// </summary>
         /// <param name="key">The collapse key.</param>
         /// <param name="count">The occurrence count.</param>
         /// <returns>True if the key exists; otherwise, false.</returns>
         public bool TryGetCollapseCount(string key, out int count)
         {
            return _collapseCount.TryGetValue(key, out count);
         }

         /// <summary>
         /// Determines whether a log type is visible with current settings.
         /// </summary>
         /// <param name="type">The log type.</param>
         /// <returns>True if visible; otherwise, false.</returns>
         private bool IsTypeVisible(LogType type)
         {
            return type switch
               {
                  LogType.Warning => ShowWarn,
                  LogType.Error or LogType.Assert or LogType.Exception => ShowErr,
                  _ => ShowLog
               };
         }
      }
   }
#endif
