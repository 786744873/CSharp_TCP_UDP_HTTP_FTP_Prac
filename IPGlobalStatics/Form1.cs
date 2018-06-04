using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;

namespace IPGlobalStatics
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPGlobalStatistics ipstat= properties.GetIPv4GlobalStatistics();
            listBoxResult.Items.Add("本机所在域......" + properties.DomainName);
            listBoxResult.Items.Add("接收数据包....:" + ipstat.ReceivedPackets);
            listBoxResult.Items.Add("转发数据包....:" + ipstat.ReceivedPacketsForwarded);
            listBoxResult.Items.Add("传送数据包....:" + ipstat.ReceivedPacketsDelivered);
            listBoxResult.Items.Add("丢弃数据包....:" + ipstat.ReceivedPacketsDiscarded);

        }
    }
}
