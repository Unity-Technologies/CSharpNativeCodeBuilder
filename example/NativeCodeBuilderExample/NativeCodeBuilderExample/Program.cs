using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeCodeBuilderExample
{
    class Program
    {
        static void Main(string[] args)
        {
            int a = 1;
            int b = 2;
            int c = NativeFuncs.inst.add_two_nums(a, b);

            Console.WriteLine(string.Format("{0} + {1} = {3}", a, b, c));
        }
    }
}
