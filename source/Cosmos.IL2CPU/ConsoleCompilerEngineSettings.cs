using System;
using System.Collections.Generic;
using System.IO;

using Cosmos.Build.Common;

namespace Cosmos.IL2CPU
{
    internal class ConsoleCompilerEngineSettings : ICompilerEngineSettings
    {
        public bool EnableLogging => GetOption<bool>(nameof(EnableLogging));

        public bool EnableDebug => GetOption<bool>(nameof(EnableDebug));
        public DebugMode DebugMode => GetEnumOption<DebugMode>(nameof(DebugMode));
        public byte DebugCom => GetOption<byte>(nameof(DebugCom));
        public bool EmitDebugSymbols => GetOption<bool>(nameof(EmitDebugSymbols));
        public bool IgnoreDebugStubAttribute => GetOption<bool>(nameof(IgnoreDebugStubAttribute));

        public TraceAssemblies TraceAssemblies => GetEnumOption<TraceAssemblies>(nameof(TraceAssemblies));
        public bool EnableStackCorruptionDetection => GetOption<bool>(nameof(EnableStackCorruptionDetection));
        public StackCorruptionDetectionLevel StackCorruptionDetectionLevel =>
            GetEnumOption<StackCorruptionDetectionLevel>(nameof(StackCorruptionDetectionLevel));

        public string TargetAssembly => GetOption<string>(nameof(TargetAssembly));

        public IEnumerable<string> References => mReferences;
        public IEnumerable<string> PlugsReferences => mPlugsReferences;
        public IEnumerable<string> AssemblySearchDirs => mAssemblySearchDirs;

        public string OutputFilename => GetOption<string>(nameof(OutputFilename));

        public string ResponseFile => GetOption<string>(nameof(ResponseFile));

        private Action<string> mLogMessage;
        private Action<string> mLogError;

        private List<string> mReferences;
        private List<string> mPlugsReferences;
        private List<string> mAssemblySearchDirs;

        private Dictionary<string, string> mCmdOptions;

        public bool RemoveBootDebugOutput => GetOption<bool>(nameof(RemoveBootDebugOutput));
        public bool CompileVBEMultiboot => GetOption<bool>(nameof(CompileVBEMultiboot));
        public string VBEResolution => GetOption<string>(nameof(VBEResolution));
        public bool AllowComments => GetOption<bool>(nameof(AllowComments));
        public string TargetArchitecture => GetOption<string>(nameof(TargetArchitecture));

        public ConsoleCompilerEngineSettings(string[] aArgs, Action<string> aLogMessage, Action<string> aLogError)
        {
            mLogMessage = aLogMessage;
            mLogError = aLogError;

            mReferences = new List<string>();
            mPlugsReferences = new List<string>();
            mAssemblySearchDirs = new List<string>();

            mCmdOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            ParseArgs(aArgs);

            if (File.Exists(ResponseFile))
            {
                ParseArgs(File.ReadAllLines(ResponseFile));
            }

            if (mCmdOptions.TryGetValue("KernelPkg", out var xKernelPkg))
            {
                CompilerEngine.KernelPkg = xKernelPkg;
            }
        }

        private T GetEnumOption<T>(string aOptionName)
            where T : struct
        {
            var xValue = GetOption<string>(aOptionName);

            if (string.IsNullOrEmpty(xValue))
            {
                if (typeof(T) == typeof(TraceAssemblies))
                {
                    return (T)(object)TraceAssemblies.User;
                }
                else if (typeof(T) == typeof(StackCorruptionDetectionLevel))
                {
                    return (T)(object)StackCorruptionDetectionLevel.MethodFooters;
                }

                return default(T);
            }

            try
            {
                if (Enum.TryParse<T>(xValue, out var xEnumValue))
                {
                    return xEnumValue;
                }
            }
            catch (Exception e)
            {
                mLogError(e.ToString());
            }

            return default(T);
        }

        private T GetOption<T>(string aOptionName)
        {
            aOptionName = aOptionName.ToLower();

            if (mCmdOptions.TryGetValue(aOptionName, out var xValue))
            {
                try
                {
                    return (T)Convert.ChangeType(xValue, typeof(T));
                }
                catch (Exception e)
                {
                    mLogError(e.ToString());
                }
            }

            return typeof(T) == typeof(string) ? (T)(object)String.Empty : default(T);
        }

        private void ParseArgs(string[] args)
        {
            foreach (var arg in args)
            {
                var indexOfSeparator = arg.IndexOf(':');

                if (indexOfSeparator == -1)
                {
                    continue;
                }

                var key = arg.Substring(0, indexOfSeparator);
                var value = arg.Substring(indexOfSeparator + 1);

                if (String.Equals(key, "References", StringComparison.OrdinalIgnoreCase))
                {
                    value = Path.GetFullPath(value);
                    mReferences.Add(value);
                }
                else if (String.Equals(key, "PlugsReferences", StringComparison.OrdinalIgnoreCase))
                {
                    value = Path.GetFullPath(value);
                    mPlugsReferences.Add(value);
                }
                else if (String.Equals(key, "AssemblySearchDirs", StringComparison.OrdinalIgnoreCase))
                {
                    mAssemblySearchDirs.Add(value);
                }
                else
                {
                    mCmdOptions[key] = value;
                }
            }
        }
    }
}