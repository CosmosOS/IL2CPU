using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

using IL2CPU.Debug.Symbols;
using IL2CPU.Debug.Symbols.Pdb;

using XSharp.Assembler;

namespace Cosmos.IL2CPU.Extensions
{
    internal static class MethodExtensions
    {
        public static string GetFullName(this MethodBase aMethod)
        {
            return LabelName.Get(aMethod);
        }

        public static IList<LocalVariableInfo> GetLocalVariables(this MethodBase aThis)
        {
            return DebugSymbolReader.GetLocalVariableInfos(aThis);
        }

        public static IEnumerable<_ExceptionRegionInfo> GetExceptionRegionInfos(this MethodBase aThis)
        {
            foreach (var x in aThis.GetMethodBody().ExceptionHandlingClauses)
            {
                yield return new _ExceptionRegionInfo(x);
            }
        }
    }
}
