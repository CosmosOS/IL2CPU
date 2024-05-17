using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Cosmos.IL2CPU
{
    /// <summary>
    /// Represents the information about a clause in a structured exception-handling block.
    /// </summary>
    public class _ExceptionRegionInfo
    {
        /// <summary>
        /// The exception handling clause that this object describes.
        /// </summary>
        public ExceptionHandlingClause ExceptionClause { get; }

        /// <summary>
        /// Gets the offset within the method body, in bytes, of this exception-handling clause.
        /// </summary>
        public int HandlerOffset { get; }

        /// <summary>
        /// Gets the length, in bytes, of the body of this exception-handling clause.
        /// </summary>
        public int HandlerLength { get; }

        /// <summary>
        /// The offset within the method, in bytes, of the try block that includes this exception-handling clause.
        /// </summary>
        public int TryOffset { get; }

        /// <summary>
        /// The total length, in bytes, of the try block that includes this exception-handling clause.
        /// </summary>
        public int TryLength { get; }

        /// <summary>
        /// Gets the offset within the method body, in bytes, of the user-supplied filter code.
        /// </summary>
        public int FilterOffset { get; }

        /// <summary>
        /// Gets the type of this exception region.
        /// </summary>
        public ExceptionRegionKind Kind { get; }

        /// <summary>
        /// Gets the type of exception handled by this clause.
        /// </summary>
        public Type CatchType { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="_ExceptionRegionInfo"/> class, describing
        /// the given exception handling clause.
        /// </summary>
        /// <param name="aExceptionClause">The exception handling clause that the newly created object should describe.</param>
        public _ExceptionRegionInfo(ExceptionHandlingClause aExceptionClause)
            : this(
                  aExceptionClause,
                  aExceptionClause.HandlerOffset,
                  aExceptionClause.HandlerLength,
                  aExceptionClause.TryOffset,
                  aExceptionClause.TryLength,
                  aExceptionClause.Flags == ExceptionHandlingClauseOptions.Filter ? aExceptionClause.FilterOffset : 0
                )
        {

        }

        /// <summary>
        /// Creates a new instance of the <see cref="_ExceptionRegionInfo"/> class, describing
        /// the given exception handling clause with the given region offsets and lengths.
        /// </summary>
        /// <param name="aExceptionClause">The exception handling clause that the newly created object should describe.</param>
        internal _ExceptionRegionInfo(ExceptionHandlingClause aExceptionClause, int aHandlerOffset, int aHandlerLength, int aTryOffset, int aTryLength, int aFilterOffset)
        {
            try {
                ExceptionClause = aExceptionClause;
                HandlerOffset = aHandlerOffset;
                HandlerLength = aHandlerLength;
                TryOffset = aTryOffset;
                TryLength = aTryLength;

                if (aExceptionClause.Flags == ExceptionHandlingClauseOptions.Clause) {
                    Kind = ExceptionRegionKind.Catch;
                    CatchType = aExceptionClause.CatchType;
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Fault)) {
                    Kind = ExceptionRegionKind.Fault;
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Filter)) {
                    Kind = ExceptionRegionKind.Filter;
                    FilterOffset = aFilterOffset;
                    CatchType = typeof(System.Exception); //TODO: Confirm that this is correct.
                }
                else if (aExceptionClause.Flags.HasFlag(ExceptionHandlingClauseOptions.Finally)) {
                    Kind = ExceptionRegionKind.Finally;
                }
            }
            catch {
                // ignored
            }
        }
    }
}
