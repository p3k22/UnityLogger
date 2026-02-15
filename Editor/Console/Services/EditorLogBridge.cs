#if UNITY_EDITOR
namespace P3k.UnityRichConsoleLogger.Editor.Console.Services
   {
      using System.Linq;

      using UnityEditor;

      using UnityEngine;

      using Logger = Logger;

      /// <summary>
      /// Editor-only bridge that forwards Unity log messages to the rich console
      /// logger while in Edit Mode. Automatically unsubscribes during Play Mode so
      /// the runtime <see cref="UnityLogBridge"/> handles logs instead.
      /// </summary>
      [InitializeOnLoad]
      internal static class EditorLogBridge
      {
         /// <summary>
         /// Tracks whether the bridge is currently subscribed to
         /// <see cref="Application.logMessageReceived"/>.
         /// </summary>
         private static bool _subscribed;

         /// <summary>
         /// Registers editor callbacks for play-mode transitions and assembly
         /// reloads, then performs the initial subscription check.
         /// </summary>
         static EditorLogBridge()
         {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
            UpdateSubscription();
         }

         /// <summary>
         /// Called after a domain reload to re-evaluate the subscription state.
         /// </summary>
         private static void AfterAssemblyReload()
         {
            UpdateSubscription();
         }

         /// <summary>
         /// Called when the editor transitions between Edit and Play Mode,
         /// triggering a subscription update.
         /// </summary>
         /// <param name="change">The play-mode transition that occurred.</param>
         private static void OnPlayModeStateChanged(PlayModeStateChange change)
         {
            UpdateSubscription();
         }

         /// <summary>
         /// Callback invoked by <see cref="Application.logMessageReceived"/>.
         /// Maps the Unity <see cref="LogType"/> to a hex colour and forwards the
         /// message to the logger, skipping messages during Play Mode or those
         /// already emitted by <see cref="Logger"/> to avoid infinite recursion.
         /// </summary>
         /// <param name="condition">The log message text.</param>
         /// <param name="stackTrace">The associated stack trace.</param>
         /// <param name="type">The Unity log type of the message.</param>
         private static void OnUnityLog(string condition, string stackTrace, LogType type)
         {
            // Do not forward during play (runtime bridge is active) or if already emitting from Logger.
            if (Application.isPlaying || Logger.IsEmitting)
            {
               return;
            }

            var logger = new Logger();
            var color = type switch
               {
                  LogType.Error or LogType.Assert or LogType.Exception => "FF4500",
                  LogType.Warning => "FFD700",
                  _ => "FFFFFF"
               };

            Logger.EmitFromBridge(condition, type, color, stackTrace);
         }

         /// <summary>
         /// Subscribes to <see cref="Application.logMessageReceived"/> in Edit Mode
         /// and unsubscribes in Play Mode so the runtime bridge handles logs.
         /// </summary>
         private static void UpdateSubscription()
         {
            var shouldSubscribe = !Application.isPlaying;

            if (shouldSubscribe && !_subscribed)
            {
               Application.logMessageReceived += OnUnityLog;
               _subscribed = true;
            }
            else if (!shouldSubscribe && _subscribed)
            {
               Application.logMessageReceived -= OnUnityLog;
               _subscribed = false;
            }
         }
      }
   }
#endif
