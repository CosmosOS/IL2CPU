using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU.ILOpCodes;
using static Cosmos.IL2CPU.ILOpCode;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.Extensions
{
    public static class InvocationExtensions
    {
        public static object ReInvoke(this MethodBase metaMethod, object obj, object[] parameters)
        {
            var reader = new ILReader();
            var stack = new Stack<object>();
            var codes = reader.ProcessMethod(metaMethod);
            var locals = new Dictionary<int, LocalVar>();
            foreach (var local in metaMethod.GetLocalVariables())
            {
                var localType = ReplaceLoad(local.LocalType);
                locals[local.LocalIndex] = new LocalVar { VarType = localType };
            }
            foreach (var code in codes)
            {
                var op = code.OpCode;
                switch (op)
                {
                    case Code.Nop:
                        break;
                    case Code.Ldsfld:
                        var fieldInfo = ((OpField)code).Value;
                        var fType = ReplaceLoad(fieldInfo.DeclaringType);
                        var field = fType.GetField(fieldInfo.Name);
                        var fVal = field.GetValue(null);
                        stack.Push(fVal);
                        break;
                    case Code.Ldc_I4:
                        stack.Push((int)0);
                        break;
                    case Code.Ldloca:
                        var loc = (int)((OpVar)code).Value;
                        stack.Push(locals[loc]);
                        break;
                    case Code.Initobj:
                        var addr = (LocalVar)stack.Pop();
                        addr.VarValue = GetDefaultVal(addr.VarType);
                        break;
                    case Code.Ldloc:
                        stack.Push(locals[0].VarValue);
                        break;
                    case Code.Ldstr:
                        stack.Push(((OpString)code).Value);
                        break;
                    case Code.Newobj:
                        var nType = ReplaceLoad(code.StackPushTypes[0]);
                        var newArgs = stack.Reverse().ToArray();
                        var newObj = Activator.CreateInstance(nType, newArgs);
                        stack.Clear();
                        stack.Push(newObj);
                        break;
                    case Code.Call:
                        var callBase = ((OpMethod)code).Value;
                        var realMethod = ReplaceLoad(callBase);
                        var callArgs = stack.Reverse().ToArray();
                        callArgs = ConvertTypes(realMethod.GetParameters(), callArgs);
                        var retObj = realMethod.Invoke(null, callArgs);
                        stack.Clear();
                        if (callBase is MethodInfo cm && cm.ReturnType != BaseTypes.Void)
                            stack.Push(retObj);
                        break;
                    case Code.Pop:
                        stack.Pop();
                        break;
                    case Code.Ret:
                        return null;
                    default:
                        throw new InvalidOperationException(code + " ?");
                }
            }
            throw new InvalidOperationException(metaMethod + " " + obj + " " + parameters);
        }

        private static object[] ConvertTypes(ParameterInfo[] parameters, object[] callArgs)
        {
            var args = new object[callArgs.Length];
            for (var i = 0; i < callArgs.Length; i++)
            {
                var pType = parameters[i].ParameterType;
                var arg = callArgs[i];
                if (pType.IsEnum)
                {
                    arg = Enum.Parse(pType, arg + "");
                }
                else if (arg is IConvertible)
                {
                    arg = Convert.ChangeType(arg, pType);
                }
                args[i] = arg;
            }
            return args;
        }

        private static MethodBase ReplaceLoad(MethodBase m)
        {
            var owner = ReplaceLoad(m.DeclaringType);
            var all = owner.GetMethods().Concat<MethodBase>(owner.GetConstructors());
            var methods = all.Where(x => x.Name == m.Name &&
                                         x.GetParameters().Length == m.GetParameters().Length).ToArray();
            MethodBase method;
            if (methods.Length == 1)
            {
                method = methods[0];
            }
            else
            {
                var pTypes = m.GetParameters().Select(p => ReplaceLoad(p.ParameterType)).ToArray();
                method = owner.GetMethod(m.Name, pTypes);
            }
            return method;
        }

        private static Type ReplaceLoad(Type type)
        {
            var fqn = type.FullName + ", " + type.Assembly.GetName().Name;
            // TODO : HACK for XSharp
            const string tmp = "XSharp, Version=1.0.0.0";
            if (fqn.Contains(tmp))
                fqn = fqn.Replace(tmp, "XSharp, Version=0.1.0.0");
            var rtType = Type.GetType(fqn);
            return rtType;
        }

        #region Default values
        public static T GetDefault<T>() => default;

        private static readonly MethodInfo def = typeof(InvocationExtensions).GetMethod(nameof(GetDefault));

        private static object GetDefaultVal(Type t) => def.MakeGenericMethod(t).Invoke(null, null);
        #endregion

        private class LocalVar
        {
            public object VarValue;
            public Type VarType;

            public override string ToString() => VarValue + " [" + VarType + "]";
        }
    }
}
