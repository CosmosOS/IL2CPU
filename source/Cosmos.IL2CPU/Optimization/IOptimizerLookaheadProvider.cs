using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.IL2CPU.Optimization
{
    /// <summary>
    /// Represents an object that can provide methods that can be used in
    /// look-ahead operations by an <see cref="Optimizer"/>.
    /// </summary>
    /// <remarks>
    /// A look-ahead is used when an <see cref="Optimizer"/> requires to
    /// compute the method body of a method it hasn't been provided with before.
    /// </remarks>
    public interface IOptimizerLookaheadProvider
    {
        /// <summary>
        /// Provides the method body for the given method, using a look-ahead lookup.
        /// When using a optimization look-ahead, no optimizations are applied, and
        /// a different cache is used.
        /// </summary>
        /// <param name="aMethod">The method to compute the body of.</param>
        public List<ILOpCode> ProcessMethodAhead(MethodBase aMethod);
    }
}
