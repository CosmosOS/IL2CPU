//#define COSMOSDEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using IL2CPU.API;
using IL2CPU.API.Attribs;
using Cosmos.IL2CPU.Extensions;
using IL2CPU.Reflection;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU
{
    public class ScannerQueueItem
    {
        public MemberInfo Item { get; }
        public string QueueReason { get; }
        public string SourceItem { get; }

        public ScannerQueueItem(MemberInfo aMemberInfo, string aQueueReason, string aSourceItem)
        {
            Item = aMemberInfo;
            QueueReason = aQueueReason;
            SourceItem = aSourceItem;
        }

        public override string ToString()
        {
            return Item.GetType().Name + " " + Item.ToString();
        }
    }

    internal class ILScanner : IDisposable
    {
        public Action<Exception> LogException = null;
        public Action<string> LogWarning = null;

        protected ILVisitor mReader;
        protected AppAssembler mAsmblr;

        // List of asssemblies found during scan. We cannot use the list of loaded
        // assemblies because the loaded list includes compilers, etc, and also possibly
        // other unused assemblies. So instead we collect a list of assemblies as we scan.
        internal List<AssemblyInfo> mUsedAssemblies = new List<AssemblyInfo>();

        protected OurHashSet<MemberInfo> mItems = new OurHashSet<MemberInfo>();
        protected List<object> mItemsList = new List<object>();

        // Contains items to be scanned, both types and methods
        protected Queue<ScannerQueueItem> mQueue = new Queue<ScannerQueueItem>();

        // Virtual methods are nasty and constantly need to be rescanned for
        // overriding methods in new types, so we keep track of them separately.
        // They are also in the main mItems and mQueue.
        protected HashSet<MethodInfo> mVirtuals = new HashSet<MethodInfo>();

        protected IDictionary<MethodInfo, uint> mMethodUIDs = new Dictionary<MethodInfo, uint>();
        protected IDictionary<TypeInfo, uint> mTypeUIDs = new Dictionary<TypeInfo, uint>();

        protected PlugManager mPlugManager = null;

        // Logging
        // Only use for debugging and profiling.
        protected bool mLogEnabled = false;

        protected string mMapPathname;
        protected TextWriter mLogWriter;

        protected struct LogItem
        {
            public string SrcType;
            public object Item;
        }

        protected Dictionary<object, List<LogItem>> mLogMap;

        public ILScanner(AppAssembler aAsmblr, MetadataContext aMetadataContext)
        {
            mAsmblr = aAsmblr;
            mPlugManager = new PlugManager(LogException, LogWarning, aMetadataContext);
        }

        public bool EnableLogging(string aPathname)
        {
            mLogMap = new Dictionary<object, List<LogItem>>();
            mMapPathname = aPathname;
            mLogEnabled = true;

            // be sure that file could be written, to prevent exception on Dispose call, cause we could not make Task log in it
            try
            {
                File.CreateText(aPathname).Dispose();
            }
            catch
            {
                return false;
            }
            return true;
        }

        protected void Queue(MemberInfo aItem, object aSrc, string aSrcType, string sourceItem = null)
        {
            if (aItem == null)
            {
                throw new ArgumentNullException(nameof(aItem));
            }

            if (!mItems.Contains(aItem))
            {
                if (mLogEnabled)
                {
                    LogMapPoint(aSrc, aSrcType, aItem);
                }

                mItems.Add(aItem);
                mItemsList.Add(aItem);

                if (aSrc is MethodInfo xMethodInfoSrc)
                {
                    aSrc = xMethodInfoSrc.DeclaringType + "::" + aSrc;
                }

                mQueue.Enqueue(new ScannerQueueItem(aItem, aSrcType, aSrc + Environment.NewLine + sourceItem));
            }
        }

        #region Gen2

        public void Execute(MethodInfo aStartMethod, IEnumerable<AssemblyInfo> plugsAssemblies)
        {
            if (aStartMethod == null)
            {
                throw new ArgumentNullException(nameof(aStartMethod));
            }
            // TODO: Investigate using MS CCI
            // Need to check license, as well as in profiler
            // http://cciast.codeplex.com/

            #region Description

            // Methodology
            //
            // Ok - we've done the scanner enough times to know it needs to be
            // documented super well so that future changes won't inadvertently
            // break undocumented and unseen requirements.
            //
            // We've tried many approaches including recursive and additive scanning.
            // They typically end up being inefficient, overly complex, or both.
            //
            // -We would like to scan all types/methods so we can plug them.
            // -But we can't scan them until we plug them, because we will scan things
            // that plugs would remove/change the paths of.
            // -Plugs may also call methods which are also plugged.
            // -We cannot resolve plugs ahead of time but must do on the fly during
            // scanning.
            // -TODO: Because we do on the fly resolution, we need to add explicit
            // checking of plug classes and err when public methods are found that
            // do not resolve. Maybe we can make a list and mark, or rescan. Can be done
            // later or as an optional auditing step.
            //
            // This why in the past we had repetitive scans.
            //
            // Now we focus on more passes, but simpler execution. In the end it should
            // be eaiser to optmize and yield overall better performance. Most of the
            // passes should be low overhead versus an integrated system which often
            // would need to reiterate over items multiple times. So we do more loops on
            // with less repetitive analysis, instead of fewer loops but more repetition.
            //
            // -Locate all plug classes
            // -Scan from entry point collecting all types and methods while checking
            // for and following plugs
            // -For each type
            //    -Include all ancestors
            //    -Include all static constructors
            // -For each virtual method
            //    -Scan overloads in descendants until IsFinal, IsSealed or end
            //    -Scan base in ancestors until top or IsAbstract
            // -Go to scan types again, until no new ones found.
            // -Because the virtual method scanning will add to the list as it goes, maintain
            //  2 lists.
            //    -Known Types and Methods
            //    -Types and Methods in Queue - to be scanned
            // -Finally, do compilation

            #endregion Description

            mPlugManager.FindPlugImpls(plugsAssemblies);
            // Now that we found all plugs, scan them.
            // We have to scan them after we find all plugs, because
            // plugs can use other plugs
            mPlugManager.ScanFoundPlugs();
            foreach (var xPlug in mPlugManager.PlugImpls)
            {
                CompilerHelpers.Debug($"Plug found: '{xPlug.Key.FullName}' in '{xPlug.Key.Assembly.Identity.FullName}'");
            }

            ILOp.PlugManager = mPlugManager;

            // Pull in extra implementations, GC etc.
            Queue(RuntimeEngineRefs.InitializeApplicationRef, null, "Explicit Entry");
            Queue(RuntimeEngineRefs.FinalizeApplicationRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.IsInstanceRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetTypeInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetInterfaceInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetMethodInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetInterfaceMethodInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.GetMethodAddressForTypeRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.GetMethodAddressForInterfaceTypeRef, null, "Explicit Entry");
            Queue(GCImplementationRefs.IncRefCountRef, null, "Explicit Entry");
            Queue(GCImplementationRefs.DecRefCountRef, null, "Explicit Entry");
            Queue(GCImplementationRefs.AllocNewObjectRef, null, "Explicit Entry");
            // for now, to ease runtime exception throwing
            Queue(ExceptionHelperRefs.ThrowNotImplementedRef, null, "Explicit Entry");
            Queue(ExceptionHelperRefs.ThrowOverflowRef, null, "Explicit Entry");
            Queue(ExceptionHelperRefs.ThrowInvalidOperationRef, null, "Explicit Entry");
            Queue(ExceptionHelperRefs.ThrowArgumentOutOfRangeRef, null, "Explicit Entry");

            // register system types:
            Queue(TypeOf(BclType.Array), null, "Explicit Entry");
            Queue(TypeOf(BclType.Array).Methods.Single(m => m.IsConstructor && m.ParameterTypes.Count == 0), null, "Explicit Entry");
            Queue(TypeOf(typeof(MulticastDelegate)).Methods.Single(m => m.Name == "GetInvocationList"), null, "Explicit Entry");
            Queue(ExceptionHelperRefs.CurrentExceptionRef, null, "Explicit Entry");
            Queue(ExceptionHelperRefs.ThrowInvalidCastExceptionRef, null, "Explicit Entry");
            Queue(ExceptionHelperRefs.ThrowNotFiniteNumberExceptionRef, null, "Explicit Entry");
            Queue(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef, null, "Explicit Entry");

            mAsmblr.ProcessField(TypeOf(BclType.String).Fields.Single(f => f.Name == "Empty" && f.IsStatic));

            // Start from entry point of this program
            Queue(aStartMethod, null, "Entry Point");

            ScanQueue();
            UpdateAssemblies();
            Assemble();

            mAsmblr.EmitEntrypoint(aStartMethod);
        }

        #endregion Gen2

        #region Gen3

        public void Execute(
            IReadOnlyList<MethodInfo> aBootEntries,
            List<MemberInfo> aForceIncludes,
            IEnumerable<AssemblyInfo> plugsAssemblies)
        {
            foreach (var xBootEntry in aBootEntries)
            {
                Queue(xBootEntry.DeclaringType, null, "Boot Entry Declaring Type");
                Queue(xBootEntry, null, "Boot Entry");
            }

            foreach (var xForceInclude in aForceIncludes)
            {
                Queue(xForceInclude, null, "Force Include");
            }

            mPlugManager.FindPlugImpls(plugsAssemblies);
            // Now that we found all plugs, scan them.
            // We have to scan them after we find all plugs, because
            // plugs can use other plugs
            mPlugManager.ScanFoundPlugs();
            foreach (var xPlug in mPlugManager.PlugImpls)
            {
                CompilerHelpers.Debug($"Plug found: '{xPlug.Key.FullName}' in '{xPlug.Key.Assembly.Identity.FullName}'");
            }

            ILOp.PlugManager = mPlugManager;

            // Pull in extra implementations, GC etc.
            Queue(RuntimeEngineRefs.InitializeApplicationRef, null, "Explicit Entry");
            Queue(RuntimeEngineRefs.FinalizeApplicationRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetMethodInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.IsInstanceRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetTypeInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetInterfaceInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.SetInterfaceMethodInfoRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.GetMethodAddressForTypeRef, null, "Explicit Entry");
            Queue(VTablesImplRefs.GetMethodAddressForInterfaceTypeRef, null, "Explicit Entry");
            Queue(GCImplementationRefs.IncRefCountRef, null, "Explicit Entry");
            Queue(GCImplementationRefs.DecRefCountRef, null, "Explicit Entry");
            Queue(GCImplementationRefs.AllocNewObjectRef, null, "Explicit Entry");
            // Pull in Array constructor
            Queue(TypeOf(BclType.Array).Methods.Single(m => m.IsConstructor && m.ParameterTypes.Count == 0), null, "Explicit Entry");
            // Pull in MulticastDelegate.GetInvocationList, needed by the Invoke plug
            Queue(TypeOf(typeof(MulticastDelegate)).Methods.Single(m => m.Name == "GetInvocationList"), null, "Explicit Entry");

            mAsmblr.ProcessField(TypeOf(BclType.String).Fields.Single(f => f.Name == "Empty" && f.IsStatic));

            ScanQueue();
            UpdateAssemblies();
            Assemble();

            mAsmblr.EmitEntrypoint(null, aBootEntries);
        }

        #endregion Gen3

        public void QueueMethod(MethodInfo method)
        {
            Queue(method, null, "Explicit entry via QueueMethod");
        }

        /// This method changes the opcodes. Changes are:
        /// * inserting the ValueUID for method ops.
        private void ProcessInstructions(IReadOnlyList<ILOpCode> aOpCodes)
        {
            foreach (var xOpCode in aOpCodes)
            {
                if (xOpCode is ILOpCodes.OpMethod xOpMethod)
                {
                    xOpMethod.Value = (MethodInfo)mItems.GetItemInList(xOpMethod.Value);
                    xOpMethod.ValueUID = GetMethodUID(xOpMethod.Value);
                }
            }
        }

        public void Dispose()
        {
            if (mLogEnabled)
            {
                // Create bookmarks, but also a dictionary that
                // we can find the items in
                var xBookmarks = new Dictionary<object, int>();
                int xBookmark = 0;
                foreach (var xList in mLogMap)
                {
                    foreach (var xItem in xList.Value)
                    {
                        xBookmarks.Add(xItem.Item, xBookmark);
                        xBookmark++;
                    }
                }

                using (mLogWriter = new StreamWriter(File.OpenWrite(mMapPathname)))
                {
                    mLogWriter.WriteLine("<html><body>");
                    foreach (var xList in mLogMap)
                    {
                        var xLogItemText = LogItemText(xList.Key);

                        mLogWriter.WriteLine("<hr>");

                        // Emit bookmarks above source, so when clicking links user doesn't need
                        // to constantly scroll up.
                        foreach (var xItem in xList.Value)
                        {
                            mLogWriter.WriteLine("<a name=\"Item" + xBookmarks[xItem.Item].ToString() + "_S\"></a>");
                        }

                        if (!xBookmarks.TryGetValue(xList.Key, out var xHref))
                        {
                            xHref = -1;
                        }
                        mLogWriter.Write("<p>");
                        if (xHref >= 0)
                        {
                            mLogWriter.WriteLine("<a href=\"#Item" + xHref.ToString() + "_S\">");
                            mLogWriter.WriteLine("<a name=\"Item{0}\">", xHref);
                        }
                        if (xList.Key == null)
                        {
                            mLogWriter.WriteLine("Unspecified Source");
                        }
                        else
                        {
                            mLogWriter.WriteLine(xLogItemText);
                        }
                        if (xHref >= 0)
                        {
                            mLogWriter.Write("</a>");
                            mLogWriter.Write("</a>");
                        }
                        mLogWriter.WriteLine("</p>");

                        mLogWriter.WriteLine("<ul>");
                        foreach (var xItem in xList.Value)
                        {
                            mLogWriter.Write("<li><a href=\"#Item{1}\">{0}</a></li>", LogItemText(xItem.Item), xBookmarks[xItem.Item]);

                            mLogWriter.WriteLine("<ul>");
                            mLogWriter.WriteLine("<li>" + xItem.SrcType + "</li>");
                            mLogWriter.WriteLine("</ul>");
                        }
                        mLogWriter.WriteLine("</ul>");
                    }
                    mLogWriter.WriteLine("</body></html>");
                }
            }
        }

        public int MethodCount => mMethodUIDs.Count;

        protected string LogItemText(object aItem)
        {
            if (aItem is MethodInfo xMethod)
            {
                return "Method: " + xMethod.DeclaringType + "." + xMethod.Name + "<br>" + xMethod.GetFullName();
            }
            if (aItem is TypeInfo xType)
            {
                return "Type: " + xType.FullName;
            }
            return "Other: " + aItem;
        }

        protected void ScanMethod(MethodInfo aMethod, bool aIsPlug, string sourceItem)
        {
            CompilerHelpers.Debug($"ILScanner: ScanMethod");
            CompilerHelpers.Debug($"Method = '{aMethod}'");
            CompilerHelpers.Debug($"IsPlug = '{aIsPlug}'");
            CompilerHelpers.Debug($"Source = '{sourceItem}'");

            var xParams = aMethod.ParameterTypes;
            // Dont use foreach, enum generaly keeps order but
            // isn't guaranteed.
            //string xMethodFullName = LabelName.GetFullName(aMethod);

            for (int i = 0; i < xParams.Count; i++)
            {
                Queue(xParams[i], aMethod, "Parameter");
            }
            // Queue Types directly related to method
            if (!aIsPlug)
            {
                // Don't queue declaring types of plugs
                Queue(aMethod.DeclaringType, aMethod, "Declaring Type");
            }

            Queue((aMethod).ReturnType, aMethod, "Return Type");

            // Scan virtuals

            #region Virtuals scan

            if (aMethod.IsVirtual)
            {
                // For virtuals we need to climb up the type tree
                // and find the top base method. We then add that top
                // node to the mVirtuals list. We don't need to add the
                // types becuase adding DeclaringType will already cause
                // all ancestor types to be added.

                var xVirtMethod = aMethod;
                var xVirtType = aMethod.DeclaringType;
                MethodInfo xNewVirtMethod;
                while (true)
                {
                    xVirtType = xVirtType.BaseType;
                    if (xVirtType == null)
                    {
                        // We've reached object, can't go farther
                        xNewVirtMethod = null;
                    }
                    else
                    {
                        xNewVirtMethod = xVirtType.Methods
                                                  .Where(method => !method.IsStatic && method.Matches(aMethod))
                                                  .SingleOrDefault();
                        if (xNewVirtMethod != null)
                        {
                            if (!xNewVirtMethod.IsVirtual)
                            {
                                // This can happen if a virtual "replaces" a non virtual
                                // above it that is not virtual.
                                xNewVirtMethod = null;
                            }
                        }
                    }
                    // We dont bother to add these to Queue, because we have to do a
                    // full downlevel scan if its a new base virtual anyways.
                    if (xNewVirtMethod == null)
                    {
                        // If its already in the list, we mark it null
                        // so we dont do a full downlevel scan.
                        if (mVirtuals.Contains(xVirtMethod))
                        {
                            xVirtMethod = null;
                        }
                        break;
                    }
                    xVirtMethod = xNewVirtMethod;
                }

                // New virtual base found, we need to downscan it
                // If it was already in mVirtuals, then ScanType will take
                // care of new additions.
                if (xVirtMethod != null)
                {
                    Queue(xVirtMethod, aMethod, "Virtual Base");
                    mVirtuals.Add(xVirtMethod);

                    // List changes as we go, cant be foreach
                    for (int i = 0; i < mItemsList.Count; i++)
                    {
                        if (mItemsList[i] is TypeInfo xType && xType != xVirtMethod.DeclaringType && !xType.IsInterface)
                        {
                            if (xType.IsSubclassOf(xVirtMethod.DeclaringType))
                            {
                                var xNewMethod = xType.Methods
                                                      .Where(method => !method.IsStatic && method.Matches(aMethod))
                                                      .SingleOrDefault();
                                if (xNewMethod != null)
                                {
                                    // We need to check IsVirtual, a non virtual could
                                    // "replace" a virtual above it?
                                    if (xNewMethod.IsVirtual)
                                    {
                                        Queue(xNewMethod, aMethod, "Virtual Downscan");
                                    }
                                }
                            }
                            else if (xVirtMethod.DeclaringType.IsInterface
                                  && xType.ImplementsInterface(xVirtMethod.DeclaringType))
                            {
                                var xInterfaceMap = xType.GetInterfaceMapping(xVirtMethod.DeclaringType);
                                var xTargetMethod = xInterfaceMap.SingleOrDefault(
                                    m => m.InterfaceMethod == xVirtMethod).TargetMethod;

                                if (xTargetMethod != null)
                                {
                                    Queue(xTargetMethod, aMethod, "Virtual Downscan");
                                }
                            }
                        }
                    }
                }
            }

            #endregion Virtuals scan

            MethodInfo xPlug = null;
            // Plugs may use plugs, but plugs won't be plugged over themself
            var inl = aMethod.GetCustomAttribute<InlineAttribute>();
            if (!aIsPlug)
            {
                // Check to see if method is plugged, if it is we don't scan body

                xPlug = mPlugManager.ResolvePlug(aMethod, xParams);
                if (xPlug != null)
                {
                    //ScanMethod(xPlug, true, "Plug method");
                    if (inl == null)
                    {
                        Queue(xPlug, aMethod, "Plug method");
                    }
                }
            }

            if (xPlug == null)
            {
                bool xNeedsPlug = false;
                if (aMethod.IsPinvokeImpl)
                {
                    // pinvoke methods dont have an embedded implementation
                    xNeedsPlug = true;
                }
                else
                {
                    // todo: prob even more
                    if (aMethod.IsImplNative || aMethod.IsImplInternalCall)
                    {
                        // native implementations cannot be compiled
                        xNeedsPlug = true;
                    }
                }
                if (xNeedsPlug)
                {
                    throw new Exception(Environment.NewLine
                        + "Native code encountered, plug required." + Environment.NewLine
                                        + "  DO NOT REPORT THIS AS A BUG." + Environment.NewLine
                                        + "  Please see http://www.gocosmos.org/docs/plugs/missing/" + Environment.NewLine
                        + "  Need plug for: " + LabelName.GetFullName(aMethod) + "." + Environment.NewLine
                        + "  Called from :" + Environment.NewLine + sourceItem + Environment.NewLine);
                }

                //TODO: As we scan each method, we could update or put in a new list
                // that has the resolved plug so we don't have to reresolve it again
                // later for compilation.

                // Scan the method body for more type and method refs
                //TODO: Dont queue new items if they are plugged
                // or do we need to queue them with a resolved ref in a new list?

                if (inl != null)
                {
                    return; // cancel inline
                }

                var xOpCodes = ProcessMethod(aMethod);
                if (xOpCodes != null)
                {
                    ProcessInstructions(xOpCodes);
                    foreach (var xOpCode in xOpCodes)
                    {
                        if (xOpCode is ILOpCodes.OpMethod xOpMethod)
                        {
                            Queue(xOpMethod.Value, aMethod, "Call", sourceItem);
                        }
                        else if (xOpCode is ILOpCodes.OpType xOpType)
                        {
                            Queue(xOpType.Value, aMethod, "OpCode Value");
                        }
                        else if (xOpCode is ILOpCodes.OpField xOpField)
                        {
                            //TODO: Need to do this? Will we get a ILOpCodes.OpType as well?
                            Queue(xOpField.Value.DeclaringType, aMethod, "OpCode Value");
                            if (xOpField.Value.IsStatic)
                            {
                                //TODO: Why do we add static fields, but not instance?
                                // AW: instance fields are "added" always, as part of a type, but for static fields, we need to emit a datamember
                                Queue(xOpField.Value, aMethod, "OpCode Value");
                            }
                        }
                        else if (xOpCode is ILOpCodes.OpToken xOpToken)
                        {
                            if (xOpToken.ValueIsType)
                            {
                                Queue(xOpToken.ValueType, aMethod, "OpCode Value");
                            }
                            if (xOpToken.ValueIsField)
                            {
                                Queue(xOpToken.ValueField.DeclaringType, aMethod, "OpCode Value");
                                if (xOpToken.ValueField.IsStatic)
                                {
                                    //TODO: Why do we add static fields, but not instance?
                                    // AW: instance fields are "added" always, as part of a type, but for static fields, we need to emit a datamember
                                    Queue(xOpToken.ValueField, aMethod, "OpCode Value");
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void ScanType(TypeInfo aType)
        {
            CompilerHelpers.Debug($"ILScanner: ScanType");
            CompilerHelpers.Debug($"Type = '{aType}'");

            // Add immediate ancestor type
            // We dont need to crawl up farther, when the BaseType is scanned
            // it will add its BaseType, and so on.
            if (aType.BaseType != null)
            {
                Queue(aType.BaseType, aType, "Base Type");
            }
            // Queue static ctors
            // We always need static ctors, else the type cannot
            // be created.
            var xCctor = aType.GetTypeInitializer();
            if (xCctor != null)
            {
                Queue(xCctor, aType, "Static Constructor");
            }

            // For each new type, we need to scan for possible new virtuals
            // in our new type if its a descendant of something in
            // mVirtuals.
            foreach (var xVirt in mVirtuals)
            {
                // See if our new type is a subclass of any virt's DeclaringTypes
                // If so our new type might have some virtuals
                if (aType.IsSubclassOf(xVirt.DeclaringType))
                {
                    var xParams = xVirt.ParameterTypes;
                    var xMethod = aType.GetMethod(xVirt.Name, xParams, m => !m.IsStatic);
                    if (xMethod != null)
                    {
                        // We need to check IsVirtual, a non virtual could
                        // "replace" a virtual above it?
                        if (xMethod.IsVirtual)
                        {
                            Queue(xMethod, aType, "Virtual");
                        }
                    }
                }
                //if (!aType.IsGenericParameter && xVirt.DeclaringType.IsInterface)
                if (xVirt.DeclaringType.IsInterface)
                {
                    if (!aType.IsInterface && aType.ImplementsInterface(xVirt.DeclaringType))
                    {
                        var xIntfMapping = aType.GetInterfaceMapping(xVirt.DeclaringType);
                        var xTargetMethod = xIntfMapping.SingleOrDefault(m => m.InterfaceMethod == xVirt).TargetMethod;

                        if (xTargetMethod != null)
                        {
                            Queue(xTargetMethod, aType, "Virtual");
                        }
                    }
                }
            }

            foreach (var xInterface in aType.ExplicitImplementedInterfaces)
            {
                Queue(xInterface, aType, "Implemented Interface");
            }
        }

        protected void ScanQueue()
        {
            while (mQueue.Count > 0)
            {
                var xItem = mQueue.Dequeue();
                CompilerHelpers.Debug($"ILScanner: ScanQueue - '{xItem}'");
                // Check for MethodInfo first, they are more numerous
                // and will reduce compares
                if (xItem.Item is MethodInfo xMethod)
                {
                    ScanMethod(xMethod, false, xItem.SourceItem);
                }
                else if (xItem.Item is TypeInfo xType)
                {
                    ScanType(xType);

                    // Methods and fields cant exist without types, so we only update
                    // mUsedAssemblies in type branch.
                    if (!mUsedAssemblies.Contains(xType.Assembly))
                    {
                        mUsedAssemblies.Add(xType.Assembly);
                    }
                }
                else if (xItem.Item is FieldInfo)
                {
                    // todo: static fields need more processing?
                }
                else
                {
                    throw new Exception("Unknown item found in queue.");
                }
            }
        }

        protected void LogMapPoint(object aSrc, string aSrcType, object aItem)
        {
            // Keys cant be null. If null, we just say ILScanner is the source
            if (aSrc == null)
            {
                aSrc = typeof(ILScanner);
            }

            var xLogItem = new LogItem
            {
                SrcType = aSrcType,
                Item = aItem
            };
            if (!mLogMap.TryGetValue(aSrc, out var xList))
            {
                xList = new List<LogItem>();
                mLogMap.Add(aSrc, xList);
            }
            xList.Add(xLogItem);
        }

        private MethodInfo GetUltimateBaseMethod(MethodInfo aMethod)
        {
            var xBaseMethod = aMethod;

            while (true)
            {
                var xBaseDefinition = xBaseMethod.GetBaseDefinition();

                if (xBaseDefinition == xBaseMethod)
                {
                    return xBaseMethod;
                }

                xBaseMethod = xBaseDefinition;
            }
        }

        protected uint GetMethodUID(MethodInfo aMethod)
        {
            if (mMethodUIDs.TryGetValue(aMethod, out var xMethodUID))
            {
                return xMethodUID;
            }
            else
            {
                if (!aMethod.DeclaringType.IsInterface)
                {
                    var xBaseMethod = GetUltimateBaseMethod(aMethod);

                    if (!mMethodUIDs.TryGetValue(xBaseMethod, out xMethodUID))
                    {
                        xMethodUID = (uint)mMethodUIDs.Count;
                        mMethodUIDs.Add(xBaseMethod, xMethodUID);
                    }

                    if (aMethod != xBaseMethod)
                    {
                        mMethodUIDs.Add(aMethod, xMethodUID);
                    }

                    return xMethodUID;
                }

                xMethodUID = (uint)mMethodUIDs.Count;
                mMethodUIDs.Add(aMethod, xMethodUID);

                return xMethodUID;
            }
        }

        protected uint GetTypeUID(TypeInfo aType)
        {
            if (!mItems.Contains(aType))
            {
                throw new Exception("Cannot get UID of types which are not queued!");
            }
            if (!mTypeUIDs.ContainsKey(aType))
            {
                var xId = (uint)mTypeUIDs.Count;
                mTypeUIDs.Add(aType, xId);
                return xId;
            }
            return mTypeUIDs[aType];
        }

        protected void UpdateAssemblies()
        {
            // It would be nice to keep DebugInfo output into assembler only but
            // there is so much info that is available in scanner that is needed
            // or can be used in a more efficient manner. So we output in both
            // scanner and assembler as needed.
            mAsmblr.DebugInfo.AddAssemblies(mUsedAssemblies);
        }

        protected void Assemble()
        {
            foreach (var xItem in mItems)
            {
                if (xItem is MethodInfo xMethod)
                {
                    var xParams = xMethod.Parameters;
                    var xParamTypes = xMethod.ParameterTypes;
                    var xPlug = mPlugManager.ResolvePlug(xMethod, xParamTypes);
                    var xMethodType = _MethodInfo.TypeEnum.Normal;
                    Type xPlugAssembler = null;
                    _MethodInfo xPlugInfo = null;
                    var xMethodInline = xMethod.GetCustomAttribute<InlineAttribute>();
                    if (xMethodInline != null)
                    {
                        // inline assembler, shouldn't come here..
                        continue;
                    }
                    var xMethodIdMethod = mItemsList.IndexOf(xMethod);
                    if (xMethodIdMethod == -1)
                    {
                        throw new Exception("Method not in scanner list!");
                    }
                    PlugMethod xPlugAttrib = null;
                    if (xPlug != null)
                    {
                        xMethodType = _MethodInfo.TypeEnum.NeedsPlug;
                        xPlugAttrib = xPlug.GetCustomAttribute<PlugMethod>();
                        var xInlineAttrib = xPlug.GetCustomAttribute<InlineAttribute>();
                        var xMethodIdPlug = mItemsList.IndexOf(xPlug);
                        if ((xMethodIdPlug == -1) && (xInlineAttrib == null))
                        {
                            throw new Exception("Plug method not in scanner list!");
                        }
                        if ((xPlugAttrib != null) && (xInlineAttrib == null))
                        {
                            xPlugAssembler = xPlugAttrib.Assembler;
                            xPlugInfo = new _MethodInfo(xPlug, (uint)xMethodIdPlug, _MethodInfo.TypeEnum.Plug, null, xPlugAssembler);

                            var xMethodInfo = new _MethodInfo(xMethod, (uint)xMethodIdMethod, xMethodType, xPlugInfo);
                            if (xPlugAttrib.IsWildcard)
                            {
                                xPlugInfo.IsWildcard = true;
                                xPlugInfo.PluggedMethod = xMethodInfo;
                                var xInstructions = ProcessMethod(xPlug);
                                if (xInstructions != null)
                                {
                                    ProcessInstructions(xInstructions);
                                    mAsmblr.ProcessMethod(xPlugInfo, xInstructions);
                                }
                            }
                            mAsmblr.GenerateMethodForward(xMethodInfo, xPlugInfo);
                        }
                        else
                        {
                            if (xInlineAttrib != null)
                            {
                                var xMethodID = mItemsList.IndexOf(xItem);
                                if (xMethodID == -1)
                                {
                                    throw new Exception("Method not in list!");
                                }
                                xPlugInfo = new _MethodInfo(xPlug, (uint)xMethodID, _MethodInfo.TypeEnum.Plug, null, true);

                                var xMethodInfo = new _MethodInfo(xMethod, (uint)xMethodIdMethod, xMethodType, xPlugInfo);

                                xPlugInfo.PluggedMethod = xMethodInfo;
                                var xInstructions = ProcessMethod(xPlug);
                                if (xInstructions != null)
                                {
                                    ProcessInstructions(xInstructions);
                                    mAsmblr.ProcessMethod(xPlugInfo, xInstructions);
                                }
                                mAsmblr.GenerateMethodForward(xMethodInfo, xPlugInfo);
                            }
                            else
                            {
                                xPlugInfo = new _MethodInfo(xPlug, (uint)xMethodIdPlug, _MethodInfo.TypeEnum.Plug, null, xPlugAssembler);

                                var xMethodInfo = new _MethodInfo(xMethod, (uint)xMethodIdMethod, xMethodType, xPlugInfo);
                                mAsmblr.GenerateMethodForward(xMethodInfo, xPlugInfo);
                            }
                        }
                    }
                    else
                    {
                        xPlugAttrib = xMethod.GetCustomAttribute<PlugMethod>();

                        if (xPlugAttrib != null)
                        {
                            if (xPlugAttrib.IsWildcard)
                            {
                                continue;
                            }
                            if (xPlugAttrib.PlugRequired)
                            {
                                throw new Exception(String.Format("Method {0} requires a plug, but none is implemented", xMethod.Name));
                            }
                            xPlugAssembler = xPlugAttrib.Assembler;
                        }

                        var xMethodInfo = new _MethodInfo(xMethod, (uint)xMethodIdMethod, xMethodType, xPlugInfo, xPlugAssembler);
                        var xInstructions = ProcessMethod(xMethod);
                        if (xInstructions != null)
                        {
                            ProcessInstructions(xInstructions);
                            mAsmblr.ProcessMethod(xMethodInfo, xInstructions);
                        }
                    }
                }
                else if (xItem is FieldInfo)
                {
                    mAsmblr.ProcessField((FieldInfo)xItem);
                }
            }

            var xTypes = new HashSet<TypeInfo>();
            var xMethods = new HashSet<MethodInfo>();

            foreach (var xItem in mItems)
            {
                if (xItem is MethodInfo)
                {
                    xMethods.Add((MethodInfo)xItem);
                }
                else if (xItem is TypeInfo)
                {
                    xTypes.Add((TypeInfo)xItem);
                }
            }

            mAsmblr.GenerateVMTCode(xTypes, xMethods, GetTypeUID, GetMethodUID);
        }

        private static IReadOnlyList<ILOpCode> ProcessMethod(MethodInfo aMethodInfo)
        {
            if (aMethodInfo.DeclaringType.FullName == "System.Runtime.CompilerServices.Unsafe")
            {
                var type = CompilerEngine.MetadataContext.ResolveTypeByName(
                    "System.Runtime.CompilerServices.Unsafe, System.Runtime.CompilerServices");

                aMethodInfo = type.GetMethod(aMethodInfo.Name, aMethodInfo.ParameterTypes);
            }

            if (aMethodInfo.MethodBody == null)
            {
                return null;
            }

            var ilReader = aMethodInfo.MethodBody.GetILReader();
            var visitor = new ILVisitor(aMethodInfo);

            ilReader.ReadIL(visitor);

            return visitor.ILOpCodes;
        }
    }
}
