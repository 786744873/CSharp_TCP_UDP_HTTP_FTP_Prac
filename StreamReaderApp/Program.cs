using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamReaderApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string s = "";
            using (StreamReader sr=new StreamReader("c:\\file1.txt"))
            {
                s = sr.ReadToEnd();
            }
            Console.WriteLine(s);
            Console.ReadLine();
        }
    }
}
