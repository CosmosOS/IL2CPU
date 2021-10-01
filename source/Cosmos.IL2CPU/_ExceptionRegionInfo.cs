using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Cosmos.IL2CPU
{
    public class _ExceptionRegionInfo
    {
        public ExceptionHandlingClause ExceptionClause { get; }
        public int HandlerOffset { get; }
        public int HandlerLength { get; }
        public int TryOffset { get; }
        public int TryLength { get; }
        public int FilterOffset { get; }
        public ExceptionRegionKind Kind { get; }
        public Type CatchType { get; }

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
                    CatchType = BaseTypes.Exception; //TODO: Confirm that this is correct.
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Finally))
                {
                    Kind = ExceptionRegionKind.Finally;
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
