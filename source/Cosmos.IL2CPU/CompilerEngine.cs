﻿//#define COSMOSDEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Cosmos.Build.Common;

using IL2CPU.Debug.Symbols;
using IL2CPU.API.Attribs;

using XSharp.Assembler;

namespace Cosmos.IL2CPU
{
    // http://blogs.msdn.com/b/visualstudio/archive/2010/07/06/debugging-msbuild-script-with-visual-studio.aspx
    internal class CompilerEngine
    {
        public Action<string> OnLogMessage;
        public Action<string> OnLogError;
        public Action<string> OnLogWarning;
        public Action<Exception> OnLogException;
        protected static Action<string> mStaticLog = null;

        public static string KernelPkg { get; set; }
        public static bool UseGen3Kernel
        {
            get
            {
                return String.Equals(KernelPkg, "X86", StringComparison.OrdinalIgnoreCase);
            }
        }

        private ICompilerEngineSettings mSettings;

        private Dictionary<MethodBase, int?> mBootEntries;
        private List<MemberInfo> mForceIncludes;

        public string AssemblerLog = "XSharp.Assembler.log";

        protected void LogTime(string message)
        {
        }

        protected void LogMessage(string aMsg)
        {
            OnLogMessage?.Invoke(aMsg);
        }

        protected void LogWarning(string aMsg)
        {
            OnLogWarning?.Invoke(aMsg);
        }

        protected void LogError(string aMsg)
        {
            OnLogError?.Invoke(aMsg);
        }

        protected void LogException(Exception e)
        {
            OnLogException?.Invoke(e);
        }

        public CompilerEngine(ICompilerEngineSettings aSettings)
        {
            mSettings = aSettings;
            EnsureCosmosPathsInitialization();
        }

        private bool EnsureCosmosPathsInitialization()
        {
            try
            {
                CosmosPaths.Initialize();
                return true;
            }
            catch (Exception e)
            {
                var builder = new StringBuilder();
                builder.Append("Error while initializing Cosmos paths");
                for (Exception scannedException = e; null != scannedException; scannedException = scannedException.InnerException)
                {
                    builder.Append(" | " + scannedException.Message);
                }
                LogError(builder.ToString());
                return false;
            }
        }
        
        public bool Execute()
        {
            try
            {
                LogMessage("Executing IL2CPU on assembly");
                LogTime("Engine execute started");

                AssemblyLoadContext.Default.Resolving += Default_Resolving;

                // Gen2
                // Find the kernel's entry point. We are looking for a public class Kernel, with public static void Boot()
                MethodBase xKernelCtor = null;

                if (UseGen3Kernel)
                {
                    LoadBootEntries();
                }
                else
                {
                    xKernelCtor = LoadAssemblies();
                    if (xKernelCtor == null)
                    {
                        return false;
                    }
                }

                var xOutputFilenameWithoutExtension = Path.ChangeExtension(mSettings.OutputFilename, null);
                if (!mSettings.EnableDebug)
                {
                    // Default of 1 is in Cosmos.Targets. Need to change to use proj props.
                    mSettings.DebugCom = 0;
                }

                using (var xAsm = GetAppAssembler())
                {
                    using (var xDebugInfo = new DebugInfo(xOutputFilenameWithoutExtension + ".cdb", true, false))
                    {
                        xAsm.DebugInfo = xDebugInfo;
                        xAsm.DebugEnabled = mSettings.EnableDebug;
                        xAsm.StackCorruptionDetection = mSettings.EnableStackCorruptionDetection;
                        xAsm.StackCorruptionDetectionLevel = mSettings.StackCorruptionDetectionLevel;
                        xAsm.DebugMode = mSettings.DebugMode;
                        xAsm.TraceAssemblies = mSettings.TraceAssemblies;
                        xAsm.IgnoreDebugStubAttribute = mSettings.IgnoreDebugStubAttribute;
                        if (!mSettings.EnableDebug)
                        {
                            xAsm.ShouldOptimize = true;
                        }

                        xAsm.Assembler.Initialize();
                        using (var xScanner = new ILScanner(xAsm))
                        {
                            xScanner.LogException = LogException;
                            xScanner.LogWarning = LogWarning;
                            CompilerHelpers.DebugEvent += LogMessage;
                            if (mSettings.EnableLogging)
                            {
                                var xLogFile = xOutputFilenameWithoutExtension + ".log.html";
                                if (!xScanner.EnableLogging(xLogFile))
                                {
                                    // file creation not possible
                                    mSettings.EnableLogging = false;
                                    LogWarning("Could not create the file \"" + xLogFile + "\"! No log will be created!");
                                }
                            }

                            if (UseGen3Kernel)
                            {
                                xScanner.Execute(mBootEntries.Keys.ToArray(), mForceIncludes);
                            }
                            else
                            {
                                xScanner.QueueMethod(xKernelCtor.DeclaringType.BaseType.GetMethod(UseGen3Kernel ? "EntryPoint" : "Start"));
                                xScanner.Execute(xKernelCtor);
                            }

                            //AppAssemblerRingsCheck.Execute(xScanner, xKernelCtor.DeclaringType.Assembly);

                            using (var xOut = new StreamWriter(File.Create(mSettings.OutputFilename), Encoding.ASCII, 128 * 1024))
                            {
                                //if (EmitDebugSymbols) {
                                xAsm.Assembler.FlushText(xOut);
                                xAsm.FinalizeDebugInfo();
                                //// for now: write debug info to console
                                //Console.WriteLine("Wrote {0} instructions and {1} datamembers", xAsm.Assembler.Instructions.Count, xAsm.Assembler.DataMembers.Count);
                                //var dict = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                                //foreach (var instr in xAsm.Assembler.Instructions)
                                //{
                                //    var mn = instr.Mnemonic ?? "";
                                //    if (dict.ContainsKey(mn))
                                //    {
                                //        dict[mn] = dict[mn] + 1;
                                //    }
                                //    else
                                //    {
                                //        dict[mn] = 1;
                                //    }
                                //}
                                //foreach (var entry in dict)
                                //{
                                //    Console.WriteLine("{0}|{1}", entry.Key, entry.Value);
                                //}
                            }
                        }
                        // If you want to uncomment this line make sure to enable PERSISTANCE_PROFILING symbol in
                        // DebugInfo.cs file.
                        //LogMessage(string.Format("DebugInfo flatening {0} seconds, persistance : {1} seconds",
                        //    (int)xDebugInfo.FlateningDuration.TotalSeconds,
                        //    (int)xDebugInfo.PersistanceDuration.TotalSeconds));
                    }
                }
                LogTime("Engine execute finished");
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex);
                LogMessage("Loaded assemblies: ");
                foreach (var xAsm in AssemblyLoadContext.Default.GetLoadedAssemblies())
                {
                    if (xAsm.IsDynamic)
                    {
                        continue;
                    }

                    try
                    {
                        LogMessage(xAsm.Location);
                    }
                    catch
                    {
                    }
                }
                return false;
            }
        }

        private AppAssembler GetAppAssembler()
        {
            return new AppAssembler(mSettings.DebugCom, Path.Combine(Path.GetDirectoryName(mSettings.OutputFilename), AssemblerLog));
        }

        private Assembly Default_Resolving(AssemblyLoadContext aContext, AssemblyName aName)
        {
            foreach (var xRef in mSettings.References)
            {
                var xName = AssemblyLoadContext.GetAssemblyName(xRef);
                if (xName.Name == aName.Name)
                {
                    return aContext.LoadFromAssemblyPath(xRef);
                }
            }

            foreach (var xRef in mSettings.References)
            {
                var xKernelAssemblyDir = Path.GetDirectoryName(xRef);
                var xAssemblyPath = Path.Combine(xKernelAssemblyDir, aName.Name);
                if (File.Exists(xAssemblyPath + ".dll"))
                {
                    return aContext.LoadFromAssemblyPath(xAssemblyPath + ".dll");
                }
                if (File.Exists(xAssemblyPath + ".exe"))
                {
                    return aContext.LoadFromAssemblyPath(xAssemblyPath + ".exe");
                }
            }

            // check for assembly in working directory
            var xPathToCheck = Path.Combine(Directory.GetCurrentDirectory(), aName.Name + ".dll");
            if (File.Exists(xPathToCheck))
            {
                return aContext.LoadFromAssemblyPath(xPathToCheck);
            }

            foreach (var xDir in mSettings.AssemblySearchDirs)
            {
                var xPath = Path.Combine(xDir, aName.Name + ".dll");
                if (File.Exists(xPath))
                {
                    return aContext.LoadFromAssemblyPath(xPath);
                }
                xPath = Path.Combine(xDir, aName.Name + ".exe");
                if (File.Exists(xPath))
                {
                    return aContext.LoadFromAssemblyPath(xPath);
                }
            }

            return null;
        }

        #region Gen2

        /// <summary>Load every refernced assemblies that have an associated FullPath property and seek for
        /// the kernel default constructor.</summary>
        /// <returns>The kernel default constructor or a null reference if either none or several such
        /// constructor could be found.</returns>
        private MethodBase LoadAssemblies()
        {
            // Try to load explicit path references.
            // These are the references of our boot project. We dont actually ever load the boot
            // project asm. Instead the references will contain plugs, and the kernel. We load
            // them then find the entry point in the kernel.
            //
            // Plugs and refs in this list will be loaded absolute (or as proj refs) only. Asm resolution
            // will not be tried on them, but will on ASMs they reference.

            string xKernelBaseName = "Cosmos.System.Kernel";
            LogMessage("Kernel Base: " + xKernelBaseName);

            Type xKernelType = null;
            foreach (string xRef in mSettings.References)
            {
                LogMessage("Checking Reference: " + xRef);
                if (File.Exists(xRef))
                {
                    LogMessage("  Exists");
                    var xAssembly = AssemblyLoadContext.Default.LoadFromAssemblyCacheOrPath(xRef);

                    CompilerHelpers.Debug($"Looking for kernel in {xAssembly}");

                    foreach (var xType in xAssembly.ExportedTypes)
                    {
                        if (!xType.IsGenericTypeDefinition && !xType.IsAbstract)
                        {
                            CompilerHelpers.Debug($"Checking type {xType.FullName}");

                            // We used to resolve with this:
                            //   if (xType.IsSubclassOf(typeof(Cosmos.System.Kernel))) {
                            // But this caused a single dependency on Cosmos.System which is bad.
                            // We could use an attribute, or maybe an interface would be better in this limited case. Interface
                            // will force user to implement what is needed if replacing our core. But in the end this is a "not needed" feature
                            // and would only complicate things.
                            // So for now at least, we look by name so we dont have a dependency since the method returns a MethodBase and not a Kernel instance anyway.
                            if (xType.BaseType.FullName == xKernelBaseName)
                            {
                                if (xKernelType != null)
                                {
                                    LogError($"Two kernels found: {xType.FullName} and {xKernelType.FullName}");
                                    return null;
                                }
                                xKernelType = xType;
                            }
                        }
                    }
                }
            }

            if (xKernelType == null)
            {
                LogError("No kernel found.");
                return null;
            }
            var xCtor = xKernelType.GetConstructor(Type.EmptyTypes);
            if (xCtor == null)
            {
                LogError("Kernel has no public parameterless constructor.");
                return null;
            }
            return xCtor;
        }

        #endregion

        private void LoadBootEntries()
        {
            mBootEntries = new Dictionary<MethodBase, int?>();
            mForceIncludes = new List<MemberInfo>();

            var xCheckedAssemblies = new List<string>();

            foreach (string xRef in mSettings.References)
            {
                LogMessage("Checking Reference: " + xRef);
                if (File.Exists(xRef))
                {
                    LogMessage("  Exists");
                    var xAssembly = AssemblyLoadContext.Default.LoadFromAssemblyCacheOrPath(xRef);
                    CheckAssembly(xAssembly);
                }
            }

            void CheckAssembly(Assembly aAssembly)
            {
                // Just for debugging
                //LogMessage("Checking Assembly: " + aAssembly.Location);

                xCheckedAssemblies.Add(aAssembly.GetName().ToString());

                foreach (var xType in aAssembly.GetTypes())
                {
                    var xForceIncludeAttribute = xType.GetCustomAttribute<ForceInclude>();

                    if (xForceIncludeAttribute != null)
                    {
                        ForceInclude(xType, xForceIncludeAttribute);
                    }

                    foreach (var xMethod in xType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        xForceIncludeAttribute = xMethod.GetCustomAttribute<ForceInclude>();

                        if (xForceIncludeAttribute != null)
                        {
                            ForceInclude(xMethod, xForceIncludeAttribute);
                        }

                        var xBootEntryAttribute = xMethod.GetCustomAttribute<BootEntry>();

                        if (xBootEntryAttribute != null)
                        {
                            var xEntryIndex = xBootEntryAttribute.EntryIndex;

                            LogMessage("Boot Entry found: Name: " + xMethod + ", Entry Index: "
                                + (xEntryIndex.HasValue ? xEntryIndex.Value.ToString() : "null"));

                            if (xMethod.ReturnType != typeof(void))
                            {
                                throw new NotSupportedException("Boot Entry should return void! Method: " + LabelName.Get(xMethod));
                            }

                            if (xMethod.GetParameters().Length != 0)
                            {
                                throw new NotSupportedException("Boot Entry shouldn't have parameters! Method: " + LabelName.Get(xMethod));
                            }

                            mBootEntries.Add(xMethod, xEntryIndex);
                        }
                    }

                    if (xType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                             .Where(m => m.GetCustomAttribute<BootEntry>() != null).Any())
                    {
                        throw new NotSupportedException("Boot Entry should be static! Type: " + LabelName.GetFullName(xType));
                    }
                }

                foreach (var xReference in aAssembly.GetReferencedAssemblies())
                {
                    try
                    {
                        if (!xCheckedAssemblies.Contains(xReference.ToString()))
                        {
                            var xAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(xReference);

                            if (xAssembly != null)
                            {
                                CheckAssembly(xAssembly);
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        if (xReference.Name.Contains("Cosmos"))
                        {
                            LogWarning("Cosmos Assembly not found!" + Environment.NewLine +
                                       "Assembly Name: " + xReference.FullName);
                        }
                    }
                }
            }

            if (mBootEntries.Count == 0)
            {
                throw new NotSupportedException("No boot entries found!");
            }

            if (mBootEntries.Where(e => e.Value == null).Count() == 0)
            {
                throw new NotImplementedException("No default boot entries found!");
            }

            mBootEntries = mBootEntries.OrderBy(e => e.Value)
                                       .OrderByDescending(e => e.Value.HasValue)
                                       .ToDictionary(e => e.Key, e => e.Value);

            if (mBootEntries.Count > 1)
            {
                var xLastEntryIndex = mBootEntries.Values.ElementAt(0);

                for (int i = 1; i < mBootEntries.Count; i++)
                {
                    var xEntryIndex = mBootEntries.Values.ElementAt(i);

                    if (xLastEntryIndex == xEntryIndex)
                    {
                        throw new NotSupportedException("Two boot entries with the same entry index were found! Methods: '" +
                                                        LabelName.GetFullName(mBootEntries.Keys.ElementAt(i - 1)) + "' and '" +
                                                        LabelName.GetFullName(mBootEntries.Keys.ElementAt(i)) + "'");
                    }

                    xLastEntryIndex = xEntryIndex;
                }
            }
        }

        private void ForceInclude(MemberInfo aMemberInfo, ForceInclude aForceIncludeAttribute)
        {
            if (aMemberInfo is Type xType)
            {
                mForceIncludes.Add(xType);

                foreach (var xMethod in xType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    mForceIncludes.Add(xMethod);
                }

                foreach (var xMethod in xType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!xMethod.IsSpecialName)
                    {
                        mForceIncludes.Add(xMethod);
                    }
                }
            }
            else if (aMemberInfo is MethodInfo xMethod)
            {
                mForceIncludes.Add(xMethod);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
