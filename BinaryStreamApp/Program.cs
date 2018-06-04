using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryStreamApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //为文件打开一个二进制写入器
            FileStream fs;
            fs = new FileStream("c:\\BinFile.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryWriter bw = new BinaryWriter(fs);
            //准备不同类型的数据
            double aDouble = 1234.67;
            int aInt = 34567;
            char[] aCharArray = { 'A', 'B', 'C' };
            //利用Write方法的多种重载方式写入数据
            bw.Write(aDouble);
            bw.Write(aInt);
            bw.Write(aCharArray);
            int length = Convert.ToInt32(bw.BaseStream.Length);
            fs.Close();
            bw.Close();
            //读取并输出数据
            fs = new FileStream("c:\\BinFile.dat", FileMode.OpenOrCreate, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            Console.WriteLine(br.ReadDouble().ToString());
            Console.WriteLine(br.ReadInt32().ToString());
            char[] data = br.ReadChars(length);
            for (int i = 0; i < data.Length; i++)
            {
                Console.WriteLine("{0,7:x}",data[i]);
            }
            fs.Close();
            br.Close();
            Console.ReadKey();
        }
    }
}
