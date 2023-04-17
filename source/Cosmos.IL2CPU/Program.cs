using System;
using System.Diagnostics;

namespace Cosmos.IL2CPU
{
    public static class Program
    {
        private const int Succeeded = 0;
        private const int Failed = 1;

        public static int Run(string[] aArgs, Action<string> aLogMessage, Action<string> aLogError)
        {
            var debug = Environment.GetEnvironmentVariable("IL2CPU_DEBUG");

            if (string.Equals(debug, bool.TrueString, StringComparison.OrdinalIgnoreCase)
                || string.Equals(debug, "1", StringComparison.OrdinalIgnoreCase))
            {
                aLogMessage($"Waiting for debugger. PID: {Process.GetCurrentProcess().Id}");

                while (!Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }

            #region Null Checks

            if (aArgs == null)
            {
                throw new ArgumentNullException(nameof(aArgs));
            }

            if (aLogMessage == null)
            {
                throw new ArgumentNullException(nameof(aLogMessage));
            }

            if (aLogError == null)
            {
                throw new ArgumentNullException(nameof(aLogError));
            }

            #endregion

            try
            {
                if (RunCompilerEngine(aArgs, aLogMessage, aLogError))
                {
                    return Succeeded;
                }
            }
            catch (Exception e)
            {
                aLogError(string.Format("Error occurred: " + e.ToString()));
            }

            return Failed;
        }

        private static bool RunCompilerEngine(string[] aArgs, Action<string> aLogMessage, Action<string> aLogError)
        {
            var xSettings = new ConsoleCompilerEngineSettings(aArgs, aLogMessage, aLogError);

            var xEngine = new CompilerEngine(xSettings)
            {
                OnLogError = aLogError,
                OnLogWarning = m => aLogMessage(string.Format("Warning: {0}", m)),
                OnLogMessage = aLogMessage,
                OnLogException = m => aLogError(string.Format("Exception: {0}", m.ToString()))
            };

            return xEngine.Execute();
        }
    }
}
