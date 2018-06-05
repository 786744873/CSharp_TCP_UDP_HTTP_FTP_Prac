using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// 用于保存已经坐在游戏桌座位上玩家的情况
    /// </summary>
    struct Player
    {
        public User user;           //User类的实例
        public bool started;        //是否已经开始
        public int grade;           //成绩
        public bool someone;        //是否有人坐下
    }
}
