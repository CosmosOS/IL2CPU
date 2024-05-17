using System;
using System.Linq;
using System.Collections.Generic;

namespace Cosmos.IL2CPU.Optimization
{
    /// <summary>
    /// Optimizes IL methods.
    /// </summary>
    public class Optimizer
    {
        /// <summary>
        /// The method look-ahead provider that can be used to compute the method body of a method it hasn't been provided with before.
        /// </summary>
        public IOptimizerLookaheadProvider LookaheadProvider { get; set; }

        /// <summary>
        /// The passes that the optimizer will use.
        /// </summary>
        public List<OptimizerPass> Passes { get; init; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Optimizer"/> class.
        /// </summary>
        /// <param name="aLookaheadProvider">The look-ahead provider to use.</param>
        public Optimizer(IOptimizerLookaheadProvider aLookaheadProvider)
        {
            LookaheadProvider = aLookaheadProvider ?? throw new ArgumentNullException(nameof(aLookaheadProvider));
        }

        /// <summary>
        /// Exclude the given type of optimizer passes in this optimizer.
        /// </summary>
        /// <param name="passType">The type to exclude.</param>
        /// <exception cref="InvalidOperationException">Thrown when the given type is not a subclass of <see cref="OptimizerPass"/>.</exception>
        public void ExcludePassType(Type passType)
        {
            if(!passType.IsSubclassOf(typeof(OptimizerPass))) {
                throw new InvalidOperationException("The given type is not a subclass of OptimizerPass.");
            }

            for (int i = Passes.Count - 1; i >= 0; i--) {
                if (Passes[i].GetType() == passType) {
                    Passes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Optimizes the given method.
        /// </summary>
        /// <param name="il">The body of the method, in IL instructions.</param>
        /// <returns>The optimized method body.</returns>
        public List<ILOpCode> Optimize(List<ILOpCode> instructions)
        {
            foreach (var pass in Passes) {
                instructions = pass.Process(instructions);
            }

            return instructions;
        }

        /// <summary>
        /// Adds a pass to this <see cref="Optimizer"/> and returns the object.
        /// </summary>
        /// <param name="aPass">The pass to add to the optimizer.</param>
        public Optimizer WithPass(OptimizerPass aPass)
        {
            aPass.Owner = this;
            Passes.Add(aPass);
            return this;
        }
    }
}
