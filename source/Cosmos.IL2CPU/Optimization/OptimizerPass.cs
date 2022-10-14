using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.IL2CPU.Optimization
{
    /// <summary>
    /// Represents a single optimization pass.
    /// </summary>
    public abstract class OptimizerPass
    {
        /// <summary>
        /// The owner of this <see cref="OptimizerPass"/>.
        /// </summary>
        public Optimizer Owner { get; internal set; }

        /// <summary>
        /// Optimizes the given method.
        /// </summary>
        /// <param name="instructions">The body of the target method.</param>
        /// <returns>The optimized method body.</returns>
        public abstract List<ILOpCode> Process(List<ILOpCode> instructions);
    }
}
