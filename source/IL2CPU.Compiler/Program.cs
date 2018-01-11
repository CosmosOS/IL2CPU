using System;

namespace IL2CPU.Compiler
{
    public class Program
    {
        public static int Main(string[] args) =>
            Cosmos.IL2CPU.Program.Run(args,
                m => Console.WriteLine($"Message: {m}"),
                e => Console.Error.WriteLine($"Error: {e}"));
    }
}
