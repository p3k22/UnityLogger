#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Services
   {
      using P3k.UnityRichConsoleLogger.Editor.Console.Data;

      using System.Collections.Generic;
      using System.Linq;
      using System.Text;

      using UnityEngine;

      /// <summary>
      ///    Tracks selected log rows and provides selection utilities.
      /// </summary>
      internal sealed class SelectionModel
      {
         private readonly HashSet<int> _selected = new();

         /// <summary>
         ///    Gets the number of selected rows.
         /// </summary>
         public int Count => _selected.Count;

         /// <summary>
         ///    Gets or sets the last clicked visible index.
         /// </summary>
         public int LastClickedVisible { get; set; } = -1;

         /// <summary>
         ///    Adds a row index to the selection.
         /// </summary>
         /// <param name="rowIndex">The row index to add.</param>
         public void Add(int rowIndex)
         {
            _selected.Add(rowIndex);
         }

         /// <summary>
         ///    Builds the text for the currently selected rows.
         /// </summary>
         /// <param name="visible">The visible row indexes.</param>
         /// <param name="rows">The row data.</param>
         /// <returns>The combined selected text.</returns>
         public string BuildSelectedText(IReadOnlyList<int> visible, IReadOnlyList<Row> rows)
         {
            if (_selected.Count == 0)
            {
               return string.Empty;
            }

            var sb = new StringBuilder();
            for (var vi = 0; vi < visible.Count; vi++)
            {
               var ri = visible[vi];
               if (!_selected.Contains(ri))
               {
                  continue;
               }

               if (sb.Length > 0)
               {
                  sb.AppendLine();
               }

               var row = rows[ri];
               sb.Append(row.Text);

               if (!string.IsNullOrEmpty(row.Stack))
               {
                  sb.AppendLine();
                  sb.Append(row.Stack);
               }
            }

            return sb.ToString();
         }

         /// <summary>
         ///    Clears the selection.
         /// </summary>
         public void Clear()
         {
            _selected.Clear();
            LastClickedVisible = -1;
         }

         /// <summary>
         ///    Determines whether the selection contains a row index.
         /// </summary>
         /// <param name="rowIndex">The row index to check.</param>
         /// <returns>True if selected; otherwise, false.</returns>
         public bool Contains(int rowIndex)
         {
            return _selected.Contains(rowIndex);
         }

         /// <summary>
         ///    Gets the primary selected row.
         /// </summary>
         /// <param name="rows">The row data.</param>
         /// <returns>The primary selected row, or null if none.</returns>
         public Row GetPrimarySelected(IReadOnlyList<Row> rows)
         {
            if (_selected.Count == 0)
            {
               return null;
            }

            var max = -1;
            foreach (var ri in _selected)
            {
               if (ri > max)
               {
                  max = ri;
               }
            }

            return max >= 0 && max < rows.Count ? rows[max] : null;
         }

         /// <summary>
         ///    Resets the selection anchor.
         /// </summary>
         public void ResetAnchor()
         {
            LastClickedVisible = -1;
         }

         /// <summary>
         ///    Selects a range of visible rows.
         /// </summary>
         /// <param name="visible">The visible row indexes.</param>
         /// <param name="fromVisible">The starting visible index.</param>
         /// <param name="toVisible">The ending visible index.</param>
         public void SelectRange(IReadOnlyList<int> visible, int fromVisible, int toVisible)
         {
            var a = Mathf.Min(fromVisible, toVisible);
            var b = Mathf.Max(fromVisible, toVisible);
            _selected.Clear();
            for (var v = a; v <= b; v++)
            {
               if (v >= 0 && v < visible.Count)
               {
                  _selected.Add(visible[v]);
               }
            }
         }

         /// <summary>
         ///    Sets the selection to a single row.
         /// </summary>
         /// <param name="rowIndex">The row index to select.</param>
         public void Set(int rowIndex)
         {
            _selected.Clear();
            _selected.Add(rowIndex);
         }

         /// <summary>
         ///    Toggles a row selection.
         /// </summary>
         /// <param name="rowIndex">The row index to toggle.</param>
         /// <returns>True if the row was selected; otherwise, false.</returns>
         public bool Toggle(int rowIndex)
         {
            if (!_selected.Remove(rowIndex))
            {
               _selected.Add(rowIndex);
               return true;
            }

            return false;
         }
      }
   }
#endif
