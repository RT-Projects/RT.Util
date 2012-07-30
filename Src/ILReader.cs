using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    public static class ILReader
    {
        private static Dictionary<short, OpCode> _opCodeList = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode) f.GetValue(null)).ToDictionary(o => o.Value);

        public static IEnumerable<Instruction> ReadIL(MethodBase method, Type genericContext)
        {
            MethodBody body = method.GetMethodBody();
            if (body == null)
                yield break;

            int offset = 0;
            byte[] il = body.GetILAsByteArray();
            while (offset < il.Length)
            {
                int startOffset = offset;
                byte opCodeByte = il[offset];
                short opCodeValue = opCodeByte;
                offset++;

                // If it's an extended opcode then grab the second byte. The 0xFE prefix codes aren't marked as prefix operators though.
                if (opCodeValue == 0xFE || _opCodeList[opCodeValue].OpCodeType == OpCodeType.Prefix)
                {
                    opCodeValue = (short) ((opCodeValue << 8) + il[offset]);
                    offset++;
                }

                OpCode code = _opCodeList[opCodeValue];

                object operand = null;
                switch (code.OperandType)
                {
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        operand = il[offset];
                        offset++;
                        break;

                    case OperandType.InlineI:
                    case OperandType.InlineBrTarget:
                        operand = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        break;

                    case OperandType.ShortInlineR: operand = BitConverter.ToSingle(il, offset); offset += 4; break;
                    case OperandType.InlineVar: operand = BitConverter.ToInt16(il, offset); offset += 2; break;
                    case OperandType.InlineI8: operand = BitConverter.ToInt64(il, offset); offset += 8; break;
                    case OperandType.InlineR: operand = BitConverter.ToDouble(il, offset); offset += 8; break;

                    case OperandType.InlineField:
                        try
                        {
                            operand = method.Module.ResolveField(BitConverter.ToInt32(il, offset), genericContext.GetGenericArguments(), method.GetGenericArguments());
                        }
                        catch (ArgumentException e)
                        {
                            throw new Exception("{0} / {1}".Fmt(genericContext, method, e.Message));
                        }
                        offset += 4;
                        break;
                    case OperandType.InlineMethod: operand = method.Module.ResolveMethod(BitConverter.ToInt32(il, offset), genericContext.GetGenericArguments(), method.GetGenericArguments()); offset += 4; break;
                    case OperandType.InlineSig: operand = method.Module.ResolveSignature(BitConverter.ToInt32(il, offset)); offset += 4; break;
                    case OperandType.InlineString: operand = method.Module.ResolveString(BitConverter.ToInt32(il, offset)); offset += 4; break;
                    case OperandType.InlineType: operand = method.Module.ResolveType(BitConverter.ToInt32(il, offset), genericContext.GetGenericArguments(), method.GetGenericArguments()); offset += 4; break;

                    case OperandType.InlineSwitch:
                        long num = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        var ints = new int[num];
                        for (int i = 0; i < num; i++)
                        {
                            ints[i] = BitConverter.ToInt32(il, offset);
                            offset += 4;
                        }
                        operand = ints;
                        break;

                    case OperandType.InlineTok:
                        operand = method.Module.ResolveMember(BitConverter.ToInt32(il, offset), genericContext.GetGenericArguments(), method.GetGenericArguments()); offset += 4; break;
                }

                yield return new Instruction(startOffset, code, operand);
            }
        }
    }

    public sealed class Instruction
    {
        public int StartOffset { get; private set; }
        public OpCode OpCode { get; private set; }
        public object Operand { get; private set; }
        public Instruction(int startOffset, OpCode opCode, object operand)
        {
            StartOffset = startOffset;
            OpCode = opCode;
            Operand = operand;
        }
        public override string ToString()
        {
            return OpCode.ToString() + (Operand == null ? string.Empty : " " + Operand.ToString());
        }
    }
}
