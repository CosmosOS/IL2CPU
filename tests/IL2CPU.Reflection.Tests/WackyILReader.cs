using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace IL2CPU.Reflection.Tests
{
    internal class WackyILReader
    {
        private readonly byte[] _bytes;
        private int _ptr;

        public WackyILReader(MethodInfo method) : this(method.GetMethodBody()?.GetILAsByteArray())
        {
        }

        public WackyILReader(byte[] bytes)
        {
            _bytes = bytes;
            _ptr = 0;
        }

        public List<int> MethodTokens { get; } = new List<int>();
        public List<int> FieldTokens { get; } = new List<int>();
        public List<int> TypeTokens { get; } = new List<int>();
        public List<int> StringTokens { get; } = new List<int>();

        public bool Read()
        {
            if (_ptr < _bytes?.Length)
            {
                var opCode = ReadOpCode();
                ReadOperand(opCode);
                return true;
            }
            return false;
        }

        private OpCode ReadOpCode()
        {
            var instruction = ReadByte();
            if (instruction != 254 && instruction < singleByteOpCode.Length)
                return singleByteOpCode[instruction];
            var doubleInstr = ReadByte();
            if (doubleInstr < doubleByteOpCode.Length)
                return doubleByteOpCode[doubleInstr];
            return default;
        }

        private void ReadOperand(OpCode code)
        {
            switch (code.OperandType)
            {
                case OperandType.InlineField:
                    var fieldToken = ReadInt();
                    if (fieldToken != 0)
                        FieldTokens.Add(fieldToken);
                    break;
                case OperandType.InlineMethod:
                    var methodToken = ReadInt();
                    if (methodToken != 0)
                        MethodTokens.Add(methodToken);
                    break;
                case OperandType.InlineType:
                    var typeToken = ReadInt();
                    if (typeToken != 0)
                        TypeTokens.Add(typeToken);
                    break;
                case OperandType.InlineString:
                    var strToken = ReadInt();
                    if (strToken != 0)
                        StringTokens.Add(strToken);
                    break;
                case OperandType.InlineNone: break;
                case OperandType.InlineR: break;
                case OperandType.InlineI: break;
                case OperandType.InlineI8: break;
                case OperandType.InlineSig: break;
                case OperandType.InlineVar: break;
                case OperandType.ShortInlineBrTarget: break;
                case OperandType.InlineSwitch: break;
                case OperandType.InlineBrTarget: break;
                case OperandType.ShortInlineI: break;
                case OperandType.InlineTok: break;
                case OperandType.ShortInlineR: break;
                case OperandType.ShortInlineVar: break;
                default:
                    throw new InvalidOperationException(code + " " + code.OperandType);
            }
        }

        private byte ReadByte() => _bytes[_ptr++];

        private int ReadInt()
        {
            try
            {
                var b1 = ReadByte();
                var b2 = ReadByte();
                var b3 = ReadByte();
                var b4 = ReadByte();
                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
            }
            catch (IndexOutOfRangeException)
            {
                return default;
            }
        }

        static WackyILReader()
        {
            CreateOpCodes();
        }

        private static OpCode[] singleByteOpCode;
        private static OpCode[] doubleByteOpCode;

        private static void CreateOpCodes()
        {
            singleByteOpCode = new OpCode[225];
            doubleByteOpCode = new OpCode[31];
            var fields = GetOpCodeFields();
            for (var i = 0; i < fields.Length; i++)
            {
                var code = (OpCode)fields[i].GetValue(null);
                if (code.OpCodeType == OpCodeType.Nternal)
                    continue;
                if (code.Size == 1)
                    singleByteOpCode[code.Value] = code;
                else
                    doubleByteOpCode[code.Value & 0xff] = code;
            }
        }

        private static FieldInfo[] GetOpCodeFields()
            => typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
    }
}
