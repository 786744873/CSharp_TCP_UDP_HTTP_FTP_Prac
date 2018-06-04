using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadPoolExample
{
    class Program
    {
        static void Main(string[] args)
        {
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            Console.WriteLine($"workerThreads线程数:{workerThreads}");
            Console.WriteLine($"completionPortThreads线程数:{completionPortThreads}");
            //将任务添加到队列中
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc));
            Console.WriteLine("主线程执行一些操作，然后暂停等待异步操作完成");
            //如果不用sleep语句，当主线程在线程池中的任务完成前推出，会导致后台执行的线程也直接退出，从而无法完成后台任务
            Thread.Sleep(1000);
            Console.WriteLine("主线程已退出");
            Console.ReadKey();
        }

        //执行任务的线程
        static void ThreadProc(Object stateInfo)
        {
            //由于没有为QueueUserWorkItem传递object参数，所以stateInfo为null
            Console.WriteLine("这是线程池中的线程输出的内容");
        }
    }
}
