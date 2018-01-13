using System.Collections.Generic;

using Cosmos.Build.Common;

namespace Cosmos.IL2CPU
{
    public interface ICompilerEngineSettings
    {
        bool EnableLogging { get; set; }

        bool EnableDebug { get; }
        DebugMode DebugMode { get; }
        byte DebugCom { get; set; }
        bool EmitDebugSymbols { get; }
        bool IgnoreDebugStubAttribute { get; }

        TraceAssemblies TraceAssemblies { get; }
        bool EnableStackCorruptionDetection { get; }
        StackCorruptionDetectionLevel StackCorruptionDetectionLevel { get; }

        IEnumerable<string> References { get; }
        IEnumerable<string> AssemblySearchDirs { get; }

        string OutputFilename { get; }
    }
}
