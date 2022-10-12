using System;
using PluginManager.Online.Helpers;

namespace PluginManager.Online.Updates;

public class Update
{
    public static Update Empty = new(null, null, null);

    private readonly bool isEmpty;

    public VersionString newVersion;
    public string        pakName;
    public string        UpdateMessage;

    public Update(string pakName, string updateMessage, VersionString newVersion)
    {
        this.pakName    = pakName;
        UpdateMessage   = updateMessage;
        this.newVersion = newVersion;

        if (pakName is null && updateMessage is null && newVersion is null)
            isEmpty = true;
    }

    public override string ToString()
    {
        if (isEmpty)
            throw new Exception("The update is EMPTY. Can not print information about an empty update !");
        return $"Package Name: {pakName}\n"         +
               $"Update Message: {UpdateMessage}\n" +
               $"Version: {newVersion}";
    }
}