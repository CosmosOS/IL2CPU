using System.Collections.Generic;

using Cosmos.Build.Common;

namespace Cosmos.IL2CPU
{
    public interface ICompilerEngineSettings
    {
        bool EnableLogging { get; }

        bool EnableDebug { get; }
        DebugMode DebugMode { get; }
        byte DebugCom { get; }
        bool EmitDebugSymbols { get; }
        bool IgnoreDebugStubAttribute { get; }

        TraceAssemblies TraceAssemblies { get; }
        bool EnableStackCorruptionDetection { get; }
        StackCorruptionDetectionLevel StackCorruptionDetectionLevel { get; }

        string TargetAssembly { get; }

        IEnumerable<string> References { get; }
        IEnumerable<string> PlugsReferences { get; }
        IEnumerable<string> AssemblySearchDirs { get; }

        string OutputFilename { get; }

        bool EnableLittleOptimization { get; }
        bool CompileVBEMultiboot { get; }
        string VBEResolution { get;  }
    }
}
