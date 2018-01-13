using System;
using System.Collections.Generic;

using Cosmos.Build.Common;

namespace Cosmos.IL2CPU
{
    internal class ConsoleCompilerEngineSettings : ICompilerEngineSettings
    {
        private Action<string> mLogMessage;
        private Action<string> mLogError;

        private List<string> mReferences;
        private List<string> mAssemblySearchDirs;

        private Dictionary<string, string> mCmdOptions;

        public ConsoleCompilerEngineSettings(string[] aArgs, Action<string> aLogMessage, Action<string> aLogError)
        {
            mLogMessage = aLogMessage;
            mLogError = aLogError;

            mReferences = new List<string>();
            mAssemblySearchDirs = new List<string>();

            mCmdOptions = new Dictionary<string, string>();

            foreach (var s in aArgs)
            {
                string[] s1 = s.Split(':');
                string argID = s1[0].ToLower();
                if (argID == "References".ToLower())
                {
                    mReferences.Add(s.Replace(s1[0] + ":", ""));
                }
                else if (argID == "AssemblySearchDirs".ToLower())
                {
                    mAssemblySearchDirs.Add(s.Replace(s1[0] + ":", ""));
                }
                else
                {
                    mCmdOptions.Add(argID, s.Replace(s1[0] + ":", ""));
                }
            }
        }

        public bool EnableLogging
        {
            get => GetOption<bool>(nameof(EnableLogging));
            set => mCmdOptions[nameof(EnableLogging)] = value.ToString();
        }

        public bool EnableDebug => GetOption<bool>(nameof(EnableDebug));
        public DebugMode DebugMode => GetOption<DebugMode>(nameof(DebugMode));
        public byte DebugCom
        {
            get => GetOption<byte>(nameof(DebugCom));
            set => mCmdOptions[nameof(DebugCom)] = value.ToString();
        }
        public bool EmitDebugSymbols => GetOption<bool>(nameof(EmitDebugSymbols));
        public bool IgnoreDebugStubAttribute => GetOption<bool>(nameof(IgnoreDebugStubAttribute));

        public TraceAssemblies TraceAssemblies => GetOption<TraceAssemblies>(nameof(TraceAssemblies));
        public bool EnableStackCorruptionDetection => GetOption<bool>(nameof(EnableStackCorruptionDetection));
        public StackCorruptionDetectionLevel StackCorruptionDetectionLevel =>
            GetOption<StackCorruptionDetectionLevel>(nameof(StackCorruptionDetectionLevel));

        public IEnumerable<string> References => mReferences;
        public IEnumerable<string> AssemblySearchDirs => mAssemblySearchDirs;

        public string OutputFilename => GetOption<string>(nameof(OutputFilename));

        public string KernelPkg => GetOption<string>(nameof(KernelPkg));

        private T GetOption<T>(string aOptionName)
        {
            aOptionName = aOptionName.ToLower();

            if (mCmdOptions.TryGetValue(aOptionName, out var xValue))
            {
                try
                {
                    if (typeof(T).IsEnum)
                    {
                        return (T)Enum.Parse(typeof(T), xValue, true);
                    }

                    return (T)Convert.ChangeType(xValue, typeof(T));
                }
                catch (Exception e)
                {
                    mLogError(e.ToString());
                }
            }

            return typeof(T) == typeof(string) ? (T)(object)String.Empty : default(T);
        }
    }
}
