using System;

namespace Multiply
{
    class Program
    {
        static void Main(string[] args)
        {
            int.TryParse(args[0], out int value1);
            int.TryParse(args[1], out int value2);

            int result = value1 * value2;

            Console.WriteLine($"{value1}*{value2}={result}");
        }
    }
}