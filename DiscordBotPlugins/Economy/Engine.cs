using System;
using System.Collections.Generic;

using PluginManager.Database;

namespace Economy;
public static class Engine
{
    public static SqlDatabase Database { get; set; }

    public static async void AddUser(ulong userID)
    {
        await Database.InsertAsync("UserBank", userID.ToString(), "0");
    }

    public static async void RemoveUser(ulong userID)
    {
        await Database.RemoveKeyAsync("UserBank", "UserID", userID.ToString());
    }

    public static async Task AddMoney(ulong userID, float amount)
    {
        var balance = await Database.GetValueAsync("UserBank", "UserID", userID.ToString(), "Balance");
        if (balance == null)
        {
            AddUser(userID);
            balance = "0";
        }

        var float_balance = float.Parse(balance);
        float_balance += amount;
        await Database.SetValueAsync("UserBank", "UserID", userID.ToString(), "Balance", float_balance.ToString());
    }

    public static async Task RemoveMoney(ulong userID, float amount)
    {
        var balance = await Database.GetValueAsync("UserBank", "UserID", userID.ToString(), "Balance");
        if (balance == null)
        {
            AddUser(userID);
            balance = "0";
        }

        var float_balance = float.Parse(balance);
        float_balance -= amount;
        await Database.SetValueAsync("UserBank", "UserID", userID.ToString(), "Balance", float_balance.ToString());
    }

    public static async Task<float> GetBalance(ulong userID)
    {
        var balance = await Database.GetValueAsync("UserBank", "UserID", userID.ToString(), "Balance");
        if (balance == null)
        {
            AddUser(userID);
            balance = "0";
        }

        return float.Parse(balance);
    }


}
