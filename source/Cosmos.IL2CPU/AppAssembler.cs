//#define VMT_DEBUG
//#define COSMOSDEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
#if VMT_DEBUG
using System.Xml;
#endif

using Cosmos.Build.Common;

using IL2CPU.API;
using IL2CPU.API.Attribs;
using IL2CPU.Debug.Symbols;
using Cosmos.IL2CPU.Extensions;
using Cosmos.IL2CPU.ILOpCodes;
using Cosmos.IL2CPU.Interpret;
using Cosmos.IL2CPU.X86.IL;

using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;
using Label = XSharp.Assembler.Label;
using Cosmos.IL2CPU.MethodAnalysis;
using IL2CPU.Reflection;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU
{
    internal sealed class AppAssembler : IDisposable
    {
        private static readonly ZooLoadContext loadCtx = new ZooLoadContext();

        public const string EndOfMethodLabelNameNormal = ".END__OF__METHOD_NORMAL";
        public const string EndOfMethodLabelNameException = ".END__OF__METHOD_EXCEPTION";
        private const string InitStringIDsLabel = "___INIT__STRINGS_TYPE_ID_S___";
        private List<LOCAL_ARGUMENT_INFO> mLocals_Arguments_Infos = new List<LOCAL_ARGUMENT_INFO>();
        private ILOp[] mILOpsLo = new ILOp[256];
        private ILOp[] mILOpsHi = new ILOp[256];
        public bool ShouldOptimize = false;
        public DebugInfo DebugInfo { get; set; }
        private TextWriter mLog;
        private Dictionary<string, ModuleDefinition> mLoadedModules = new Dictionary<string, ModuleDefinition>();
        public TraceAssemblies TraceAssemblies;
        public bool DebugEnabled = false;
        public bool StackCorruptionDetection = false;
        public StackCorruptionDetectionLevel StackCorruptionDetectionLevel;
        public DebugMode DebugMode;
        public bool IgnoreDebugStubAttribute;
        private List<MethodIlOp> mSymbols = new List<MethodIlOp>();
        private List<INT3Label> mINT3Labels = new List<INT3Label>();
        public readonly CosmosAssembler Assembler;

        public AppAssembler(CosmosAssembler aAssembler, TextWriter aLog)
        {
            Assembler = aAssembler;
            mLog = aLog;
            InitILOps();
        }

        public void Dispose()
        {
            if (mLog != null)
            {
                mLog.Dispose();
                mLog = null;
            }
            GC.SuppressFinalize(this);
        }

        private void MethodBegin(_MethodInfo aMethod)
        {
            XS.Comment("---------------------------------------------------------");
            XS.Comment("Assembly: " + aMethod.MethodBase.DeclaringType.Assembly.FullName);
            XS.Comment("Type: " + aMethod.MethodBase.DeclaringType);
            XS.Comment("Name: " + aMethod.MethodBase.Name);
            XS.Comment("Plugged: " + (aMethod.PlugMethod == null ? "No" : "Yes"));

            #region Document locals, arguments and return value
            if (aMethod.MethodAssembler == null && !aMethod.IsInlineAssembler)
            {
                // the body of aMethod is getting emitted
                var xLocals = aMethod.MethodBase.GetLocalVariables() ?? new List<LocalVariableInfo>();
                for (int i = 0; i < xLocals.Count; i++)
                {
                    XS.Comment(String.Format("Local {0} at EBP-{1}", i, ILOp.GetEBPOffsetForLocal(aMethod, i)));
                }

                var xIdxOffset = 0u;
                if (!aMethod.MethodBase.IsStatic)
                {
                    XS.Comment(String.Format("Argument[0] $this at EBP+{0}, size = {1}", X86.IL.Ldarg.GetArgumentDisplacement(aMethod, 0), ILOp.Align(ILOp.SizeOfType(aMethod.MethodBase.DeclaringType), 4)));
                    xIdxOffset++;
                }

                string x = aMethod.MethodBase.Name;
                string y = aMethod.MethodBase.DeclaringType.Name;
                var xParams = aMethod.MethodBase.GetParameters();
                var xParamCount = (ushort)xParams.Length;

                for (ushort i = 0; i < xParamCount; i++)
                {
                    var xOffset = X86.IL.Ldarg.GetArgumentDisplacement(aMethod, (ushort)(i + xIdxOffset));
                    var xSize = ILOp.SizeOfType(xParams[i].ParameterType);
                    // if last argument is 8 byte long, we need to add 4, so that debugger could read all 8 bytes from this variable in positiv direction
                    XS.Comment(String.Format("Argument[{3}] {0} at EBP+{1}, size = {2}", xParams[i].Name, xOffset, xSize, (xIdxOffset + i)));
                }

                var xMethodInfo = aMethod.MethodBase as MethodInfo;
                if (xMethodInfo != null)
                {
                    var xSize = ILOp.Align(ILOp.SizeOfType(xMethodInfo.ReturnType), 4);
                    XS.Comment(String.Format("Return size: {0}", xSize));
                }
            }
            #endregion

            // Issue label that is used for calls etc.
            string xMethodLabel;
            if (aMethod.PluggedMethod != null)
            {
                xMethodLabel = "PLUG_FOR___" + LabelName.Get(aMethod.PluggedMethod.MethodBase);
            }
            else
            {
                xMethodLabel = LabelName.Get(aMethod.MethodBase);
            }
            XS.Label(xMethodLabel);

            // Alternative asm labels for the method
            var xAsmLabelAttributes = aMethod.MethodBase.FetchCustomAttributes<AsmLabel>();
            foreach (var xAttribute in xAsmLabelAttributes)
            {
                XS.Label(xAttribute.Label);
            }

            //Assembler.WriteDebugVideo("Method " + aMethod.UID);

            // We could use same GUID as MethodLabelStart, but its better to keep GUIDs unique globaly for items
            // so during debugging they can never be confused as to what they point to.
            aMethod.DebugMethodUID = DebugInfo.CreateId;

            // We issue a second label for GUID. This is increases label count, but for now we need a master label first.
            // We issue a GUID label to reduce amount of work and time needed to construct debugging DB.
            aMethod.DebugMethodLabelUID = DebugInfo.CreateId;
            XS.Label("GUID_" + aMethod.DebugMethodLabelUID.ToString());

            Label.LastFullLabel = "METHOD_" + aMethod.DebugMethodLabelUID.ToString();

            if (DebugEnabled && StackCorruptionDetection)
            {
                // if StackCorruption detection is active, we're also going to emit a stack overflow detection
                XS.Set(EAX, "Before_Kernel_Stack");
                XS.Compare(EAX, ESP);
                XS.Jump(ConditionalTestEnum.LessThan, ".StackOverflowCheck_End");
                XS.ClearInterruptFlag();
                // don't remove the call. It seems pointless, but we need it to retrieve the EIP value
                XS.Call(".StackOverflowCheck_GetAddress");
                XS.Label(".StackOverflowCheck_GetAddress");
                XS.Exchange(BX, BX);
                XS.Pop(EAX);
                XS.Set(AsmMarker.Labels[AsmMarker.Type.DebugStub_CallerEIP], EAX, destinationIsIndirect: true);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendStackOverflowEvent]);
                XS.Halt();
                XS.Label(".StackOverflowCheck_End");
            }

            aMethod.EndMethodID = DebugInfo.CreateId;

            if (aMethod.MethodBase.IsStatic && aMethod.MethodBase is ConstructorInfo)
            {
                XS.Comment("Static constructor. See if it has been called already, return if so.");
                var xName = DataMember.FilterStringForIncorrectChars("CCTOR_CALLED__" + LabelName.GetFullName(aMethod.MethodBase.DeclaringType));
                XS.DataMember(xName, 1, "db", "0");
                XS.Compare(xName, 1, destinationIsIndirect: true, size: RegisterSize.Byte8);
                XS.Jump(ConditionalTestEnum.Equal, ".BeforeQuickReturn");
                XS.Set(xName, 1, destinationIsIndirect: true, size: RegisterSize.Byte8);
                XS.Jump(".AfterCCTorAlreadyCalledCheck");
                XS.Label(".BeforeQuickReturn");
                XS.Set(ECX, 0);
                XS.Return();
                XS.Label(".AfterCCTorAlreadyCalledCheck");
            }

            XS.Push(EBP);
            XS.Set(EBP, ESP);

            if (aMethod.MethodAssembler == null && aMethod.PlugMethod == null && !aMethod.IsInlineAssembler)
            {
                // the body of aMethod is getting emitted
                aMethod.LocalVariablesSize = 0;
                var xLocals = aMethod.MethodBase.GetLocalVariables();
                for (int i = 0; i < xLocals.Count; i++)
                {
                    {
                        var xInfo = new LOCAL_ARGUMENT_INFO
                        {
                            METHODLABELNAME = xMethodLabel,
                            IsArgument = false,
                            INDEXINMETHOD = xLocals[i].LocalIndex,
                            NAME = "Local" + xLocals[i].LocalIndex,
                            OFFSET = 0 - (int)ILOp.GetEBPOffsetForLocalForDebugger(aMethod, i),
                            TYPENAME = xLocals[i].LocalType.FullName
                        };
                        mLocals_Arguments_Infos.Add(xInfo);

                        var xSize = ILOp.Align(ILOp.SizeOfType(xLocals[i].LocalType), 4);
                        XS.Comment(String.Format("Local {0}, Size {1}", i, xSize));
                        for (int j = 0; j < xSize / 4; j++) //TODO: Can this be done shorter?
                        {
                            XS.Push(0);
                        }
                        aMethod.LocalVariablesSize += xSize;
                    }
                }

                // debug info:
                var xIdxOffset = 0u;
                if (!aMethod.MethodBase.IsStatic)
                {
                    mLocals_Arguments_Infos.Add(new LOCAL_ARGUMENT_INFO
                    {
                        METHODLABELNAME = xMethodLabel,
                        IsArgument = true,
                        NAME = "this:" + X86.IL.Ldarg.GetArgumentDisplacement(aMethod, 0),
                        INDEXINMETHOD = 0,
                        OFFSET = X86.IL.Ldarg.GetArgumentDisplacement(aMethod, 0),
                        TYPENAME = aMethod.MethodBase.DeclaringType.FullName
                    });

                    xIdxOffset++;
                }

                var xParams = aMethod.MethodBase.GetParameters();
                var xParamCount = (ushort)xParams.Length;

                for (ushort i = 0; i < xParamCount; i++)
                {
                    var xOffset = X86.IL.Ldarg.GetArgumentDisplacement(aMethod, (ushort)(i + xIdxOffset));
                    // if last argument is 8 byte long, we need to add 4, so that debugger could read all 8 bytes from this variable in positiv direction
                    xOffset -= (int)ILOp.Align(ILOp.SizeOfType(xParams[i].ParameterType), 4) - 4;
                    mLocals_Arguments_Infos.Add(new LOCAL_ARGUMENT_INFO
                    {
                        METHODLABELNAME = xMethodLabel,
                        IsArgument = true,
                        INDEXINMETHOD = (int)(i + xIdxOffset),
                        NAME = xParams[i].Name,
                        OFFSET = xOffset,
                        TYPENAME = xParams[i].ParameterType.FullName
                    });
                }
            }
        }

        public DebugInfo.SequencePoint[] GenerateDebugSequencePoints(_MethodInfo aMethod, DebugMode aDebugMode)
        {
            if (aDebugMode == DebugMode.Source)
            {
                // Would be nice to use xMethodSymbols.GetSourceStartEnd but we cant
                // because its not implemented by the unmanaged code underneath.
                DebugInfo.SequencePoint[] mSequences = DebugInfo.GetSequencePoints(aMethod.MethodBase, true);
                if (mSequences.Length > 0)
                {
                    DebugInfo.AddDocument(mSequences[0].Document);

                    var xMethod = new Method
                    {
                        ID = aMethod.DebugMethodUID,
                        TypeToken = aMethod.MethodBase.DeclaringType.GetMetadataToken(),
                        MethodToken = aMethod.MethodBase.MetadataToken,
                        LabelStartID = aMethod.DebugMethodLabelUID,
                        LabelEndID = aMethod.EndMethodID,
                        LabelCall = aMethod.MethodLabel
                    };
                    if (DebugInfo.AssemblyGUIDs.TryGetValue(aMethod.MethodBase.DeclaringType.Assembly, out var xAssemblyFileID))
                    {
                        xMethod.AssemblyFileID = xAssemblyFileID;
                    }
                    xMethod.DocumentID = DebugInfo.DocumentGUIDs[mSequences[0].Document.ToLower()];
                    xMethod.LineColStart = ((long)mSequences[0].LineStart << 32) + mSequences[0].ColStart;
                    xMethod.LineColEnd = ((long)(mSequences[mSequences.Length - 1].LineEnd) << 32) + mSequences[mSequences.Length - 1].ColEnd;
                    DebugInfo.AddMethod(xMethod);
                }
                return mSequences;
            }
            return new DebugInfo.SequencePoint[0];
        }

        private void MethodEnd(_MethodInfo aMethod)
        {
            XS.Comment("End Method: " + aMethod.MethodBase.Name);

            uint xReturnSize = 0;
            var xMethInfo = aMethod.MethodBase as MethodInfo;
            if (xMethInfo != null)
            {
                xReturnSize = ILOp.Align(ILOp.SizeOfType(xMethInfo.ReturnType), 4);
            }

            var xMethodLabel = ILOp.GetLabel(aMethod);
            XS.Label(xMethodLabel + EndOfMethodLabelNameNormal);
            XS.Comment("Following code is for debugging. Adjust accordingly!");
            XS.Set(AsmMarker.Labels[AsmMarker.Type.Int_LastKnownAddress], xMethodLabel + EndOfMethodLabelNameNormal, destinationIsIndirect: true);

            XS.Set(ECX, 0);
            var xTotalArgsSize = (from item in aMethod.MethodBase.GetParameters()
                                  select (int)ILOp.Align(ILOp.SizeOfType(item.ParameterType), 4)).Sum();
            if (!aMethod.MethodBase.IsStatic)
            {
                if (aMethod.MethodBase.DeclaringType.IsValueType)
                {
                    xTotalArgsSize += 4; // only a reference is passed
                }
                else
                {
                    xTotalArgsSize += (int)ILOp.Align(ILOp.SizeOfType(aMethod.MethodBase.DeclaringType), 4);
                }
            }

            if (aMethod.PluggedMethod != null)
            {
                xReturnSize = 0;
                xMethInfo = aMethod.PluggedMethod.MethodBase as MethodInfo;
                if (xMethInfo != null)
                {
                    xReturnSize = ILOp.Align(ILOp.SizeOfType(xMethInfo.ReturnType), 4);
                }
                xTotalArgsSize = (from item in aMethod.PluggedMethod.MethodBase.GetParameters()
                                  select (int)ILOp.Align(ILOp.SizeOfType(item.ParameterType), 4)).Sum();
                if (!aMethod.PluggedMethod.MethodBase.IsStatic)
                {
                    if (aMethod.PluggedMethod.MethodBase.DeclaringType.IsValueType)
                    {
                        xTotalArgsSize += 4; // only a reference is passed
                    }
                    else
                    {
                        xTotalArgsSize += (int)ILOp.Align(ILOp.SizeOfType(aMethod.PluggedMethod.MethodBase.DeclaringType), 4);
                    }
                }
            }

            if (xReturnSize > 0)
            {
                var xOffset = GetResultCodeOffset(xReturnSize, (uint)xTotalArgsSize);
                for (int i = 0; i < ((int)(xReturnSize / 4)); i++)
                {
                    XS.Pop(EAX);
                    XS.Set(EBP, EAX, destinationDisplacement: (int)(xOffset + ((i + 0) * 4)));
                }
                // extra stack space is the space reserved for example when a "public static int TestMethod();" method is called, 4 bytes is pushed, to make room for result;
            }
            // Handle exception code here
            var xLabelExc = xMethodLabel + EndOfMethodLabelNameException;
            XS.Label(xLabelExc);
            if (aMethod.MethodAssembler == null && aMethod.PlugMethod == null && !aMethod.IsInlineAssembler)
            {
                uint xLocalsSize = 0;
                var xLocalInfos = aMethod.MethodBase.GetLocalVariables();
                for (int j = xLocalInfos.Count - 1; j >= 0; j--)
                {
                    xLocalsSize += ILOp.Align(ILOp.SizeOfType(xLocalInfos[j].LocalType), 4);

                    if (xLocalsSize >= 256)
                    {
                        XS.Add(ESP, 255);
                        xLocalsSize -= 255;
                    }
                }
                if (xLocalsSize > 0)
                {
                    XS.Add(ESP, xLocalsSize);
                }
            }

            if (DebugEnabled && StackCorruptionDetection)
            {
                // if debugstub is active, emit a stack corruption detection. at this point EBP and ESP should have the same value.
                // if not, we should somehow break here.
                XS.Set(EAX, ESP);
                XS.Set(EBX, EBP);
                XS.Compare(EAX, EBX);
                XS.Jump(ConditionalTestEnum.Equal, xLabelExc + "__2");
                XS.ClearInterruptFlag();
                // don't remove the call. It seems pointless, but we need it to retrieve the EIP value
                XS.Call(".MethodFooterStackCorruptionCheck_Break_on_location");
                XS.Label(xLabelExc + ".MethodFooterStackCorruptionCheck_Break_on_location");
                XS.Exchange(BX, BX);
                XS.Pop(ECX);
                XS.Push(EAX);
                XS.Push(EBX);
                XS.Set(AsmMarker.Labels[AsmMarker.Type.DebugStub_CallerEIP], ECX, destinationIsIndirect: true);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendSimpleNumber]);
                XS.Add(ESP, 4);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendSimpleNumber]);
                XS.Add(ESP, 4);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendStackCorruptedEvent]);
                XS.Halt();
            }
            XS.Label(xLabelExc + "__2");
            XS.Pop(EBP);
            var xRetSize = (xTotalArgsSize) - ((int)xReturnSize);
            if (xRetSize < 0)
            {
                xRetSize = 0;
            }
            XS.Return((uint)xRetSize);

            // Final, after all code. Points to op AFTER method.
            XS.Label("GUID_" + aMethod.EndMethodID.ToString());
        }

        public void FinalizeDebugInfo()
        {
            DebugInfo.AddDocument(null, true);
            DebugInfo.AddAssemblies(null, true);
            DebugInfo.AddMethod(null, true);
            DebugInfo.WriteAllLocalsArgumentsInfos(mLocals_Arguments_Infos);
            DebugInfo.AddSymbols(mSymbols, true);
            DebugInfo.AddINT3Labels(mINT3Labels, true);
        }

        public static uint GetResultCodeOffset(uint aResultSize, uint aTotalArgumentSize)
        {
            uint xOffset = 8;
            if ((aTotalArgumentSize > 0) && (aTotalArgumentSize >= aResultSize))
            {
                xOffset += aTotalArgumentSize;
                xOffset -= aResultSize;
            }
            return xOffset;
        }

        public void ProcessMethod(_MethodInfo aMethod, List<ILOpCode> aOpCodes)
        {
            try
            {
                // We check this here and not scanner as when scanner makes these
                // plugs may still have not yet been scanned that it will depend on.
                // But by the time we make it here, they have to be resolved.
                if (aMethod.Type == _MethodInfo.TypeEnum.NeedsPlug && aMethod.PlugMethod == null)
                {
                    throw new Exception("Method needs plug, but no plug was assigned.");
                }

                // todo: MtW: how to do this? we need some extra space.
                //		see ConstructLabel for extra info
                if (aMethod.UID > 0x00FFFFFF)
                {
                    throw new Exception("Too many methods.");
                }

                MethodBegin(aMethod);
                mLog.WriteLine("Method '{0}', ID = '{1}'", aMethod.MethodBase.GetFullName(), aMethod.UID);
                mLog.Flush();
                if (aMethod.MethodAssembler != null)
                {
                    var xAssembler = new ZooAssemblerMethod(loadCtx, aMethod.MethodAssembler);
                    xAssembler.AssembleNew(Assembler, aMethod.PluggedMethod);
                }
                else if (aMethod.IsInlineAssembler)
                {
                    var args = new object[aMethod.MethodBase.GetParameters().Length];
                    ZooAssemblerMethod.DoInline(loadCtx, aMethod.MethodBase, args);
                }
                else
                {
                    var emitINT3 = true;
                    AnalyseMethodOpCodes(aMethod, aOpCodes);

                    EmitInstructions(aMethod, aOpCodes, ref emitINT3);
                }
                MethodEnd(aMethod);
            }
            catch (Exception E)
            {
                throw new Exception("Error compiling method '" + aMethod.MethodBase.GetFullName() + "': " + E.ToString(), E);
            }
        }

        public void AnalyseMethodOpCodes(_MethodInfo aMethod, List<ILOpCode> aOpCodes)
        {
            var mSequences = GenerateDebugSequencePoints(aMethod, DebugMode); // TODO: Maybe control the DebugeMode to reduce methods debug symbols are generated for
                                                                              // now emit the actual assembler code for this method.

            //Conditions under which we should emit an INT3 instead of a plceholder NOP:
            /* - First instruction in a Method / Loop / If / Else etc.
             *   -- In essence, whenever there is a opening {
             *   -- C# Debug builds automatically insert NOPs at these locations (otherwise NOP is not used)
             *   -- So only insert an INT3 when we are about to insert a NOP that came from IL code
             */
            CompilerHelpers.Debug($"AppAssembler: Method: {aMethod.MethodBase.GetFullName()}");
            var method = new ILMethod(aOpCodes, mSequences);
            method.Analyse();
        }

#pragma warning disable CA1822 // Mark members as static
        private void BeforeEmitInstructions(_MethodInfo aMethod, List<ILOpCode> aCurrentGroup)
#pragma warning restore CA1822 // Mark members as static
        {
            // do optimizations
        }

#pragma warning disable CA1822 // Mark members as static
        private void AfterEmitInstructions(_MethodInfo aMethod, List<ILOpCode> aCurrentGroup)
#pragma warning restore CA1822 // Mark members as static
        {
            // do optimizations
        }

        //private static bool mDebugStackErrors = true;

        private void EmitInstructions(_MethodInfo aMethod, List<ILOpCode> aCurrentGroup, ref bool emitINT3)
        {
            BeforeEmitInstructions(aMethod, aCurrentGroup);

            var xFirstInstruction = true;
            foreach (var xOpCode in aCurrentGroup)
            {
                ushort xOpCodeVal = (ushort)xOpCode.OpCode;
                ILOp xILOp;
                if (xOpCodeVal <= 0xFF)
                {
                    xILOp = mILOpsLo[xOpCodeVal];
                }
                else
                {
                    xILOp = mILOpsHi[xOpCodeVal & 0xFF];
                }
                mLog.Flush();

                int? xLocalsSize = null;
                //calculate local size once
                if (aMethod.MethodBase != null)
                {
                    var xLocals = aMethod.MethodBase.GetLocalVariables();
                    xLocalsSize = (from item in xLocals
                                       select (int)ILOp.Align(ILOp.SizeOfType(item.LocalType), 4)).Sum();
                }

                //Only emit INT3 as per conditions above...
                BeforeOp(aMethod, xOpCode, emitINT3, out var INT3Emitted, xFirstInstruction, xLocalsSize);
                xFirstInstruction = false;
                //Emit INT3 on the first non-NOP instruction immediately after a NOP
                // - This is because TracePoints for NOP are automatically ignored in code called below this
                emitINT3 = (emitINT3 && !INT3Emitted) || xILOp is Nop;

                XS.Comment(xILOp.ToString());
                var xNextPosition = xOpCode.Position + 1;

                #region Exception handling support code

                _ExceptionRegionInfo xCurrentExceptionRegion = null;
                // todo: add support for nested handlers using a stack or so..
                foreach (_ExceptionRegionInfo xHandler in aMethod.MethodBase.GetExceptionRegionInfos())
                {
                    if (xHandler.TryOffset > 0)
                    {
                        if (xHandler.TryOffset <= xNextPosition && (xHandler.TryLength + xHandler.TryOffset) > xNextPosition)
                        {
                            if (xCurrentExceptionRegion == null)
                            {
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                            else if (xHandler.TryOffset > xCurrentExceptionRegion.TryOffset && (xHandler.TryLength + xHandler.TryOffset) < (xCurrentExceptionRegion.TryLength + xCurrentExceptionRegion.TryOffset))
                            {
                                // only replace if the current found handler is narrower
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                        }
                    }
                    if (xHandler.HandlerOffset > 0)
                    {
                        if (xHandler.HandlerOffset <= xNextPosition && (xHandler.HandlerOffset + xHandler.HandlerLength) > xNextPosition)
                        {
                            if (xCurrentExceptionRegion == null)
                            {
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                            else if (xHandler.HandlerOffset > xCurrentExceptionRegion.HandlerOffset && (xHandler.HandlerOffset + xHandler.HandlerLength) < (xCurrentExceptionRegion.HandlerOffset + xCurrentExceptionRegion.HandlerLength))
                            {
                                // only replace if the current found handler is narrower
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                        }
                    }
                    if (xHandler.Kind.HasFlag(ExceptionRegionKind.Filter))
                    {
                        if (xHandler.FilterOffset > 0)
                        {
                            if (xHandler.FilterOffset <= xNextPosition)
                            {
                                if (xCurrentExceptionRegion == null)
                                {
                                    xCurrentExceptionRegion = xHandler;
                                    continue;
                                }
                                else if (xHandler.FilterOffset > xCurrentExceptionRegion.FilterOffset)
                                {
                                    // only replace if the current found handler is narrower
                                    xCurrentExceptionRegion = xHandler;
                                    continue;
                                }
                            }
                        }
                    }
                }

                #endregion

                var xNeedsExceptionPush = xCurrentExceptionRegion != null &&
                    (((xCurrentExceptionRegion.HandlerOffset > 0 && xCurrentExceptionRegion.HandlerOffset == xOpCode.Position)
                            || (xCurrentExceptionRegion.Kind.HasFlag(ExceptionRegionKind.Filter) && xCurrentExceptionRegion.FilterOffset > 0
                                && xCurrentExceptionRegion.FilterOffset == xOpCode.Position))
                        && xCurrentExceptionRegion.Kind == ExceptionRegionKind.Catch);
                if (xNeedsExceptionPush)
                {
                    Push(LabelName.GetStaticFieldName(ExceptionHelperRefs.CurrentExceptionRef), true);
                    XS.Push(0);
                }

                xILOp.DebugEnabled = DebugEnabled;
                xILOp.Execute(aMethod, xOpCode);

                AfterOp(aMethod, xOpCode);
                //mLog.WriteLine( " end: " + Stack.Count.ToString() );
            }
            AfterEmitInstructions(aMethod, aCurrentGroup);
        }

        private void InitILOps()
        {
            InitILOps(typeof(ILOp));
        }

        private void InitILOps(Type aAssemblerBaseOp)
        {
            foreach (var xType in aAssemblerBaseOp.Assembly.GetExportedTypes())
            {
                if (xType.IsSubclassOf(aAssemblerBaseOp))
                {
                    var xAttribs = xType.GetCustomAttributes<OpCodeAttribute>(false);
                    foreach (var xAttrib in xAttribs)
                    {
                        var xOpCode = (ushort)xAttrib.OpCode;
                        var xCtor = xType.GetConstructor(new[] { typeof(Assembler) });
                        var xILOp = (ILOp)xCtor.Invoke(new object[] { Assembler });
                        if (xOpCode <= 0xFF)
                        {
                            mILOpsLo[xOpCode] = xILOp;
                        }
                        else
                        {
                            mILOpsHi[xOpCode & 0xFF] = xILOp;
                        }
                    }
                }
            }
        }

        private static void Push(uint aValue)
        {
            XS.Push(aValue);
        }

        private static void Push(string aLabelName, bool isIndirect = false)
        {
            XS.Push(aLabelName, isIndirect: isIndirect);
        }

        private static void Call(MethodBase aMethod)
        {
            XS.Call(LabelName.Get(aMethod));
        }

        private static _FieldInfo ResolveField(_MethodInfo method, string fieldId, bool aOnlyInstance)
        {
            return ILOp.ResolveField(method.MethodBase.DeclaringType, fieldId, aOnlyInstance);
        }

        private void Ldarg(_MethodInfo aMethod, int aIndex)
        {
            X86.IL.Ldarg.DoExecute(Assembler, aMethod, (ushort)aIndex);
        }

        private void Call(_MethodInfo aMethod, _MethodInfo aTargetMethod, string aNextLabel)
        {
            var xSize = X86.IL.Call.GetStackSizeToReservate(aTargetMethod.MethodBase);
            if (xSize > 0)
            {
                XS.Sub(ESP, xSize);
            }
            XS.Call(ILOp.GetLabel(aTargetMethod));
            var xMethodInfo = aMethod.MethodBase as MethodInfo;

            uint xReturnsize = 0;
            if (xMethodInfo != null)
            {
                xReturnsize = ILOp.SizeOfType(((MethodInfo)aMethod.MethodBase).ReturnType);
            }

            ILOp.EmitExceptionLogic(Assembler, aMethod, null, true,
                     delegate ()
                     {
                         var xResultSize = xReturnsize;
                         if (xResultSize % 4 != 0)
                         {
                             xResultSize += 4 - (xResultSize % 4);
                         }
                         for (int i = 0; i < xResultSize / 4; i++)
                         {
                             XS.Add(ESP, 4);
                         }
                     }, aNextLabel);
        }

        private void Ldflda(_MethodInfo aMethod, _FieldInfo aFieldInfo)
        {
            X86.IL.Ldflda.DoExecute(Assembler, aMethod, aMethod.MethodBase.DeclaringType, aFieldInfo, false, false, aFieldInfo.DeclaringType);
        }

        private void Ldsflda(_MethodInfo aMethod, _FieldInfo aFieldInfo)
        {
            X86.IL.Ldsflda.DoExecute(Assembler, aMethod, LabelName.GetStaticFieldName(aFieldInfo.Field), aMethod.MethodBase.DeclaringType, null);
        }

        public static byte[] AllocateEmptyArray(int aLength, int aElementSize, uint aArrayTypeID)
        {
            var xData = new byte[16 + aLength * aElementSize];
            var xTemp = BitConverter.GetBytes(aArrayTypeID);
            Array.Copy(xTemp, 0, xData, 0, 4);
            xTemp = BitConverter.GetBytes((uint)ObjectUtils.InstanceTypeEnum.StaticEmbeddedArray);
            Array.Copy(xTemp, 0, xData, 4, 4);
            xTemp = BitConverter.GetBytes(aLength);
            Array.Copy(xTemp, 0, xData, 8, 4);
            xTemp = BitConverter.GetBytes(aElementSize);
            Array.Copy(xTemp, 0, xData, 12, 4);
            return xData;
        }

        public const string InitVMTCodeLabel = "___INIT__VMT__CODE____";

        public unsafe void GenerateVMTCode(HashSet<Type> aTypesSet, HashSet<MethodBase> aMethodsSet, Func<Type, uint> aGetTypeID, Func<MethodBase, uint> aGetMethodUID)
        {
            XS.Comment("---------------------------------------------------------");
            XS.Label(InitVMTCodeLabel);
            XS.Push(EBP);
            XS.Set(EBP, ESP);

            var xTypesFieldRef = VTablesImplRefs.VTablesImplDef.GetField("mTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            string xTheName = LabelName.GetStaticFieldName(xTypesFieldRef);
            DataMember xDataMember = (from item in XSharp.Assembler.Assembler.CurrentInstance.DataMembers
                                      where item.Name == xTheName
                                      select item).FirstOrDefault();
            if (xDataMember != null)
            {
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Remove(
                    (from item in XSharp.Assembler.Assembler.CurrentInstance.DataMembers
                     where item == xDataMember
                     select item).First());
            }

            uint xArrayTypeID = aGetTypeID(Base.Array);
            byte[] xData = AllocateEmptyArray(aTypesSet.Count, (int)ILOp.SizeOfType(Base.VTable), xArrayTypeID);
            XS.DataMemberBytes(xTheName + "_Contents", xData);
            XS.DataMember(xTheName, 1, "db", "0, 0, 0, 0, 0, 0, 0, 0");
            XS.Set(xTheName, xTheName + "_Contents", destinationIsIndirect: true, destinationDisplacement: 4);
#if VMT_DEBUG
            using (var xVmtDebugOutput = XmlWriter.Create(
                File.Create(Path.Combine(mLogDir, @"vmt_debug.xml")), new XmlWriterSettings() { Indent = true }))
            {
                xVmtDebugOutput.WriteStartDocument();
                xVmtDebugOutput.WriteStartElement("VMT");
#endif
            //Push((uint)aTypesSet.Count);
            foreach (var xType in aTypesSet)
            {
                uint xTypeID = aGetTypeID(xType);
#if VMT_DEBUG
                    xVmtDebugOutput.WriteStartElement("Type");
                    xVmtDebugOutput.WriteAttributeString("TypeId", xTypeID.ToString());
                    if (xType.BaseType != null)
                    {
                        xVmtDebugOutput.WriteAttributeString("BaseTypeId", aGetTypeID(xType.BaseType).ToString());
                    }
                    xVmtDebugOutput.WriteAttributeString("Name", xType.FullName);
#endif

                var xEmittedMethods = GetEmittedMethods(xType, aMethodsSet);
                var xEmittedInterfaceMethods = GetEmittedInterfaceMethods(xType, aMethodsSet);

                int? xBaseIndex = null;
                if (xType.BaseType == null)
                {
                    xBaseIndex = (int)xTypeID;
                }
                else
                {
                    for (int t = 0; t < aTypesSet.Count; t++)
                    {
                        // todo: optimize check
                        var xItem = aTypesSet.Skip(t).First();
                        if (xItem.ToString() == xType.BaseType.ToString())
                        {
                            xBaseIndex = (int)aGetTypeID(xItem);
                            break;
                        }
                    }
                }
                if (xBaseIndex == null)
                {
                    throw new Exception("Base type not found!");
                }

                // Set type info
                string xTypeName = $"{LabelName.GetFullName(xType)} ASM_IS__{xType.Assembly.GetName().Name}";
                xTypeName = DataMember.FilterStringForIncorrectChars(xTypeName);

                // Type ID
                string xDataName = $"VMT__TYPE_ID_HOLDER__{xTypeName}";
                XS.Set(xDataName, (uint)xTypeID, destinationIsIndirect: true, size: RegisterSize.Int32);
                XS.DataMember(xDataName, xTypeID);
                Push(xTypeID);

                // Base Type ID
                Push((uint)xBaseIndex.Value);

                // Size
                Push(ILOp.SizeOfType(xType));

                // Interface Count
                var xInterfaces = xType.GetInterfaces();
                Push((uint)xInterfaces.Length);
                xData = AllocateEmptyArray(xInterfaces.Length, sizeof(uint), xArrayTypeID);
                // Interface Indexes Array
                xDataName = $"____SYSTEM____TYPE___{xTypeName}__InterfaceIndexesArray";
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Add(new DataMember(xDataName, xData));
                Push(xDataName);
                Push(0);

                // Method array
                xData = AllocateEmptyArray(xEmittedMethods.Count, sizeof(uint), xArrayTypeID);
                // Method Count
                Push((uint)xEmittedMethods.Count);
                // Method Indexes Array
                xDataName = $"____SYSTEM____TYPE___{xTypeName}__MethodIndexesArray";
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Add(new DataMember(xDataName, xData));
                Push(xDataName);
                Push(0);
                // Method Addresses Array
                xDataName = $"____SYSTEM____TYPE___{xTypeName}__MethodAddressesArray";
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Add(new DataMember(xDataName, xData));
                Push(xDataName);
                Push(0);

                // Interface methods
                xData = AllocateEmptyArray(xEmittedInterfaceMethods.Count, sizeof(uint), xArrayTypeID);
                // Interface method count
                Push((uint)xEmittedInterfaceMethods.Count);
                // Interface method indexes array
                xDataName = $"____SYSTEM____TYPE___{xTypeName}__InterfaceMethodIndexesArray";
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Add(new DataMember(xDataName, xData));
                Push(xDataName);
                Push(0);
                // Target method indexes array
                xDataName = $"____SYSTEM____TYPE___{xTypeName}__TargetMethodIndexesArray";
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Add(new DataMember(xDataName, xData));
                Push(xDataName);
                Push(0);

                // Full type name
                xDataName = $"____SYSTEM____TYPE___{xTypeName}";
                int xDataByteCount = Encoding.Unicode.GetByteCount($"{xType.FullName}, {xType.Assembly.FullName}");
                xData = AllocateEmptyArray(xDataByteCount, 2, xArrayTypeID);
                XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Add(new DataMember(xDataName, xData));

                Call(VTablesImplRefs.SetTypeInfoRef);

                for (int j = 0; j < xInterfaces.Length; j++)
                {
                    var xInterface = xInterfaces[j];
                    var xInterfaceTypeId = aGetTypeID(xInterface);
#if VMT_DEBUG
                        xVmtDebugOutput.WriteStartElement("Interface");
                        xVmtDebugOutput.WriteAttributeString("Id", xInterfaceTypeId.ToString());
                        xVmtDebugOutput.WriteAttributeString("Name", xInterface.GetFullName());
                        xVmtDebugOutput.WriteEndElement();
#endif
                    Push(xTypeID);
                    Push((uint)j);
                    Push(xInterfaceTypeId);
                    Call(VTablesImplRefs.SetInterfaceInfoRef);
                }

                for (int j = 0; j < xEmittedMethods.Count; j++)
                {
                    var xMethod = xEmittedMethods[j];
                    var xMethodUID = aGetMethodUID(xMethod);
#if VMT_DEBUG
                        xVmtDebugOutput.WriteStartElement("Method");
                        xVmtDebugOutput.WriteAttributeString("Id", xMethodUID.ToString());
                        xVmtDebugOutput.WriteAttributeString("Name", xMethod.GetFullName());
                        xVmtDebugOutput.WriteEndElement();
#endif
                    if (!xType.IsInterface)
                    {
                        Push(xTypeID);
                        Push((uint)j);
                        Push(xMethodUID);
                        if (xMethod.IsAbstract)
                        {
                            // abstract methods dont have bodies, oiw, are not emitted
                            Push(0);
                        }
                        else
                        {
                            Push(ILOp.GetLabel(xMethod));
                        }

                        Call(VTablesImplRefs.SetMethodInfoRef);
                    }
                }

                for (int j = 0; j < xEmittedInterfaceMethods.Count; j++)
                {
                    var xMethod = xEmittedInterfaceMethods.ElementAt(j);
                    var xInterfaceMethodUID = aGetMethodUID(xMethod.InterfaceMethod);
                    var xTargetMethodUID = aGetMethodUID(xMethod.TargetMethod);
#if VMT_DEBUG
                        xVmtDebugOutput.WriteStartElement("InterfaceMethod");
                        xVmtDebugOutput.WriteAttributeString("InterfaceMethodId", xInterfaceMethodUID.ToString());
                        xVmtDebugOutput.WriteAttributeString("TargetMethodId", xTargetMethodUID.ToString());
                        xVmtDebugOutput.WriteEndElement();
#endif
                    if (!xType.IsInterface)
                    {
                        Push(xTypeID);
                        Push((uint)j);
                        Push(xInterfaceMethodUID);
                        Push(xTargetMethodUID);

                        Call(VTablesImplRefs.SetInterfaceMethodInfoRef);
                    }
                }
#if VMT_DEBUG
                    xVmtDebugOutput.WriteEndElement(); // type
#endif
            }
#if VMT_DEBUG
                xVmtDebugOutput.WriteEndElement(); // types
                xVmtDebugOutput.WriteEndDocument();
            }
#endif

            XS.Label("_END_OF_" + InitVMTCodeLabel);
            XS.Pop(EBP);
            XS.Return();
        }

        private static IReadOnlyList<MethodBase> GetEmittedMethods(Type aType, HashSet<MethodBase> aMethodSet)
        {
            var xList = new List<MethodBase>();

            foreach (var xMethod in aMethodSet.Where(m => !m.IsStatic))
            {
                if (MemberInfoComparer.Instance.Equals(aType, xMethod.DeclaringType))
                {
                    xList.Add(xMethod);
                }
            }

            return xList;
        }

        private static readonly List<(MethodBase, MethodBase)> EmptyEmittedInterfaceMethodsList =
            new List<(MethodBase, MethodBase)>(0);

        private static IReadOnlyList<(MethodBase InterfaceMethod, MethodBase TargetMethod)> GetEmittedInterfaceMethods(
            Type aType, HashSet<MethodBase> aMethodSet)
        {
            if (aType.IsInterface || aType.IsArray)
            {
                return EmptyEmittedInterfaceMethodsList;
            }

            var xEmittedInterfaceMethods = new List<(MethodBase, MethodBase)>();

            foreach (var xInterface in aType.GetInterfaces())
            {
                var xInterfaceMap = aType.GetMyInterfaceMap(xInterface);

                foreach (var xMethod in aMethodSet)
                {
                    var xTargetMethod = xInterfaceMap.TargetMethods.SingleOrDefault(
                        m => MemberInfoComparer.Instance.Equals(m, xMethod) == true);

                    if (xTargetMethod != null)
                    {
                        var xInterfaceMethodIndex = Array.IndexOf(xInterfaceMap.TargetMethods, xTargetMethod);
                        var xInterfaceMethod = xInterfaceMap.InterfaceMethods[xInterfaceMethodIndex];

                        xEmittedInterfaceMethods.Add((xInterfaceMethod, xMethod));
                    }
                }
            }

            return xEmittedInterfaceMethods;
        }

        public void ProcessField(FieldInfo aField)
        {
            string xFieldName = LabelName.GetStaticFieldName(aField);
            string xFieldContentsName = $"{xFieldName}__Contents";

            if (XSharp.Assembler.Assembler.CurrentInstance.DataMembers.Count(x => x.Name == xFieldName) == 0)
            {
                var xItemList = aField.FetchCustomAttributes<ManifestResourceStreamAttribute>(false).ToList();
                object xItem = null;
                if (xItemList.Any())
                {
                    xItem = xItemList.First();
                }
                string xManifestResourceName = null;
                if (xItem != null)
                {
                    var xItemType = xItem.GetType();
                    xManifestResourceName = (string)xItemType.GetProperty("ResourceName")?.GetValue(xItem);
                }
                if (xManifestResourceName != null)
                {
                    // todo: add support for manifest streams again
                    //string xFileName = Path.Combine(mOutputDir,
                    //                                (xCurrentField.DeclaringType.Assembly.FullName + "__" + xManifestResourceName).Replace(",",
                    //                                                                                                                       "_") + ".res");
                    var xTarget = new StringBuilder();
                    byte[] xData;
                    using (var xStream = aField.DeclaringType?.Assembly.GetManifestResourceStream(xManifestResourceName))
                    {
                        if (xStream == null)
                        {
                            throw new Exception("Resource '" + xManifestResourceName + "' not found!");
                        }

                        uint xArrayTypeID = 0;
                        xData = AllocateEmptyArray((int)xStream.Length, 1, xArrayTypeID);
                        xStream.Read(xData, 16, (int)xStream.Length);

                        //xTarget.Append("0,");
                        //xTarget.Append((uint)ObjectUtils.InstanceTypeEnum.StaticEmbeddedArray);
                        //xTarget.Append(",");
                        //xTarget.Append((int)xStream.Length);
                        //xTarget.Append(",");
                        //xTarget.Append("1,");
                        //while (xStream.Position < xStream.Length)
                        //{
                        //    xTarget.Append(xStream.ReadByte());
                        //    xTarget.Append(",");
                        //}
                        //xTarget.Remove(xTarget.Length - 1, 1);
                    }

                    XS.DataMemberBytes(xFieldContentsName, xData);
                    XS.DataMember(xFieldName, 1, "dd", "0");
                    XS.DataMember("", 1, "dd", xFieldContentsName);

                    //Assembler.DataMembers.Add(new DataMember(xFieldContentsName, "db", xTarget.ToString()));
                    //Assembler.DataMembers.Add(new DataMember(xFieldName, "dd", xFieldContentsName));
                }
                else
                {
                    var xFieldType = aField.FieldType;
                    uint xFieldSize = ILOp.SizeOfType(aField.FieldType);
                    byte[] xData = new byte[xFieldSize];

                    if (xFieldType.IsValueType)
                    {
                        DebugSymbolReader.TryGetStaticFieldValue(aField.Module, aField.MetadataToken, ref xData);
                    }

                    var xAsmLabelAttributes = aField.FetchCustomAttributes<AsmLabel>();
                    if (xAsmLabelAttributes.Count() > 0)
                    {
                        Assembler.DataMembers.Add(new DataMember(xFieldName, xAsmLabelAttributes.Select(a => a.Label), xData));
                    }
                    else
                    {
                        Assembler.DataMembers.Add(new DataMember(xFieldName, xData));
                    }
                }
            }
        }

        /// <summary>
        /// Generates a forwarding stub, which transforms from the actual method to the plug.
        /// </summary>
        /// <param name="aFrom">The method to forward to the plug</param>
        /// <param name="aTo">The plug</param>
        internal void GenerateMethodForward(_MethodInfo aFrom, _MethodInfo aTo)
        {
            var xMethodLabel = ILOp.GetLabel(aFrom);
            var xEndOfMethodLabel = xMethodLabel + EndOfMethodLabelNameNormal;

            // todo: completely get rid of this kind of trampoline code
            MethodBegin(aFrom);
            {
                var xExtraSpaceToSkipDueToObjectPointerAccess = 0u;

                var xFromParameters = aFrom.MethodBase.GetParameters();
                var xParams = aTo.MethodBase.GetParameters().ToArray();
                if (aTo.IsWildcard)
                {
                    xParams = aFrom.MethodBase.GetParameters();
                }

                int xCurParamIdx = 0;
                var xCurParamOffset = 0;
                if (!aFrom.MethodBase.IsStatic)
                {
                    Ldarg(aFrom, 0);

                    if (!aTo.IsWildcard)
                    {
                        var xObjectPointerAccessAttrib = xParams[0].FetchCustomAttribute<ObjectPointerAccess>(true);
                        if (xObjectPointerAccessAttrib != null)
                        {
                            XS.Comment("Skipping the reference to the next object reference.");
                            XS.Add(ESP, 4);
                            xExtraSpaceToSkipDueToObjectPointerAccess += 4;
                        }
                        else
                        {
                            if (ILOp.IsReferenceType(aFrom.MethodBase.DeclaringType) && !ILOp.IsReferenceType(xParams[0].ParameterType))
                            {
                                throw new Exception("Original method argument $this is a reference type. Plug attribute first argument is not an argument type, nor was it marked with ObjectPointerAccessAttribute! Method: " + aFrom.MethodBase.GetFullName() + " Parameter: " + xParams[0].Name);
                            }
                        }

                        xParams = xParams.Skip(1).ToArray();
                    }
                    xCurParamOffset = 1;
                }

                var xOriginalParamsIdx = 0;
                foreach (var xParam in xParams)
                {
                    var xFieldAccessAttrib = xParam.FetchCustomAttribute<FieldAccess>(true);
                    var xObjectPointerAccessAttrib = xParam.FetchCustomAttribute<ObjectPointerAccess>(true);
                    if (xFieldAccessAttrib != null)
                    {
                        // field access
                        XS.Comment("Loading address of field '" + xFieldAccessAttrib.Name + "'");
                        var xFieldInfo = ResolveField(aFrom, xFieldAccessAttrib.Name, false);
                        if (xFieldInfo.IsStatic)
                        {
                            Ldsflda(aFrom, xFieldInfo);
                        }
                        else
                        {
                            Ldarg(aFrom, 0);
                            Ldflda(aFrom, xFieldInfo);
                        }
                    }
                    else if (xObjectPointerAccessAttrib != null)
                    {
                        xOriginalParamsIdx++;
                        Ldarg(aFrom, xCurParamIdx + xCurParamOffset);
                        XS.Add(ESP, 4);
                        xExtraSpaceToSkipDueToObjectPointerAccess += 4;
                        xCurParamIdx++;
                    }
                    else
                    {
                        if (ILOp.IsReferenceType(xFromParameters[xOriginalParamsIdx].ParameterType) && !ILOp.IsReferenceType(xParams[xCurParamIdx].ParameterType))
                        {
                            throw new Exception("Original method argument $this is a reference type. Plug attribute first argument is not an argument type, nor was it marked with ObjectPointerAccessAttribute! Method: " + aFrom.MethodBase.GetFullName() + " Parameter: " + xParam.Name);
                        }
                        // normal field access
                        XS.Comment("Loading parameter " + (xCurParamIdx + xCurParamOffset));
                        Ldarg(aFrom, xCurParamIdx + xCurParamOffset);
                        xCurParamIdx++;
                        xOriginalParamsIdx++;
                    }
                }
                Call(aFrom, aTo, xEndOfMethodLabel);
            }
            MethodEnd(aFrom);
        }

        // These are all temp functions until we move to the new assembler.
        // They are used to clean up the old assembler slightly while retaining compatibiltiy for now
        public static string TmpPosLabel(_MethodInfo aMethod, int aOffset)
        {
            return ILOp.GetLabel(aMethod, aOffset);
        }

        public static string TmpPosLabel(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            return TmpPosLabel(aMethod, aOpCode.Position);
        }

        public static string TmpBranchLabel(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            return TmpPosLabel(aMethod, ((OpBranch)aOpCode).Value);
        }

        public void EmitEntrypoint(MethodBase aEntrypoint, MethodBase[] aBootEntries = null)
        {
            // at the time the datamembers for literal strings are created, the type id for string is not yet determined.
            // for now, we fix this at runtime.
            XS.Label(InitStringIDsLabel);
            XS.Push(EBP);
            XS.Set(EBP, ESP);
            XS.Set(EAX, ILOp.GetTypeIDLabel(BaseTypes.String), sourceIsIndirect: true);
            XS.Set(LabelName.GetStaticFieldName(BaseTypes.String.GetField("Empty", BindingFlags.Static | BindingFlags.Public)),
                LdStr.GetContentsArrayName(""), destinationDisplacement: 4);

            var xMemberId = 0;

            foreach (var xDataMember in Assembler.DataMembers)
            {
                if (!xDataMember.Name.StartsWith("StringLiteral"))
                {
                    continue;
                }
                if (xDataMember.Name.EndsWith("__Handle"))
                {
                    continue;
                }
                if (xMemberId % 100 == 0)
                {
                    Assembler.WriteDebugVideo(".");
                }
                xMemberId++;
                new Mov { DestinationRef = ElementReference.New(xDataMember.Name), DestinationIsIndirect = true, SourceReg = RegistersEnum.EAX };
            }
            Assembler.WriteDebugVideo("Done");
            XS.Pop(EBP);
            XS.Return();

            XS.Label(CosmosAssembler.EntryPointName);
            XS.Push(EBP);
            XS.Set(EBP, ESP);
            Assembler.WriteDebugVideo("Initializing VMT.");
            XS.Call(InitVMTCodeLabel);
            Assembler.WriteDebugVideo("Initializing string IDs.");
            XS.Call(InitStringIDsLabel);
            Assembler.WriteDebugVideo("Done initializing string IDs");
            // we now need to do "newobj" on the entry point, and after that, call .Start on it
            var xCurLabel = CosmosAssembler.EntryPointName + ".CreateEntrypoint";
            XS.Label(xCurLabel);
            Assembler.WriteDebugVideo("Now create the kernel class");
            if (!CompilerEngine.UseGen3Kernel)
            {
                Newobj.Assemble(XSharp.Assembler.Assembler.CurrentInstance, null, null, xCurLabel, aEntrypoint.DeclaringType, aEntrypoint, DebugEnabled);
                Assembler.WriteDebugVideo("Kernel class created");
            }
            xCurLabel = CosmosAssembler.EntryPointName + ".CallStart";
            XS.Label(xCurLabel);
            if (CompilerEngine.UseGen3Kernel)
            {
                foreach (var xBootEntry in aBootEntries)
                {
                    Assembler.WriteDebugVideo(xBootEntry.Name);
                    X86.IL.Call.DoExecute(Assembler, null, xBootEntry, null, null, null, DebugEnabled);
                }
            }
            else
            {
                X86.IL.Call.DoExecute(Assembler, null, aEntrypoint.DeclaringType.BaseType.GetMethod(CompilerEngine.UseGen3Kernel ? "EntryPoint" : "Start"), null, xCurLabel, CosmosAssembler.EntryPointName + ".AfterStart", DebugEnabled);
            }
            XS.Label(CosmosAssembler.EntryPointName + ".AfterStart");
            XS.Pop(EBP);
            XS.Return();

            if (ShouldOptimize)
            {
                Optimizer.Optimize(Assembler);
            }
        }

#pragma warning disable CA1822 // Mark members as static
        private void AfterOp(_MethodInfo aMethod, ILOpCode aOpCode)
#pragma warning restore CA1822 // Mark members as static
        {
        }

        private void BeforeOp(_MethodInfo aMethod, ILOpCode aOpCode, bool emitInt3NotNop, out bool INT3Emitted, bool hasSourcePoint, int? xLocalsSize)
        {
            if (DebugMode == DebugMode.Source)
            {
                Assembler.EmitAsmLabels = false;
            }

            string xLabel = TmpPosLabel(aMethod, aOpCode);
            Label.LastFullLabel = xLabel;
            XS.Label(xLabel);

            uint? xStackDifference = null;

            if (mSymbols != null && aOpCode.OpCode != ILOpCode.Code.Nop)
            {
                var xMLSymbol = new MethodIlOp
                {
                    LabelName = xLabel
                };

                var xStackSize = aOpCode.StackOffsetBeforeExecution.Value;

                xMLSymbol.StackDiff = -1;
                if (aMethod.MethodBase != null)
                {
                    xMLSymbol.StackDiff = checked((int)(xLocalsSize + xStackSize));
                    xStackDifference = (uint?)xMLSymbol.StackDiff;
                }
                xMLSymbol.IlOffset = aOpCode.Position;
                xMLSymbol.MethodID = aMethod.DebugMethodUID;

                mSymbols.Add(xMLSymbol);
                DebugInfo.AddSymbols(mSymbols);
            }
            DebugInfo.AddSymbols(mSymbols, false);

            EmitTracer(aMethod, aOpCode, aMethod.MethodBase.DeclaringType.Namespace, emitInt3NotNop,
                out INT3Emitted, out var INT3PlaceholderEmitted, hasSourcePoint);

            if (INT3Emitted || INT3PlaceholderEmitted)
            {
                var xINT3Label = new INT3Label
                {
                    LabelName = xLabel,
                    MethodID = aMethod.DebugMethodUID,
                    LeaveAsINT3 = INT3Emitted
                };
                mINT3Labels.Add(xINT3Label);
                DebugInfo.AddINT3Labels(mINT3Labels);
            }

            if (DebugEnabled && StackCorruptionDetection && StackCorruptionDetectionLevel == StackCorruptionDetectionLevel.AllInstructions
                && (aOpCode.OpCode != ILOpCode.Code.Nop || aOpCode.StackOffsetBeforeExecution != null))
            {
                // if debugstub is active, emit a stack corruption detection. at this point, the difference between EBP and ESP
                // should be equal to the local variables sizes and the IL stack.
                // if not, we should break here.

                // first, calculate the expected difference
                if (xStackDifference == null)
                {
                    xStackDifference = aMethod.LocalVariablesSize;
                    xStackDifference += aOpCode.StackOffsetBeforeExecution;
                }

                XS.Comment("Stack difference = " + xStackDifference);

                // if debugstub is active, emit a stack corruption detection. at this point EBP and ESP should have the same value.
                // if not, we should somehow break here.
                XS.Set(EAX, ESP);
                XS.Set(EBX, EBP);
                if (xStackDifference != 0)
                {
                    XS.Add(EAX, xStackDifference.Value);
                }
                XS.Compare(EAX, EBX);
                XS.Jump(ConditionalTestEnum.Equal, xLabel + ".StackCorruptionCheck_End");
                XS.Push(EAX);
                XS.Push(EBX);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendSimpleNumber]);
                XS.Add(ESP, 4);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendSimpleNumber]);

                XS.ClearInterruptFlag();
                // don't remove the call. It seems pointless, but we need it to retrieve the EIP value
                XS.Call(xLabel + ".StackCorruptionCheck_GetAddress");
                XS.Label(xLabel + ".StackCorruptionCheck_GetAddress");
                XS.Exchange(BX, BX);
                XS.Pop(EAX);
                XS.Set(AsmMarker.Labels[AsmMarker.Type.DebugStub_CallerEIP], EAX, destinationIsIndirect: true);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendStackCorruptedEvent]);
                XS.Halt();
                XS.Label(xLabel + ".StackCorruptionCheck_End");
            }
        }

        private void EmitTracer(_MethodInfo aMethod, ILOpCode aOp, string aNamespace, bool emitInt3NotNop, out bool INT3Emitted, out bool INT3PlaceholderEmitted, bool isNewSourcePoint)
        {
            // NOTE - These if statements can be optimized down - but clarity is
            // more important than the optimizations. Furthermore the optimizations available
            // would not offer much benefit

            // Determine if a new DebugStub should be emitted

            INT3Emitted = false;
            INT3PlaceholderEmitted = false;

            if (aOp.OpCode == ILOpCode.Code.Nop)
            {
                // Skip NOOP's so we dont have breakpoints on them
                //TODO: Each IL op should exist in IL, and descendants in IL.X86.
                // Because of this we have this hack
                return;
            }
            else if (DebugEnabled == false)
            {
                return;
            }
            else if (DebugMode == DebugMode.Source)
            {
                // If the current position equals one of the offsets, then we have
                // reached a new atomic C# statement
                if (!isNewSourcePoint)
                {
                    return;
                }
            }

            // Check if the DebugStub has been disabled for this method
            if ((!IgnoreDebugStubAttribute) && (aMethod.DebugStubOff))
            {
                return;
            }

            // This test fixes issue #15638
            if (null != aNamespace)
            {
                // Check options for Debug Ring
                // Set based on TracedAssemblies
                if (TraceAssemblies > TraceAssemblies.None)
                {
                    if (TraceAssemblies < TraceAssemblies.All)
                    {
                        if (aNamespace.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                        if (aNamespace.ToLower() == "system")
                        {
                            return;
                        }
                        if (aNamespace.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }

                    if (TraceAssemblies < TraceAssemblies.Cosmos)
                    {
                        //TODO: Maybe an attribute that could be used to turn tracing on and off
                        if (aNamespace.StartsWith("Cosmos.", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
            // If we made it this far without a return, emit the Tracer
            // We used to emit an INT3, but this meant the DS would brwak after every C# line
            // Breaking that frequently is of course, pointless and slow.
            // So now we emit mostly NOPs and only put an INT3 when told to.
            // We should only be told to put an INT3 at the start of method but this may change so search for more comments on this.
            if (emitInt3NotNop)
            {
                INT3Emitted = true;
                XS.Int3();
            }
            else
            {
                INT3PlaceholderEmitted = true;
                XS.DebugNoop();
            }
        }
    }
}
