using System;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.CIL.Utils;
using Cosmos.IL2CPU.Cosmos;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;
using CPU = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Callvirt)]
    public class Callvirt : ILOp
    {
        public Callvirt(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpMethod = aOpCode as OpMethod;
            DoExecute(Assembler, aMethod, xOpMethod.Value, xOpMethod.ValueUID, aOpCode, DebugEnabled);
        }

        public static void DoExecute(Assembler aAssembler, Il2cpuMethodInfo aMethod, MethodBase aTargetMethod, uint aTargetMethodUID, ILOpCode aOp, bool aDebugEnabled)
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

            // This is finding offset to self? It looks like we dont need offsets of other
            // arguments, but only self. If so can calculate without calculating all fields
            // Might have to go to old data structure for the offset...
            // Can we add this method info somehow to the data passed in?
            // mThisOffset = mTargetMethodInfo.Arguments[0].Offset;
            XS.Comment($"Calling = {aTargetMethod.DeclaringType.FullName}.{aTargetMethod.Name}");
            XS.Comment("ThisOffset = " + xThisOffset);

            if (IsReferenceType(xPopType))
            {
                DoNullReferenceCheck(aAssembler, aDebugEnabled, xThisOffset + 4);
            }
            else
            {
                DoNullReferenceCheck(aAssembler, aDebugEnabled, xThisOffset);
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
                    XS.Set(EBX, EAX, sourceDisplacement: 4, sourceIsIndirect: true); // type of object
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
                    // Now check if type derives from array
                    XS.Compare(EBX, (uint)ObjectUtils.InstanceTypeEnum.Array);
                    XS.Jump(CPU.ConditionalTestEnum.NotEqual, notArrayLabel);
                    XS.Comment($"Set type to be {aTargetMethod.DeclaringType.GenericTypeArguments[0].MakeArrayType().Name}");
                    XS.Push(GetTypeIDLabel(aTargetMethod.DeclaringType.GenericTypeArguments[0].MakeArrayType()), isIndirect: true);
                    XS.Jump(endOfCheckLabel);
                    XS.Label(notArrayLabel); // we already pushed that value when it does not need to be overwritten
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
                    XS.Set(EAX, ESP, sourceDisplacement: xThisOffset + 4);
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

                    // we need to determine if we actually want to unbox the object, we do this here so the code isnt run too often
                    if (aTargetMethod.DeclaringType.IsInterface)
                    {
                        // always unbox in this case
                    }
                    else
                    {
                        XS.Push(EAX); // we will need this eax again
                        XS.Push(ECX); // the call will trash ecx
                        XS.Set(EAX, ESP, sourceDisplacement: xThisOffset + 12);
                        XS.Push(EAX, isIndirect: true);

                        XS.Push(aTargetMethodUID);

                        XS.Call(LabelName.Get(VTablesImplRefs.GetDeclaringTypeOfMethodForTypeRef));

                        XS.Pop(EBX);
                        XS.Pop(ECX); // recover after the call
                        XS.Pop(EAX);

                        XS.Compare(EBX, VTablesImplRefs.GetTypeId(typeof(object)));
                        XS.Jump(CPU.ConditionalTestEnum.Equal, xCurrentMethodLabel + ".NotBoxedThis");
                        XS.Compare(EBX, VTablesImplRefs.GetTypeId(typeof(ValueType)));
                        XS.Jump(CPU.ConditionalTestEnum.Equal, xCurrentMethodLabel + ".NotBoxedThis");
                        XS.Compare(EBX, VTablesImplRefs.GetTypeId(typeof(Enum)));
                        XS.Jump(CPU.ConditionalTestEnum.Equal, xCurrentMethodLabel + ".NotBoxedThis");
                    }


                    /*
                    * On the stack now:
                    * $esp                 Params
                    * $esp + xThisOffset   This
                    *
                    * ECX contains the method to call
                    * EAX contains the type pointer (not the handle!!)
                    */
                    XS.Add(EAX, ObjectUtils.FieldDataOffset);
                    XS.Set(ESP, EAX, destinationDisplacement: xThisOffset + 4);

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
                    XS.Label(xCurrentMethodLabel + ".NotBoxedThis");
                }


                if (xExtraStackSize > 0)
                {
                    XS.Sub(ESP, xExtraStackSize);
                }

                XS.Call(ECX);
                XS.Label(afterCall);
            }
            EmitExceptionLogic(aAssembler, aMethod, aOp, true,
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

                    EmitExceptionCleanupAfterCall(aAssembler, xResultSize, xStackOffsetBefore, xPopSize);
                });
            XS.Label(xCurrentMethodLabel + ".NoExceptionAfterCall");
        }
    }
}
