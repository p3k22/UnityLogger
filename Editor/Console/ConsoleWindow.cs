#if UNITY_EDITOR
   namespace P3k.UnityRichConsoleLogger.Editor.Console
   {
      using P3k.UnityRichConsoleLogger.Editor.Console.Data;
      using P3k.UnityRichConsoleLogger.Editor.Console.Services;
      using P3k.UnityRichConsoleLogger.Editor.Console.Utilities;

      using System.Linq;

      using UnityEditor;

      using UnityEngine;

      using Logger = Logger;

      /// <summary>
      ///    Rich console window with virtualised scrolling, search/filter, collapse,
      ///    a resizable stack-trace detail panel with clickable links, and
      ///    double-click-to-open-IDE on any log row.
      /// </summary>
      internal sealed class ConsoleWindow : EditorWindow
      {
         /// <summary>Default height of the detail panel on first open.</summary>
         private const float DETAIL_DEFAULT = 140f;

         /// <summary>Minimum height of the detail / stack-trace panel.</summary>
         private const float DETAIL_MIN = 60f;

         /// <summary>Horizontal padding around the log-type icon.</summary>
         private const float ICON_PAD = 4f;

         /// <summary>Width and height of the log-type icon drawn in each row.</summary>
         private const float ICON_SIZE = 16f;

         /// <summary>Height in pixels of each log row in the virtualised list.</summary>
         private const float ROW_HEIGHT = 24f;

         /// <summary>Height of the draggable splitter bar between the log list and detail panel.</summary>
         private const float SPLITTER_H = 6f;

         /// <summary>Height of a single stack-trace line in the detail panel.</summary>
         private const float STACK_LINE_H = 20f;

         /// <summary>
         ///    When <see langword="true" />, the next <see cref="PlayModeStateChange.EnteredEditMode" />
         ///    transition will not clear the log, preserving entries from the previous play session.
         /// </summary>
         private static bool _suppressNextEnteredEditClear;

         /// <summary>Background colour of the detail / stack-trace panel.</summary>
         private static readonly Color PalDetail = new(0.150f, 0.150f, 0.150f);

         /// <summary>Tint overlay applied to error, assert, and exception rows.</summary>
         private static readonly Color PalErrTint = new(0.500f, 0.100f, 0.100f, 0.18f);

         /// <summary>Background colour for even-indexed log rows.</summary>
         private static readonly Color PalEven = new(0.200f, 0.200f, 0.200f);

         /// <summary>Semi-transparent overlay drawn when the cursor hovers over a row.</summary>
         private static readonly Color PalHover = new(0.280f, 0.280f, 0.320f, 0.50f);

         /// <summary>Default colour for clickable stack-trace links.</summary>
         private static readonly Color PalLink = new(0.40f, 0.70f, 1.00f);

         /// <summary>Hover colour for clickable stack-trace links.</summary>
         private static readonly Color PalLinkHov = new(0.60f, 0.85f, 1.00f);

         /// <summary>Background colour for odd-indexed log rows.</summary>
         private static readonly Color PalOdd = new(0.225f, 0.225f, 0.225f);

         /// <summary>Highlight overlay drawn on selected rows.</summary>
         private static readonly Color PalSelect = new(0.173f, 0.365f, 0.530f, 0.85f);

         /// <summary>Colour of the horizontal splitter bar between the log list and detail panel.</summary>
         private static readonly Color PalSplitter = new(0.100f, 0.100f, 0.100f);

         /// <summary>Tint overlay applied to success rows.</summary>
         private static readonly Color PalSuccessTint = new(0.100f, 0.500f, 0.100f, 0.12f);

         /// <summary>Tint overlay applied to warning rows.</summary>
         private static readonly Color PalWarnTint = new(0.500f, 0.450f, 0.050f, 0.10f);

         /// <summary>Handles search, type-toggle, and collapse filtering of visible rows.</summary>
         private readonly LogFilter _filter = new();

         /// <summary>Backing store that holds all received log rows.</summary>
         private readonly LogStore _store = new();

         /// <summary>Tracks which log rows are currently selected.</summary>
         private readonly SelectionModel _selection = new();

         /// <summary>Whether the log list automatically scrolls to the newest entry.</summary>
         private bool _autoScroll = true;

         /// <summary>Whether the user is currently dragging the splitter bar.</summary>
         private bool _draggingSplitter;

         /// <summary>Current pixel height of the detail panel, adjusted by dragging the splitter.</summary>
         private float _detailHeight = DETAIL_DEFAULT;

         /// <summary>Cached icon content for info, warning, and error log types.</summary>
         private GUIContent _icnInfo, _icnWarn, _icnErr;

         /// <summary>Lazy-initialised GUI styles for rows, detail text, collapse badges, and links.</summary>
         private GUIStyle _sRow, _sDetail, _sBadge, _sLink;

         /// <summary>Scroll positions for the log list and the detail panel respectively.</summary>
         private Vector2 _logScroll, _detailScroll;

         /// <summary>
         ///    Subscribes to log, play-mode, and assembly-reload events when the window is enabled.
         /// </summary>
         private void OnEnable()
         {
            Logger.GlobalMessageReceived += OnLogEvent;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.afterAssemblyReload += OnScriptsReloaded;
         }

         /// <summary>
         ///    Unsubscribes from log, play-mode, and assembly-reload events when the window is disabled.
         /// </summary>
         private void OnDisable()
         {
            Logger.GlobalMessageReceived -= OnLogEvent;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.afterAssemblyReload -= OnScriptsReloaded;
         }

         /// <summary>
         ///    Main GUI entry point — lays out the toolbar, log list, splitter, and detail panel.
         /// </summary>
         private void OnGUI()
         {
            EnsureStyles();
            HandleKeyCommands();
            DrawToolbar();

            // Calculate layout rects for the three-panel split
            var tbH = EditorStyles.toolbar.fixedHeight;
            var totalH = position.height;
            var dH = Mathf.Clamp(_detailHeight, DETAIL_MIN, totalH * 0.6f);
            var logH = Mathf.Max(40f, totalH - tbH - SPLITTER_H - dH);

            var logRect = new Rect(0, tbH, position.width, logH);
            var splitRect = new Rect(0, tbH + logH, position.width, SPLITTER_H);
            var detRect = new Rect(0, tbH + logH + SPLITTER_H, position.width, dH);

            if (_filter.IsDirty)
            {
               _filter.Rebuild(_store.Rows);
            }

            DrawLogArea(logRect);
            DrawSplitter(splitRect);
            DrawDetailPanel(detRect);
         }

         /// <summary>
         ///    Opens the P3k Console window from the Unity menu (<c>Ctrl+Shift+D</c>).
         /// </summary>
         [MenuItem("Window/General/P3k's Console  %#d")]
         private static void Open()
         {
            var w = GetWindow<ConsoleWindow>();
            w.minSize = new Vector2(500f, 280f);
            w.titleContent = new GUIContent(
            "P3k Console",
            EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image);
         }

         /// <summary>
         ///    Clears all stored log rows, the current selection, and the filter state.
         /// </summary>
         private void ClearAll()
         {
            _store.Clear();
            _selection.Clear();
            _filter.Clear();
         }

         /// <summary>Draws the bottom detail panel showing the full message and clickable stack trace.</summary>
         /// <param name="area">Screen-space rectangle allocated to the detail panel.</param>
         private void DrawDetailPanel(Rect area)
         {
            EditorGUI.DrawRect(area, PalDetail);

            var row = _selection.GetPrimarySelected(_store.Rows);
            if (row == null)
            {
               var lr = new Rect(area.x + 8f, area.y + 8f, area.width - 16f, 20f);
               EditorGUI.LabelField(lr, "Select a log entry to view details.", _sDetail);
               return;
            }

            // Parse the stack trace into individual lines
            var stackLines = StackTraceParser.ParseStackLines(row);

            var msgH = _sDetail.CalcHeight(new GUIContent(row.Text), area.width - 24f);
            msgH = Mathf.Max(msgH, 20f);
            var contentH = msgH + 12f + (stackLines.Count * STACK_LINE_H) + 16f;
            var cw = area.width - (contentH > area.height ? 14f : 0f);

            _detailScroll = GUI.BeginScrollView(area, _detailScroll, new Rect(0, 0, cw, contentH));

            var y = 8f;
            var evt = Event.current;

            // ── Full message ──
            var mr = new Rect(8f, y, cw - 16f, msgH);
            var pc = _sDetail.normal.textColor;
            _sDetail.normal.textColor = row.Color;
            EditorGUI.LabelField(mr, row.Text, _sDetail);
            _sDetail.normal.textColor = pc;
            y += msgH + 12f;

            // ── Stack trace lines (clickable where file refs exist) ──
            foreach (var sl in stackLines)
            {
               var lr = new Rect(8f, y, cw - 16f, STACK_LINE_H);

               if (sl.HasFile)
               {
                  // Make it look and act like a hyperlink
                  EditorGUIUtility.AddCursorRect(lr, MouseCursor.Link);
                  var hover = lr.Contains(evt.mousePosition);

                  if (evt.type == EventType.Repaint)
                  {
                     var plc = _sLink.normal.textColor;
                     _sLink.normal.textColor = hover ? PalLinkHov : PalLink;
                     _sLink.Draw(lr, new GUIContent(sl.Display), false, false, false, false);
                     _sLink.normal.textColor = plc;
                  }

                  if (evt.type == EventType.MouseDown && lr.Contains(evt.mousePosition))
                  {
                     SourceNavigator.OpenPathAtLine(sl.File, sl.Line);
                     evt.Use();
                  }
               }
               else if (evt.type == EventType.Repaint)
               {
                  _sDetail.Draw(lr, new GUIContent(sl.Display), false, false, false, false);
               }

               y += STACK_LINE_H;
            }

            GUI.EndScrollView();
         }

         /// <summary>Draws the virtualised, scrollable log row list.</summary>
         /// <param name="area">Screen-space rectangle allocated to the log list.</param>
         private void DrawLogArea(Rect area)
         {
            var evt = Event.current;
            var visible = _filter.Visible;
            var n = visible.Count;
            var contentH = n * ROW_HEIGHT;
            var hasBar = contentH > area.height;
            var cw = area.width - (hasBar ? 14f : 0f);

            // ── Detect user scroll-wheel → disable auto-scroll ──
            if (_autoScroll && evt.type == EventType.ScrollWheel && area.Contains(evt.mousePosition))
            {
               _autoScroll = false;
            }

            // ── Pin scroll to bottom *before* the scroll view so the
            //    GUI sees the correct position on this very frame. ──
            if (_autoScroll && contentH > area.height)
            {
               _logScroll.y = Mathf.Max(0f, contentH - area.height);
            }

            _logScroll = GUI.BeginScrollView(area, _logScroll, new Rect(0, 0, cw, contentH));

            // Only draw rows that are actually within the viewport
            var first = Mathf.Max(0, Mathf.FloorToInt(_logScroll.y / ROW_HEIGHT));
            var last = Mathf.Min(n - 1, Mathf.CeilToInt((_logScroll.y + area.height) / ROW_HEIGHT));

            for (var vi = first; vi <= last; vi++)
            {
               var ri = visible[vi];
               var row = _store.Rows[ri];
               var r = new Rect(0, vi * ROW_HEIGHT, cw, ROW_HEIGHT);
               DrawRow(r, vi, ri, row);
            }

            GUI.EndScrollView();
         }

         /// <summary>Draws a single log row including background, icon, optional badge, text, and mouse handling.</summary>
         /// <param name="r">Screen-space rectangle for the row.</param>
         /// <param name="vi">Visible index of the row within the filtered list.</param>
         /// <param name="ri">Raw index of the row within the backing store.</param>
         /// <param name="row">The log row data to render.</param>
         private void DrawRow(Rect r, int vi, int ri, Row row)
         {
            var evt = Event.current;
            var repaint = evt.type == EventType.Repaint;

            // ── Background layers ──
            if (repaint)
            {
               // Alternating base
               EditorGUI.DrawRect(r, vi % 2 == 0 ? PalEven : PalOdd);

               // Type-tint overlay
               if (row.Type is LogType.Error or LogType.Assert or LogType.Exception)
               {
                  EditorGUI.DrawRect(r, PalErrTint);
               }
               else if (row.Type == LogType.Warning)
               {
                  EditorGUI.DrawRect(r, PalWarnTint);
               }
               else if (row.IsSuccess)
               {
                  EditorGUI.DrawRect(r, PalSuccessTint);
               }

               // Hover highlight
               if (!_selection.Contains(ri) && r.Contains(evt.mousePosition))
               {
                  EditorGUI.DrawRect(r, PalHover);
               }

               // Selection highlight
               if (_selection.Contains(ri))
               {
                  EditorGUI.DrawRect(r, PalSelect);
               }
            }

            // ── Type icon ──
            var iconR = new Rect(r.x + ICON_PAD, r.y + ((ROW_HEIGHT - ICON_SIZE) * 0.5f), ICON_SIZE, ICON_SIZE);

            var icon = row.Type switch
               {
                  LogType.Warning => _icnWarn,
                  LogType.Error or LogType.Assert or LogType.Exception => _icnErr,
                  _ => _icnInfo
               };

            if (icon?.image != null && repaint)
            {
               GUI.DrawTexture(iconR, icon.image, ScaleMode.ScaleToFit);
            }

            var tx = iconR.xMax + ICON_PAD;

            // ── Collapse count badge ──
            if (_filter.Collapse && _filter.TryGetCollapseCount(row.CollapseKey, out var cnt) && cnt > 1)
            {
               var bc = new GUIContent(cnt.ToString());
               var bs = _sBadge.CalcSize(bc);
               bs.x = Mathf.Max(bs.x + 8f, 22f);
               var br = new Rect(tx, r.y + ((ROW_HEIGHT - bs.y) * 0.5f), bs.x, bs.y);
               if (repaint)
               {
                  EditorGUI.DrawRect(br, new Color(0.35f, 0.35f, 0.35f, 0.80f));
                  _sBadge.Draw(br, bc, false, false, false, false);
               }

               tx = br.xMax + 4f;
            }

            // ── Row text ──
            var textR = new Rect(tx, r.y, r.xMax - tx - 4f, ROW_HEIGHT);
            if (repaint)
            {
               var prev = _sRow.normal.textColor;
               _sRow.normal.textColor = row.Color;
               _sRow.Draw(textR, new GUIContent(row.Text), false, false, false, false);
               _sRow.normal.textColor = prev;
            }

            // ── Mouse interaction ──
            HandleRowMouse(r, vi, ri, row);
         }

         /// <summary>Draws the draggable horizontal splitter and handles mouse-drag resizing.</summary>
         /// <param name="r">Screen-space rectangle for the splitter bar.</param>
         private void DrawSplitter(Rect r)
         {
            EditorGUI.DrawRect(r, PalSplitter);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeVertical);

            var evt = Event.current;
            switch (evt.type)
            {
               case EventType.MouseDown when r.Contains(evt.mousePosition):
                  _draggingSplitter = true;
                  evt.Use();
                  break;

               case EventType.MouseDrag when _draggingSplitter:
                  _detailHeight = Mathf.Clamp(_detailHeight - evt.delta.y, DETAIL_MIN, position.height * 0.6f);
                  Repaint();
                  evt.Use();
                  break;

               case EventType.MouseUp when _draggingSplitter:
                  _draggingSplitter = false;
                  evt.Use();
                  break;
            }
         }

         /// <summary>
         ///    Draws the top toolbar with clear, search, type toggles, collapse, and auto-scroll controls.
         /// </summary>
         private void DrawToolbar()
         {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
               if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50f)))
               {
                  ClearAll();
               }

               GUILayout.Space(4f);

               // ── Search field ──
               EditorGUI.BeginChangeCheck();
               var s = GUILayout.TextField(_filter.SearchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(100f));
               if (EditorGUI.EndChangeCheck() && s != _filter.SearchText)
               {
                  _filter.SearchText = s;
                  _filter.MarkDirty();
               }

               GUILayout.FlexibleSpace();

               // ── Type toggles ──
               ToolbarToggle("ShowLog", _filter.ShowLog, $" {_store.CountLog}", _icnInfo, "Logs", 54f);
               ToolbarToggle("ShowWarn", _filter.ShowWarn, $" {_store.CountWarn}", _icnWarn, "Warnings", 54f);
               ToolbarToggle("ShowErr", _filter.ShowErr, $" {_store.CountErr}", _icnErr, "Errors", 54f);

               GUILayout.Space(6f);

               // ── Collapse ──
               EditorGUI.BeginChangeCheck();
               _filter.Collapse = GUILayout.Toggle(
               _filter.Collapse,
               new GUIContent("Collapse", "Group identical messages"),
               EditorStyles.toolbarButton,
               GUILayout.Width(62f));
               if (EditorGUI.EndChangeCheck())
               {
                  _filter.MarkDirty();
               }

               // ── Auto-scroll ──
               EditorGUI.BeginChangeCheck();
               _autoScroll = GUILayout.Toggle(
               _autoScroll,
               new GUIContent("Auto \u2193", "Auto-scroll to newest"),
               EditorStyles.toolbarButton,
               GUILayout.Width(52f));
               if (EditorGUI.EndChangeCheck() && _autoScroll)
               {
                  // User just re-enabled — trim any excess that built up.
                  if (_store.NeedsTrim)
                  {
                     _store.TrimIfNeeded();
                     _selection.Clear();
                     _filter.MarkDirty();
                  }
               }
            }
         }

         /// <summary>
         ///    Lazily initialises GUI styles and icon content on first use.
         /// </summary>
         private void EnsureStyles()
         {
            if (_sRow != null)
            {
               return;
            }

            _sRow = new GUIStyle(EditorStyles.label)
                       {
                          richText = false,
                          wordWrap = false,
                          clipping = TextClipping.Clip,
                          alignment = TextAnchor.MiddleLeft,
                          fontSize = 12,
                          padding = new RectOffset(2, 4, 0, 0)
                       };

            _sDetail = new GUIStyle(EditorStyles.label)
                          {
                             richText = false,
                             wordWrap = true,
                             fontSize = 11,
                             normal = {textColor = new Color(0.78f, 0.78f, 0.78f)}
                          };

            _sBadge = new GUIStyle(EditorStyles.miniLabel)
                         {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 10,
                            fontStyle = FontStyle.Bold,
                            normal = {textColor = Color.white}
                         };

            _sLink = new GUIStyle(EditorStyles.label)
                        {
                           richText = false,
                           wordWrap = false,
                           fontSize = 11,
                           normal = {textColor = PalLink},
                           padding = new RectOffset(2, 4, 0, 0)
                        };

            _icnInfo = EditorGUIUtility.IconContent("console.infoicon.sml");
            _icnWarn = EditorGUIUtility.IconContent("console.warnicon.sml");
            _icnErr = EditorGUIUtility.IconContent("console.erroricon.sml");
         }

         /// <summary>
         ///    Processes keyboard commands such as <c>Ctrl+C</c> to copy selected log text.
         /// </summary>
         private void HandleKeyCommands()
         {
            var evt = Event.current;

            // Validate Copy
            if (evt.type == EventType.ValidateCommand && evt.commandName == "Copy" && _selection.Count > 0)
            {
               evt.Use();
               return;
            }

            // Execute Copy  (menu command or Ctrl+C)
            var isCopy = (evt.type == EventType.ExecuteCommand && evt.commandName == "Copy")
                         || (evt.type == EventType.KeyDown && (evt.control || evt.command) && evt.keyCode == KeyCode.C);

            if (isCopy && _selection.Count > 0)
            {
               EditorGUIUtility.systemCopyBuffer = _selection.BuildSelectedText(_filter.Visible, _store.Rows);
               evt.Use();
            }
         }

         /// <summary>Handles mouse clicks on a log row — single, double, shift, and ctrl/cmd selection.</summary>
         /// <param name="r">Screen-space rectangle of the row.</param>
         /// <param name="vi">Visible index of the row within the filtered list.</param>
         /// <param name="ri">Raw index of the row within the backing store.</param>
         /// <param name="row">The log row data associated with this click target.</param>
         private void HandleRowMouse(Rect r, int vi, int ri, Row row)
         {
            var evt = Event.current;
            if (evt.button != 0 || !r.Contains(evt.mousePosition))
            {
               return;
            }

            // ── Double-click → open source in IDE ──
            if (evt.type == EventType.MouseDown && evt.clickCount == 2)
            {
               SourceNavigator.TryOpenRowInIde(row);
               evt.Use();
               return;
            }

            if (evt.type != EventType.MouseDown)
            {
               return;
            }

            // ── Shift-click: range select ──
            if (evt.shift && _selection.LastClickedVisible >= 0 && _selection.LastClickedVisible < _filter.Visible.Count)
            {
               _selection.SelectRange(_filter.Visible, _selection.LastClickedVisible, vi);
            }
            // ── Ctrl/Cmd-click: toggle ──
            else if (EditorGUI.actionKey)
            {
               _selection.Toggle(ri);
               _selection.LastClickedVisible = vi;
            }
            // ── Normal click ──
            else
            {
               _selection.Set(ri);
               _selection.LastClickedVisible = vi;
            }

            Repaint();
            evt.Use();
         }

         /// <summary>Callback invoked when a new log message is received from the logger.</summary>
         /// <param name="e">The incoming log event.</param>
         private void OnLogEvent(LogEvent e)
         {
            _store.Add(e);

            // Only trim while auto-scroll is active — when the user is
            // scrolling manually we leave the buffer intact so they can
            // browse freely without messages vanishing under them.
            if (_autoScroll && _store.NeedsTrim)
            {
               _store.TrimIfNeeded();
               _selection.Clear();
            }

            _filter.MarkDirty();
            Repaint();
         }

         /// <summary>Handles play-mode transitions — clears logs on enter and optionally preserves them on exit.</summary>
         /// <param name="change">The play-mode state that was entered.</param>
         private void OnPlayModeChanged(PlayModeStateChange change)
         {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
               _suppressNextEnteredEditClear = true;
               return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
               ClearAll();
               _autoScroll = true;
               Repaint();
               return;
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
               if (_suppressNextEnteredEditClear)
               {
                  _suppressNextEnteredEditClear = false;
                  return;
               }

               ClearAll();
               Repaint();
            }
         }

         /// <summary>Clears and repaints the console after a domain / script reload,
         /// unless a play-mode exit is in progress (logs are preserved).
         /// </summary>
         private void OnScriptsReloaded()
         {
            if (_suppressNextEnteredEditClear)
            {
               return;
            }

            ClearAll();
            Repaint();
         }

         /// <summary>Draws a single toolbar toggle button and updates the corresponding filter property when changed.</summary>
         /// <param name="property">Name of the filter property to update (<c>ShowLog</c>, <c>ShowWarn</c>, or <c>ShowErr</c>).</param>
         /// <param name="value">Current toggle state.</param>
         /// <param name="label">Display label including the count.</param>
         /// <param name="icon">Optional icon content shown beside the label.</param>
         /// <param name="tip">Tooltip text.</param>
         /// <param name="w">Fixed width of the toggle button.</param>
         private void ToolbarToggle(string property, bool value, string label, GUIContent icon, string tip, float w)
         {
            EditorGUI.BeginChangeCheck();
            var newVal = GUILayout.Toggle(
            value,
            new GUIContent(label, icon?.image, tip),
            EditorStyles.toolbarButton,
            GUILayout.Width(w));
            if (EditorGUI.EndChangeCheck() && newVal != value)
            {
               switch (property)
               {
                  case "ShowLog": _filter.ShowLog = newVal; break;
                  case "ShowWarn": _filter.ShowWarn = newVal; break;
                  case "ShowErr": _filter.ShowErr = newVal; break;
               }

               _filter.MarkDirty();
            }
         }
      }
   }
#endif