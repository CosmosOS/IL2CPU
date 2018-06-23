//#define COSMOSDEBUG

using System;
using System.Diagnostics;

namespace Cosmos.IL2CPU
{
    public static class CompilerHelpers
    {
#pragma warning disable CA1710 // Identifiers should have correct suffix
        public static event Action<string> DebugEvent;
#pragma warning restore CA1710 // Identifiers should have correct suffix

        private static void DoDebug(string message)
        {
            if (DebugEvent != null)
            {
                DebugEvent(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        [Conditional("COSMOSDEBUG")]
        public static void Debug(string aMessage, params object[] aParams)
        {
            var xMessage = aMessage;

            if (aParams != null)
            {
                xMessage = xMessage + " : ";
                for (int i = 0; i < aParams.Length; i++)
                {
                    var xParam = aParams[i].ToString();
                    if (!String.IsNullOrWhiteSpace(xParam))
                    {
                        xMessage = xMessage + " " + xParam;
                    }
                }
            }

            DoDebug(xMessage);
        }

        [Conditional("COSMOSDEBUG")]
        public static void Debug(string aMessage) => DoDebug(aMessage);
    }
}
