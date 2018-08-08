using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection
{
    public class ILReader
    {
        private static readonly OpCode[] OpCodesLow = new OpCode[256];
        private static readonly OpCode[] OpCodesHigh = new OpCode[256];

        private readonly ModuleInfo _module;

        private readonly GenericContext _genericContext;
        private readonly MethodBody _methodBody;

        static ILReader()
        {
            foreach (var field in typeof(OpCodes).GetTypeInfo().DeclaredFields)
            {
                var opCode = (OpCode)field.GetValue(null);
                var value = (ushort)opCode.Value;

                if (value <= 0xFF)
                {
                    OpCodesLow[value] = opCode;
                }
                else
                {
                    OpCodesHigh[value & 0xFF] = opCode;
                }
            }
        }

        internal ILReader(ModuleInfo module, GenericContext genericContext, MethodBody methodBody)
        {
            _module = module;

            _genericContext = genericContext;
            _methodBody = methodBody;
        }

        public void ReadIL(IILVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            var ilReader = _methodBody.GetILBlobReader();

            while(ilReader.RemainingBytes > 0)
            {
                var pos = ilReader.Offset;

                var curByte = ilReader.ReadByte();

                var opCode = curByte == 0xFE ? OpCodesHigh[ilReader.ReadByte()] : OpCodesLow[curByte];
                var opCodeValue = (ILOpCode)opCode.Value;

                var opPos = ilReader.Offset;

                switch (opCode.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        visitor.OnInlineBrTarget(opCodeValue, pos, opPos + 4, ilReader.ReadInt32() + ilReader.Offset);
                        break;
                    case OperandType.InlineField:

                        var field = _module.ResolveFieldHandle(MetadataTokens.EntityHandle(ilReader.ReadInt32()), _genericContext);
                        visitor.OnInlineField(opCodeValue, pos, opPos + 4, field);

                        break;

                    case OperandType.InlineI:
                        visitor.OnInlineI(opCodeValue, pos, opPos + 4, ilReader.ReadInt32());
                        break;
                    case OperandType.InlineI8:
                        visitor.OnInlineI8(opCodeValue, pos, opPos + 8, ilReader.ReadInt64());
                        break;
                    case OperandType.InlineMethod:

                        var method = _module.ResolveMethodHandle(MetadataTokens.EntityHandle(ilReader.ReadInt32()), _genericContext);
                        visitor.OnInlineMethod(opCodeValue, pos, opPos + 4, method);

                        break;

                    case OperandType.InlineNone:
                        visitor.OnInlineNone(opCodeValue, pos, opPos);
                        break;
                    case OperandType.InlineR:
                        visitor.OnInlineR(opCodeValue, pos, opPos + 8, ilReader.ReadDouble());
                        break;
                    case OperandType.InlineSig:

                        var standaloneSignature = _module.MetadataReader.GetStandaloneSignature(
                            (StandaloneSignatureHandle)MetadataTokens.EntityHandle(ilReader.ReadInt32()));

                        var signature = standaloneSignature.DecodeMethodSignature(
                            _module.TypeProvider, _genericContext);

                        visitor.OnInlineSig(opCodeValue, pos, opPos + 4, signature);

                        break;

                    case OperandType.InlineString:

                        var value = _module.MetadataReader.GetUserString(
                            (UserStringHandle)MetadataTokens.Handle(ilReader.ReadInt32()));
                        visitor.OnInlineString(opCodeValue, pos, opPos + 4, value);

                        break;

                    case OperandType.InlineSwitch:

                        var length = ilReader.ReadUInt32();
                        var nextPos = (int)(opPos + (length + 1) * 4);

                        var builder = ImmutableArray.CreateBuilder<int>();

                        for (uint i = 0; i < length; i++)
                        {
                            builder.Add(nextPos + ilReader.ReadInt32());
                        }

                        visitor.OnInlineSwitch(opCodeValue, pos, nextPos, builder.ToImmutable());

                        break;

                    case OperandType.InlineTok:
                        visitor.OnInlineTok(opCodeValue, pos, opPos + 4, ilReader.ReadInt32(),
                            _module, _genericContext.TypeArguments, _genericContext.MethodArguments);
                        break;
                    case OperandType.InlineType:

                        var type = _module.ResolveTypeHandle(MetadataTokens.EntityHandle(ilReader.ReadInt32()), _genericContext);
                        visitor.OnInlineType(opCodeValue, pos, opPos + 4, type);

                        break;

                    case OperandType.InlineVar:
                        visitor.OnInlineVar(opCodeValue, pos, opPos + 2, ilReader.ReadUInt16());
                        break;
                    case OperandType.ShortInlineBrTarget:
                        visitor.OnShortInlineBrTarget(opCodeValue, pos, opPos + 1, ilReader.ReadSByte() + ilReader.Offset);
                        break;
                    case OperandType.ShortInlineI:
                        visitor.OnShortInlineI(opCodeValue, pos, opPos + 1, ilReader.ReadByte());
                        break;
                    case OperandType.ShortInlineR:
                        visitor.OnShortInlineR(opCodeValue, pos, opPos + 4, ilReader.ReadSingle());
                        break;
                    case OperandType.ShortInlineVar:
                        visitor.OnShortInlineVar(opCodeValue, pos, opPos + 1, ilReader.ReadByte());
                        break;
                }
            }
        }
    }
}
