using IL2CPU.API;
using System;
using System.Linq;
using CPU = XSharp.Assembler;
using System.Text;
using XSharp.Assembler;
using Cosmos.IL2CPU.ILOpCodes;
using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldstr)]
    public class LdStr : ILOp
    {
        public LdStr(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpString = aOpCode as OpString;
            string xDataName = GetContentsArrayName(xOpString.Value);
            XS.Comment("String Value: \"" + xOpString.Value.Replace("\r", "\\r").Replace("\n", "\\n") + "\"");
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

        public static string GetContentsArrayName(string aLiteral)
        {
            var xAsm = CPU.Assembler.CurrentInstance;

            Encoding xEncoding = Encoding.Unicode;

            string xDataName = xAsm.GetIdentifier("StringLiteral");
            var xBytecount = xEncoding.GetByteCount(aLiteral);
            var xObjectData = new byte[(4 * 4) + (xBytecount)];
            Array.Copy(BitConverter.GetBytes((int)-1), 0, xObjectData, 0, 4);
            Array.Copy(BitConverter.GetBytes((uint)ObjectUtils.InstanceTypeEnum.StaticEmbeddedObject), 0, xObjectData, 4, 4);
            Array.Copy(BitConverter.GetBytes((int)1), 0, xObjectData, 8, 4);
            Array.Copy(BitConverter.GetBytes(aLiteral.Length), 0, xObjectData, 12, 4);
            Array.Copy(xEncoding.GetBytes(aLiteral), 0, xObjectData, 16, xBytecount);
            xAsm.DataMembers.Add(new DataMember(xDataName, xObjectData));
            return xDataName;
        }
    }
}
