using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.CIL.Utils;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class LdStr : ILOp
    {
        private readonly Assembler assembler;

        public LdStr(Assembler aAssembler) : base(aAssembler)
        {
            assembler = aAssembler;
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            string xValue = (aOpCode as OpString).Value;
            PushString(assembler, xValue);
        }

        /// <summary>
        /// Emit X# instructions to push a specified string value to the stack as an 64bit pointer
        /// </summary>
        /// <param name="xValue"></param>
        /// <param name="xDataName"></param>
        /// <exception cref="Exception"></exception>
        public static void PushString(Assembler aAssembler, string aValue)
        {
            string xDataName = GetContentsArrayName(aAssembler, aValue);
            XS.Comment("String Value: \"" + aValue.Replace("\r", "\\r").Replace("\n", "\\n") + "\"");
            XS.Push(xDataName);
            XS.Push(0);

            // DEBUG VERIFICATION: leave it here for now. we have issues with fields ordering.
            // if that changes, we need to change the code below!
            // We also need to change the debugstub to fix this then.
            #region Debug verification
            var xFields = GetFieldsInfo(typeof(string), false).Where(i => !i.IsStatic).ToArray();
            if (xFields[0].Id != "System.Int32 System.String._stringLength" || xFields[0].Offset != 0)
            {
                throw new Exception("Fields changed!");
            }
            if (xFields[1].Id != "System.Char System.String._firstChar" || xFields[1].Offset != 4)
            {
                throw new Exception("Fields changed!");
            }
            #endregion
        }

        static readonly Dictionary<string, string> stringLiterals = new Dictionary<string, string>();

        /// <summary>
        /// Create a new string literal to be included in the assembly
        /// </summary>
        /// <param name="assembler"></param>
        /// <param name="aLiteral"></param>
        /// <returns></returns>
        public static string GetContentsArrayName(Assembler assembler, string aLiteral)
        {
            // check if we already have this string literal emitted, if yes reuse it
            if(stringLiterals.TryGetValue(aLiteral, out string xDataName))
            {
                return xDataName;
            }
            Encoding xEncoding = Encoding.Unicode;

            xDataName = assembler.GetIdentifier("StringLiteral");
            var xBytecount = xEncoding.GetByteCount(aLiteral);
            var xObjectData = new byte[4 * 4 + (xBytecount)];
            Array.Copy(BitConverter.GetBytes(-1), 0, xObjectData, 0, 4);
            Array.Copy(BitConverter.GetBytes((uint)ObjectUtils.InstanceTypeEnum.StaticEmbeddedObject), 0, xObjectData, 4, 4);
            Array.Copy(BitConverter.GetBytes(1), 0, xObjectData, 8, 4);
            Array.Copy(BitConverter.GetBytes(aLiteral.Length), 0, xObjectData, 12, 4);
            Array.Copy(xEncoding.GetBytes(aLiteral), 0, xObjectData, 16, xBytecount);
            assembler.DataMembers.Add(new DataMember(xDataName, xObjectData));

            stringLiterals[aLiteral] = xDataName;


            return xDataName;
        }
    }
}
