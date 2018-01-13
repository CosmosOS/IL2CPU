using System;

namespace Cosmos.IL2CPU
{
    public class Program
    {
        private const int Succeeded = 0;
        private const int Failed = 1;

        public static int Run(string[] aArgs, Action<string> aLogMessage, Action<string> aLogError)
        {
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
            catch (Exception E)
            {
                aLogError(String.Format("Error occurred: " + E.ToString()));
            }

            return Failed;
        }

        private static bool RunCompilerEngine(string [] aaArgs, Action<string> aaLogMessage, Action<string> aaLogError)
        {
            var xSettings = new ConsoleCompilerEngineSettings(aaArgs, aaLogMessage, aaLogError);

            var xEngine = new CompilerEngine(xSettings)
            {
                OnaLogError = aaLogError,
                OnLogWarning = m => aaLogMessage(String.Format("Warning: {0}", m)),
                OnaLogMessage = aaLogMessage,
                OnLogException = (m) => aaLogError(String.Format("Exception: {0}", m.ToString()))
            };

            CompilerEngine.KernelPkg = xSettings.KernelPkg;

            return xEngine.Execute();
        }
    }
}
