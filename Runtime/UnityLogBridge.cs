namespace P3k.UnityRichConsoleLogger
{
   using System.Linq;

   using UnityEngine;

   /// <summary>
   ///    Bridges Unity's built-in log pipeline to the rich console logger by
   ///    subscribing to <see cref="Application.logMessageReceived" /> and forwarding
   ///    each message through <see cref="Logger" />.
   /// </summary>
   internal static class UnityLogBridge
   {
      /// <summary>
      ///    Shared logger instance created once on first initialization.
      /// </summary>
      private static Logger _logger;

      /// <summary>
      ///    Initializes the bridge before the first scene loads, creating a
      ///    <see cref="Logger" /> instance and subscribing to Unity's log callback.
      /// </summary>
      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void Init()
      {
         if (_logger != null)
         {
            return;
         }

         _logger = new Logger();
         Application.logMessageReceived += OnUnityLog;
      }

      /// <summary>
      ///    Callback invoked by <see cref="Application.logMessageReceived" />.
      ///    Maps the Unity <see cref="LogType" /> to a hex colour and forwards the
      ///    message to the logger, skipping messages already emitted by
      ///    <see cref="Logger" /> to avoid infinite recursion.
      /// </summary>
      /// <param name="condition">The log message text.</param>
      /// <param name="stackTrace">The associated stack trace.</param>
      /// <param name="type">The Unity log type of the message.</param>
      private static void OnUnityLog(string condition, string stackTrace, LogType type)
      {
         if (_logger == null)
         {
            return;
         }

         // Ignore logs already coming from Logger.Log(...)
         if (Logger.IsEmitting)
         {
            return;
         }

         var color = type switch
            {
               LogType.Error or LogType.Assert or LogType.Exception => "FF4500",
               LogType.Warning => "FFD700",
               _ => "FFFFFF"
            };

         Logger.EmitFromBridge(condition, type, color, stackTrace);
      }
   }
}