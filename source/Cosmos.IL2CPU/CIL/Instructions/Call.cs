using System;
using System.Reflection;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using IL2CPU.API;
using XSharp;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Call)]
    public class Call : ILOp
    {
        public Call(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public static uint GetStackSizeToReservate(MethodBase aMethod, Type aType = null)
        {
            var xMethodInfo = aMethod as MethodInfo;
            uint xReturnSize = 0;
            if (xMethodInfo != null)
            {
                xReturnSize = SizeOfType(xMethodInfo.ReturnType);
            }
            if (xReturnSize == 0)
            {
                return 0;
            }

            // todo: implement exception support
            int xExtraStackSize = (int)Align(xReturnSize, 4);
            var xParameters = aMethod.GetParameters();
            foreach (var xItem in xParameters)
            {
                xExtraStackSize -= (int)Align(SizeOfType(xItem.ParameterType), 4);
            }
            if (!xMethodInfo.IsStatic)
            {
                if (aType != null)
                {
                    if (IsReferenceType(aType))
                    {
                        xExtraStackSize -= GetObjectReferenceSize();
                    }
                    else
                    {
                        xExtraStackSize -= 4;
                    }
                }
                else
                {
                    if (IsReferenceType(aMethod.DeclaringType))
                    {
                        xExtraStackSize -= GetObjectReferenceSize();
                    }
                    else
                    {
                        xExtraStackSize -= 4;
                    }
                }
            }

            if (xExtraStackSize > 0)
            {
                return (uint)xExtraStackSize;
            }
            return 0;
        }

        private static int GetObjectReferenceSize()
        {
            // old code, which goof up everything for structs
            //return (int)Align(SizeOfType(xMethodInfo.DeclaringType), 4);
            // TODO native pointer size, so that COSMOS could be 64 bit OS
            return 8;
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpMethod = aOpCode as OpMethod;
            DoExecute(Assembler, aMethod, xOpMethod.Value, aOpCode, LabelName.Get(aMethod.MethodBase), DebugEnabled);
        }

        public static void DoExecute(XSharp.Assembler.Assembler Assembler, Il2cpuMethodInfo aCurrentMethod, MethodBase aTargetMethod, ILOpCode aCurrent, string currentLabel, bool debugEnabled)
        {
            DoExecute(Assembler, aCurrentMethod, aTargetMethod, aCurrent, currentLabel, ILOp.GetLabel(aCurrentMethod, aCurrent.NextPosition), debugEnabled);
        }

        public static void DoExecute(XSharp.Assembler.Assembler Assembler, Il2cpuMethodInfo aCurrentMethod, MethodBase aTargetMethod, ILOpCode aOp, string currentLabel, string nextLabel, bool debugEnabled)
        {
            var xMethodInfo = aTargetMethod as MethodInfo;
            string xNormalAddress = LabelName.Get(aTargetMethod);
            if (PlugManager.DirectPlugMapping.ContainsKey(LabelName.GetFullName(aTargetMethod)))
            {
                string xPlugAddress = LabelName.Get(PlugManager.DirectPlugMapping[LabelName.GetFullName(aTargetMethod)]);
                XS.Comment($"Redirecting call to {xNormalAddress} directly to plug {xPlugAddress}");
                xNormalAddress = xPlugAddress;
            }
            var xParameters = aTargetMethod.GetParameters();

            // todo: implement exception support
            uint xExtraStackSize = GetStackSizeToReservate(aTargetMethod);
            if (!aTargetMethod.IsStatic)
            {
                uint xThisOffset = 0;
                foreach (var xItem in xParameters)
                {
                    xThisOffset += Align(SizeOfType(xItem.ParameterType), 4);
                }
                var stackOffsetToCheck = xThisOffset;
                if (IsReferenceType(aTargetMethod.DeclaringType))
                {
                    DoNullReferenceCheck(Assembler, debugEnabled, (int)stackOffsetToCheck + 4);
                }
                else
                {
                    DoNullReferenceCheck(Assembler, debugEnabled, (int)stackOffsetToCheck);
                }
            }

            if (xExtraStackSize > 0)
            {
                XS.Sub(XSRegisters.ESP, xExtraStackSize);
            }
            XS.Call(xNormalAddress);

            if (aCurrentMethod != null)
            {
                uint xReturnSize = 0;
                if (xMethodInfo != null)
                {
                    xReturnSize = SizeOfType(xMethodInfo.ReturnType);
                }
                EmitExceptionLogic(Assembler, aCurrentMethod, aOp, true,
                                   delegate
                                   {
                                       var xStackOffsetBefore = aOp.StackOffsetBeforeExecution.Value;

                                       uint xPopSize = 0;
                                       foreach (var type in aOp.StackPopTypes)
                                       {
                                           xPopSize += Align(SizeOfType(type), 4);
                                       }

                                       var xResultSize = xReturnSize;
                                       if (xResultSize % 4 != 0)
                                       {
                                           xResultSize += 4 - xResultSize % 4;
                                       }

                                       ILOp.EmitExceptionCleanupAfterCall(Assembler, xResultSize, xStackOffsetBefore, xPopSize);
                                   }, nextLabel);

            }
        }
    }
}
