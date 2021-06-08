using Cosmos.IL2CPU.ILOpCodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldarg)]
  public class Ldarg : ILOp
  {
    public Ldarg(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpVar = (OpVar)aOpCode;
      DoExecute(Assembler, aMethod, xOpVar.Value);
    }

    /// <summary>
    /// <para>This methods gives the full displacement for an argument. Arguments are in "reverse" order:
    /// <code>public static int Add(int a, int b)</code>
    /// In this situation, argument b is at EBP+8, argument A is at EBP+12
    /// </para>
    /// <para>
    /// After the method returns, the return value is on the stack. This means, that when the return size is larger than the
    /// total argument size, we need to reserve extra stack:
    /// <code>public static Int64 Convert(int value)</code>
    /// In this situation, argument <code>value</code> is at EBP+12
    /// </para>
    /// </summary>
    /// <param name="aMethod"></param>
    /// <param name="aIndex"></param>
    /// <returns></returns>
    public static int GetArgumentDisplacement(_MethodInfo aMethod, ushort aIndex)
    {
      var xMethodBase = aMethod.MethodBase;
      if (aMethod.PluggedMethod != null)
      {
        xMethodBase = aMethod.PluggedMethod.MethodBase;
      }
      var xMethodInfo = xMethodBase as MethodInfo;
      var xDeclaringType = xMethodBase.DeclaringType;

      Type xReturnType = null;
      if (xMethodInfo != null)
      {
        xReturnType = xMethodInfo.ReturnType;
      }

      bool xIsStatic = aMethod.MethodBase.IsStatic;
      var xParameterTypes = xMethodBase.GetParameters().Select(p => p.ParameterType).ToArray();

      return GetArgumentDisplacement(aIndex, xDeclaringType, xParameterTypes, xReturnType, xIsStatic);
    }

    public static int GetArgumentDisplacement(ushort aIndex, Type aDeclaringType, Type[] aParameterTypes, Type aReturnType, bool aIsStatic)
    {
      uint xReturnSize = 0;
      if (aReturnType != null)
      {
        xReturnSize = Align(SizeOfType(aReturnType), 4);
      }

      uint xOffset = 8;
      var xCorrectedOpValValue = aIndex;
      if (!aIsStatic && aIndex > 0)
      {
        // if the method has a $this, the OpCode value includes the this at index 0, but GetParameters() doesnt include the this
        xCorrectedOpValValue -= 1;
      }
      var xParams = aParameterTypes;

      uint xArgSize = 0;
      foreach (var xParam in xParams)
      {
        xArgSize += Align(SizeOfType(xParam), 4);
      }
      if (!aIsStatic)
      {
        // Add $this pointer
        if (aDeclaringType.IsValueType)
        {
          // value types get a reference passed to the actual value, so pointer:
          xArgSize += 4;
        }
        else
        {
          xArgSize += Align(SizeOfType(aDeclaringType), 4);
        }
      }

      uint xCurArgSize;
      if (aIndex == 0 && !aIsStatic)
      {
        // return the this parameter, which is not in .GetParameters()
        if (aDeclaringType.IsValueType)
        {
          // value types get a reference passed to the actual value, so pointer:
          xCurArgSize = 4;
        }
        else
        {
          xCurArgSize = Align(SizeOfType(aDeclaringType), 4);
        }

        for (int i = xParams.Length - 1; i >= aIndex; i--)
        {
          var xSize = Align(SizeOfType(xParams[i]), 4);
          xOffset += xSize;
        }

      }
      else
      {
        xCurArgSize = Align(SizeOfType(xParams[xCorrectedOpValValue]), 4);

        for (int i = xParams.Length - 1; i > xCorrectedOpValValue; i--)
        {
          var xSize = Align(SizeOfType(xParams[i]), 4);
          xOffset += xSize;
        }
      }
      if (xReturnSize > xArgSize)
      {
        uint xExtraSize = xReturnSize - xArgSize;
        xOffset += xExtraSize;
      }

      return (int)(xOffset + xCurArgSize - 4);

    }

    public static void DoExecute(XSharp.Assembler.Assembler Assembler, _MethodInfo aMethod, ushort aParam)
    {
      var xDisplacement = GetArgumentDisplacement(aMethod, aParam);
      var xType = GetArgumentType(aMethod, aParam);
      uint xArgRealSize = SizeOfType(xType);
      uint xArgSize = Align(xArgRealSize, 4);

      XS.Comment("Arg idx = " + aParam);
      XS.Comment("Arg type = " + xType);
      XS.Comment("Arg real size = " + xArgRealSize + " aligned size = " + xArgSize);
      if (IsIntegerBasedType(xType) && xArgRealSize == 1 || xArgRealSize == 2)
      {
        if (TypeIsSigned(xType))
        {
          XS.MoveSignExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: xDisplacement, size: (RegisterSize)(8 * xArgRealSize));
        }
        else
        {
          XS.MoveZeroExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: xDisplacement, size: (RegisterSize)(8 * xArgRealSize));
        }

        XS.Push(EAX);
      }
      else
      {
        for (int i = 0; i < (xArgSize / 4); i++)
        {
          XS.Push(EBP, isIndirect: true, displacement: (xDisplacement - (i * 4)));
        }
      }
    }

    public static Type GetArgumentType(_MethodInfo aMethod, ushort aParam)
    {
      Type xArgType;
      if (aMethod.MethodBase.IsStatic)
      {
        xArgType = aMethod.MethodBase.GetParameters()[aParam].ParameterType;
      }
      else
      {
        if (aParam == 0u)
        {
          xArgType = aMethod.MethodBase.DeclaringType;
          if (xArgType.IsValueType)
          {
            xArgType = xArgType.MakeByRefType();
          }
        }
        else
        {
          xArgType = aMethod.MethodBase.GetParameters()[aParam - 1].ParameterType;
        }
      }

      return xArgType;
    }
  }
}
