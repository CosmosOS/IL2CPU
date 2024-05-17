using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cosmos.IL2CPU.ILOpCodes;

namespace Cosmos.IL2CPU.Optimization.Passes
{
    /// <summary>
    /// Inlines direct property accesses. A direct property access is a call
    /// to a getter/setter method that directly gets or sets an underlying field,
    /// which can be simplified to a single instruction.
    /// </summary>
    internal class InlineDirectPropertiesPass : OptimizerPass
    {
        // Control value to cancel optimization by look-ahead provider
        bool processing = false;

        public override List<ILOpCode> Process(List<ILOpCode> il)
        {
            if (processing) {
                return il;
            }

            processing = true;

            var callsToInline = Enumerable.Range(0, il.Count)
                // Select both the index and the instruction under that index.
                .Select(i => (idx: i, inst: il[i]))
                // Filter to only call instructions from which we can get a MethodBase.
                .Where(x => x.inst.OpCode == ILOpCode.Code.Call && x.inst is OpMethod)
                // Transform to (int, OpMethod).
                .Select(x => (x.idx, inst: (OpMethod)x.inst))
                // Ensure that inst.Value is not null.
                .Where(x => x.inst.Value != null)
                // Apply a look-ahead to see what method this instruction wants to call.
                .Select(x => (x.idx, x.inst, body: Owner.LookaheadProvider.ProcessMethodAhead(x.inst.Value)))
                // Some methods return null bodies; filter them out.
                .Where(x => x.body != null)
                // Find the inline instruction replacements for the method calls, now that we have the body of the method its trying to call.
                .Select(x => (x.idx, x.inst, x.body, inline: GetPropertyInlineReplacement(x.inst.Value, x.body)))
                // If there is no inline replacement, don't include it in the list.
                .Where(x => x.inline != null);

            foreach (var x in callsToInline) {
                // Create a new OpField that fits into our sequence
                // OpField and OpMethod should have the same length, so we can just replace Position and NextPosition
                // with the OpMethod instructions we are replacing
                var inline = new OpField(
                    x.inline.OpCode,
                    il[x.idx].Position, il[x.idx].NextPosition,
                    x.inline.Value,
                    x.inline.CurrentExceptionRegion
                );

                il[x.idx] = inline;
            }

            processing = false;
            return il;
        }


        /// <summary>
        /// Determines whether the provided method body is a direct property accessor
        /// and returns the field target instruction. 
        /// </summary>
        private static OpField GetPropertyInlineReplacement(MethodBase info, List<ILOpCode> il)
        {
            var filtered = il.Where(x => x.OpCode != ILOpCode.Code.Nop)
                             .ToArray();

            if (info is not MethodInfo moreInfo) {
                // The method information isn't MethodInfo... this shouldn't happen!
                return null;
            }

            bool setter = false;
            if(moreInfo.ReturnType == typeof(void) && info.GetParameters().Length == 1) {
                setter = true;
            } else if(moreInfo.GetParameters().Length != 0){
                // Not a setter, and accepts parameters; not a property
                return null;
            }

            if (info.IsStatic) {
                if(setter) {
                    var isDirectAccess =
                        filtered.Length >= 3 &&
                        filtered[0].OpCode == ILOpCode.Code.Ldarg && ((OpVar)filtered[0]).Value == 0 &&
                        filtered[1].OpCode == ILOpCode.Code.Stsfld &&
                        filtered[2].OpCode == ILOpCode.Code.Ret;

                    if(isDirectAccess) {
                        // The property loads argument 0 (value), that is in turn taken from the stack.
                        // The field then is changed with the value on the stack and returns; replace the call
                        // with the stsfld instruction (that sets the field).
                        return (OpField)filtered[1];
                    }
                } else {
                    var isDirectAccess =
                        filtered.Length >= 2 &&
                        filtered[0].OpCode == ILOpCode.Code.Ldsfld &&
                        filtered[1].OpCode == ILOpCode.Code.Ret;

                    if (isDirectAccess) {
                        // The property pushes the field on the stack and returns; replace the call
                        // with the ldsfld instruction (that pushes the field).
                        return (OpField)filtered[0];
                    }
                }
            }
            else {
                if(setter) {
                    var isDirectAccess =
                        filtered.Length >= 4 &&
                        filtered[0].OpCode == ILOpCode.Code.Ldarg && ((OpVar)filtered[0]).Value == 0 &&
                        filtered[1].OpCode == ILOpCode.Code.Ldarg && ((OpVar)filtered[1]).Value == 1 &&
                        filtered[2].OpCode == ILOpCode.Code.Stfld &&
                        filtered[3].OpCode == ILOpCode.Code.Ret;

                    if(isDirectAccess) {
                        // The property pushes two arguments onto the stack; the instance pointer
                        // and the value, and calls stfld. In order to call this method, the caller
                        // must also push the target instance pointer (even if the caller is the object
                        // that the property belongs to; in that case, it would do "ldarg.0") and the
                        // value. The stack layout does not change, meaning we can directly replace the call.
                        return (OpField)filtered[2];
                    }
                } else {
                    var isDirectAccess =
                        filtered.Length >= 3 &&
                        filtered[0].OpCode == ILOpCode.Code.Ldarg && ((OpVar)filtered[0]).Value == 0 &&
                        filtered[1].OpCode == ILOpCode.Code.Ldfld &&
                        filtered[2].OpCode == ILOpCode.Code.Ret;

                    if(isDirectAccess) {
                        // The property loads argument 0 (self pointer), executes the instruction
                        // ldfld, and returns. In order to call the property, we already push argument 0,
                        // meaning we can direclty replace the call.
                        return (OpField)filtered[1];
                    }
                }
            }

            // No instruction that can be used to directly replace the call
            return null;
        }
    }
}
