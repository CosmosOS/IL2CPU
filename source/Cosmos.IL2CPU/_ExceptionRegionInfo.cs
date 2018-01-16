using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Cosmos.IL2CPU
{
    public class _ExceptionRegionInfo
    {
        public readonly ExceptionHandlingClause ExceptionClause;
        public readonly int HandlerOffset;
        public readonly int HandlerLength;
        public readonly int TryOffset;
        public readonly int TryLength;
        public readonly int FilterOffset;
        public readonly ExceptionRegionKind Kind;
        public readonly Type CatchType;

        public _ExceptionRegionInfo(ExceptionHandlingClause aExceptionClause)
        {
            try
            {
                ExceptionClause = aExceptionClause;
                HandlerOffset = aExceptionClause.HandlerOffset;
                HandlerLength = aExceptionClause.HandlerLength;
                TryOffset = aExceptionClause.TryOffset;
                TryLength = aExceptionClause.TryLength;

                if (aExceptionClause.Flags == ExceptionHandlingClauseOptions.Clause)
                {
                    Kind = ExceptionRegionKind.Catch;
                    CatchType = aExceptionClause.CatchType;
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Fault))
                {
                    Kind = ExceptionRegionKind.Fault;
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Filter))
                {
                    Kind = ExceptionRegionKind.Filter;
                    FilterOffset = aExceptionClause.FilterOffset;
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Finally))
                {
                    Kind = ExceptionRegionKind.Finally;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
