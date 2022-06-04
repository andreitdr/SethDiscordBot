using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginManager;

namespace EVE_LevelingSystem.LevelingSystemCore
{
    internal static class LevelCalculator
    {
        internal static List<string> OnWaitingList = new();

        internal static Int64 GetNextLevelRequiredEXP(int currentLevel)
        {
            return currentLevel * 8 + 24;
        }

        internal static void LevelUp(this User user)
        {
            user.CurrentEXP           = 0;
            user.RequiredEXPToLevelUp = GetNextLevelRequiredEXP(user.CurrentLevel);
            user.CurrentLevel++;
        }

        internal static bool AddEXP(this User user)
        {
            if (OnWaitingList.Contains(user.userID)) return false;
            Random r      = new Random();
            int    exp    = r.Next(2, 12);
            Int64  userXP = user.CurrentEXP;
            Int64  reqEXP = user.RequiredEXPToLevelUp;
            if (userXP + exp >= reqEXP)
            {
                user.LevelUp();
                user.CurrentEXP = exp - (reqEXP - userXP);
                Console.WriteLine("Level up");
                return true;
            }

            user.CurrentEXP += exp;

            OnWaitingList.Add(user.userID);


            new Thread(() =>
                {
                    int minutesToWait = Level.globalSettings.TimeToWaitBetweenMessages;
                    Thread.Sleep(60000 * minutesToWait);
                    OnWaitingList.Remove(user.userID);
                }
            );

            return false;
        }
    }
}
