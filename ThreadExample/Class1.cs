﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadExample
{
    class Class1
    {
        public volatile bool shouldStop;
        private Form1 form1;

        public Class1(Form1 form1)
        {
            this.form1 = form1;
        }

        public void Method1(object obj)
        {
            string s = obj as String;
            form1.AddMessage(s);
            while (shouldStop == false)
            {
                Thread.Sleep(100);//线程1休眠100ms
                form1.AddMessage("a");
            }
            form1.AddMessage("\n线程Method1已终止");
        }

        public void Method2()
        {
            while (shouldStop==false)
            {
                Thread.Sleep(100);//线程2休眠100ms
                form1.AddMessage("b");
            }
            form1.AddMessage("\n线程Method2已终止");
        }


    }
}
