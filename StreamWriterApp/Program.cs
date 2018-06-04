using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamWriterApp
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamWriter sw = null;
            string strPath = "c:\\file1.txt";
            try
            {
                sw = new StreamWriter(strPath);
                sw.WriteLine("当前时间为：{0}", DateTime.Now);
                Console.WriteLine("写文件成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("写文件失败:{0}", ex.ToString());
            }
            finally
            {
                if (sw!=null)
                {
                    sw.Close();
                }
            }

            Console.ReadKey();
        }
    }
}
