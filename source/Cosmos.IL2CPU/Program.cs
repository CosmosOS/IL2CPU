using System;

using Serilog;

namespace Cosmos.IL2CPU
{
    public class Program
    {
        private const int Succeeded = 0;
        private const int Failed = 1;

        public static int Run(string[] aArgs, ILogger logger)
        {
            if (aArgs == null)
            {
                throw new ArgumentNullException(nameof(aArgs));
            }

            try
            {
                if (RunCompilerEngine(aArgs, logger))
                {
                    return Succeeded;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error occurred");
            }

            return Failed;
        }

        private static bool RunCompilerEngine(string[] aArgs, ILogger logger)
        {
            var xSettings = new ConsoleCompilerEngineSettings(aArgs, logger);
            var xEngine = new CompilerEngine(xSettings, logger);

            return xEngine.Execute();
        }
    }
}
