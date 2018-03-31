using System;
using System.Collections.Generic;
using System.IO;

using Serilog;

using Cosmos.Build.Common;

namespace Cosmos.IL2CPU
{
    internal class ConsoleCompilerEngineSettings : ICompilerEngineSettings
    {
        public bool EnableLogging
        {
            get => GetOption<bool>(nameof(EnableLogging));
            set => mCmdOptions[nameof(EnableLogging)] = value.ToString();
        }

        public bool EnableDebug => GetOption<bool>(nameof(EnableDebug));
        public DebugMode DebugMode => GetEnumOption<DebugMode>(nameof(DebugMode));
        public byte DebugCom
        {
            get => GetOption<byte>(nameof(DebugCom));
            set => mCmdOptions[nameof(DebugCom)] = value.ToString();
        }
        public bool EmitDebugSymbols => GetOption<bool>(nameof(EmitDebugSymbols));
        public bool IgnoreDebugStubAttribute => GetOption<bool>(nameof(IgnoreDebugStubAttribute));

        public TraceAssemblies TraceAssemblies => GetEnumOption<TraceAssemblies>(nameof(TraceAssemblies));
        public bool EnableStackCorruptionDetection => GetOption<bool>(nameof(EnableStackCorruptionDetection));
        public StackCorruptionDetectionLevel StackCorruptionDetectionLevel =>
            GetEnumOption<StackCorruptionDetectionLevel>(nameof(StackCorruptionDetectionLevel));

        public IEnumerable<string> References => mReferences;
        public IEnumerable<string> AssemblySearchDirs => mAssemblySearchDirs;

        public string OutputFilename => GetOption<string>(nameof(OutputFilename));

        public string ResponseFile => GetOption<string>(nameof(ResponseFile));

        private ILogger _logger;

        private List<string> mReferences;
        private List<string> mAssemblySearchDirs;

        private Dictionary<string, string> mCmdOptions;

        public ConsoleCompilerEngineSettings(string[] aArgs, ILogger logger)
        {
            _logger = logger;

            mReferences = new List<string>();
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

            if (String.IsNullOrEmpty(xValue))
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
                _logger.Error(e, "Error parsing argument!");
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
                    _logger.Error(e, "Invalid conversion of argument!");
                }
            }

            return typeof(T) == typeof(string) ? (T)(object)String.Empty : default(T);
        }

        private void ParseArgs(string[] args)
        {
            foreach (var s in args)
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
                    mCmdOptions[argID] = s.Replace(s1[0] + ":", "");
                }
            }
        }
    }
}
