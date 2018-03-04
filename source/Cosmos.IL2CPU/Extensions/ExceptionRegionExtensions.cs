using System;

using IL2CPU.Debug.Symbols;

namespace Cosmos.IL2CPU
{
    public static class ExceptionRegionExtensions
    {
        public static Type GetCatchType(this _ExceptionRegionInfo aThis)
        {
            return DebugSymbolReader.GetCatchType(aThis.Module, aThis.ExceptionRegion);
        }
    }
}
