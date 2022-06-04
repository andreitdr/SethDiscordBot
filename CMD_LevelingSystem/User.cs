using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD_LevelingSystem
{
    public class User
    {
        public string userID               { get; set; }
        public int    CurrentLevel         { get; set; }
        public Int64  CurrentEXP           { get; set; }
        public Int64  RequiredEXPToLevelUp { get; set; }
    }
}
