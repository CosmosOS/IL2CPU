using System;

namespace IL2CPU
{
    public class Program
    {
        public static int Main(string[] args) =>
            Cosmos.IL2CPU.Program.Run(args,
            w =>
            {
                Console.Write("Warning: ");
                Console.WriteLine(w);
            },
            e =>
            {
                Console.Write("Error: ");
                Console.WriteLine(e);
            });
    }
}
