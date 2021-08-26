using System;
using System.Linq;
using System.Reflection;

using Cosmos.IL2CPU.ILOpCodes;

using IL2CPU.API;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;
using CPU = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Callvirt)]
    public class Callvirt : ILOp
    {
        public Callvirt(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpMethod = aOpCode as OpMethod;
            DoExecute(Assembler, aMethod, xOpMethod.Value, xOpMethod.ValueUID, aOpCode, DebugEnabled);
        }

        public static void DoExecute(Assembler Assembler, _MethodInfo aMethod, MethodBase aTargetMethod, uint aTargetMethodUID,
            ILOpCode aOp, bool debugEnabled)
        {
            string xCurrentMethodLabel = GetLabel(aMethod, aOp.Position);
            Type xPopType = aOp.StackPopTypes.Last();
            var isInterface = aTargetMethod.DeclaringType.IsInterface;

            string xNormalAddress = "";
            if (aTargetMethod.IsStatic || !aTargetMethod.IsVirtual || aTargetMethod.IsFinal)
            {
                xNormalAddress = LabelName.Get(aTargetMethod);
            }

            uint xReturnSize = 0;
            var xMethodInfo = aTargetMethod as MethodInfo;
            if (xMethodInfo != null)
            {
                xReturnSize = Align(SizeOfType(xMethodInfo.ReturnType), 4);
            }

            var xExtraStackSize = Call.GetStackSizeToReservate(aTargetMethod, xPopType);
            int xThisOffset = 0;
            var xParameters = aTargetMethod.GetParameters();
            foreach (var xItem in xParameters)
            {
                xThisOffset += (int)Align(SizeOfType(xItem.ParameterType), 4);
            }

            XS.Comment($"Calling = {aTargetMethod.DeclaringType.FullName}.{aTargetMethod.Name}");
            XS.Comment("ThisOffset = " + xThisOffset);

            if (IsReferenceType(xPopType))
            {
                DoNullReferenceCheck(Assembler, debugEnabled, xThisOffset + 4);
            }
            else
            {
                DoNullReferenceCheck(Assembler, debugEnabled, xThisOffset);
            }

            if (!String.IsNullOrEmpty(xNormalAddress))
            {
                if (xExtraStackSize > 0)
                {
                    XS.Sub(ESP, xExtraStackSize);
                }

                XS.Call(xNormalAddress);
            }
            else
            {
                var afterCall = xCurrentMethodLabel + ".AfterCall";

                /*
                * On the stack now:
                * $esp                 Params
                * $esp + xThisOffset   This
                */
                if (xPopType.IsPointer || xPopType.IsByRef)
                {
                    xPopType = xPopType.GetElementType();
                    XS.Push(GetTypeIDLabel(xPopType), isIndirect: true);
                }
                else
                {
                    XS.Set(EAX, ESP, sourceDisplacement: xThisOffset + 4);
                    XS.Push(EAX, isIndirect: true);
                }

                // To handle generic interfaces on arrays, we need to check if the callvirt is for such an interface, in which case if the object
                // the method is being called on is an array, we dont push the type array but rather an typed array (System.Array vs T[])

                if (aTargetMethod.DeclaringType.IsGenericType
                    && new string[] { "IList", "ICollection", "IEnumerable", "IReadOnlyList", "IReadOnlyCollection" }
                        .Any(i => aTargetMethod.DeclaringType.Name.Contains(i)))
                {
                    isInterface = true;
                    var notArrayLabel = xCurrentMethodLabel + ".NotArrayType";
                    var endOfCheckLabel = xCurrentMethodLabel + ".AfterGenericArrayInterfaceCheck";
                    XS.Pop(EAX); // EAX now contains type of object
                    XS.Set(EBX, GetTypeIDLabel(typeof(Array)), sourceIsIndirect: true);
                    XS.Compare(EAX, EBX);
                    XS.Jump(CPU.ConditionalTestEnum.NotEqual, notArrayLabel);
                    XS.Comment($"Set type to be {aTargetMethod.DeclaringType.GenericTypeArguments[0].MakeArrayType().Name}");
                    XS.Push(GetTypeIDLabel(aTargetMethod.DeclaringType.GenericTypeArguments[0].MakeArrayType()), isIndirect: true);
                    XS.Jump(endOfCheckLabel);
                    XS.Label(notArrayLabel);
                    XS.Push(EAX);
                    XS.Label(endOfCheckLabel);
                }

                XS.Push(aTargetMethodUID);

                if (isInterface)
                {
                    XS.Call(LabelName.Get(VTablesImplRefs.GetMethodAddressForInterfaceTypeRef));
                }
                else
                {
                    XS.Call(LabelName.Get(VTablesImplRefs.GetMethodAddressForTypeRef));
                }

                if (xExtraStackSize > 0)
                {
                    xThisOffset -= (int)xExtraStackSize;
                }

                /*
                 * On the stack now:
                 * $esp                 Params
                 * $esp + xThisOffset   This
                 */
                XS.Pop(ECX);

                XS.Label(xCurrentMethodLabel + ".AfterAddressCheck");

                if (IsReferenceType(xPopType))
                {
                    /*
                    * On the stack now:
                    * $esp + 0              Params
                    * $esp + xThisOffset    This
                    */
                    // we need to see if $this is a boxed object, and if so, we need to unbox it
                    XS.Set(EAX, ESP, sourceDisplacement: (int)xThisOffset + 4);
                    XS.Compare(EAX, (int)ObjectUtils.InstanceTypeEnum.BoxedValueType, destinationIsIndirect: true, destinationDisplacement: 4, size: RegisterSize.Int32);

                    /*
                    * On the stack now:
                    * $esp                 Params
                    * $esp + xThisOffset   This
                    *
                    * ECX contains the method to call
                    * EAX contains the type pointer (not the handle!!)
                    */
                    XS.Jump(CPU.ConditionalTestEnum.NotEqual, xCurrentMethodLabel + ".NotBoxedThis");

                    /*
                    * On the stack now:
                    * $esp                 Params
                    * $esp + xThisOffset   This
                    *
                    * ECX contains the method to call
                    * EAX contains the type pointer (not the handle!!)
                    */
                    XS.Add(EAX, ObjectUtils.FieldDataOffset);
                    XS.Set(ESP, EAX, destinationDisplacement: (int)xThisOffset + 4);

                    var xHasParams = xThisOffset != 0;
                    var xNeedsExtraStackSize = xReturnSize >= xThisOffset + 8;

                    if (xHasParams || !xNeedsExtraStackSize)
                    {
                        XS.Add(ESP, (uint)(xThisOffset + 4));
                    }

                    for (int i = 0; i < xThisOffset / 4; i++)
                    {
                        XS.Push(ESP, displacement: -8);
                    }

                    if (xHasParams && xNeedsExtraStackSize)
                    {
                        XS.Sub(ESP, 4);
                    }

                    /*
                    * On the stack now:
                    * $esp                 Params
                    * $esp + xThisOffset   Pointer to address inside box
                    *
                    * ECX contains the method to call
                    */
                }

                XS.Label(xCurrentMethodLabel + ".NotBoxedThis");

                if (xExtraStackSize > 0)
                {
                    XS.Sub(ESP, xExtraStackSize);
                }

                XS.Call(ECX);
                XS.Label(afterCall);
            }
            EmitExceptionLogic(Assembler, aMethod, aOp, true,
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
                        xResultSize += 4 - (xResultSize % 4);
                    }

                    EmitExceptionCleanupAfterCall(Assembler, xResultSize, xStackOffsetBefore, xPopSize);
                });
            XS.Label(xCurrentMethodLabel + ".NoExceptionAfterCall");
            XS.Comment("Argument Count = " + xParameters.Length);
        }
    }
}
