using Discord;
using Discord.WebSocket;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Interfaces;

namespace CppModuleDemo;

public class ModuleSlashCommand : IDbSlashCommand
{
    public string Name => "cpp-module-demo";
    public string Description => "A demo command to showcase the C++ module integration with Discord Bot Core.";
    public bool CanUseDm => false;
    public bool HasInteraction => false;

    public List<SlashCommandOptionBuilder> Options => new List<SlashCommandOptionBuilder>()
    {
        new SlashCommandOptionBuilder()
        {
            Name = "example-integer-value", Description = "An example integer value",
            Type = ApplicationCommandOptionType.Integer, IsRequired = true
        },
        new SlashCommandOptionBuilder()
        {
            Name = "example-number-value", Description = "An example number value",
            Type = ApplicationCommandOptionType.Number, IsRequired = true
        },
        new SlashCommandOptionBuilder()
        {
            Name = "example-string-value", Description = "An example boolean value",
            Type = ApplicationCommandOptionType.String, IsRequired = true
        }
    };

    public async void ExecuteServer(ILogger logger, SocketSlashCommand context)
    {
        long integerValue = (long)context.Data.Options.First(option => option.Name == "example-integer-value").Value;
        double numberValue = (double)context.Data.Options.First(option => option.Name == "example-number-value").Value;
        string stringValue = (string)context.Data.Options.First(option => option.Name == "example-string-value").Value;
        
        if(integerValue > int.MaxValue || integerValue < int.MinValue)
        {
            await context.Channel.SendMessageAsync("The provided integer value is out of range. Please provide a valid integer.");
            return;
        }
        
        await context.RespondAsync("Processing your request...", ephemeral: true);

        await context.Channel.SendMessageAsync("CppModuleDemo invoked with: \n" +
                                                       $"Integer Value: {integerValue}\n" +
                                                       $"Number Value: {numberValue}\n" +
                                                       $"String Value: {stringValue}");
        
        ExampleComplexObject complexObject = new ExampleComplexObject
        {
            IntegerValue = (int)integerValue,
            DoubleValue = numberValue,
            StringValue = stringValue
        };

        Delegates.ModifyComplexObject? modifyComplexObject =
            InternalSettings.ExternalApplicationHandler?.GetFunctionDelegate<Delegates.ModifyComplexObject>(
                InternalSettings.DemoModuleInternalId, "modifyComplexObject");

        if (modifyComplexObject is null)
        {
            await context.Channel.SendMessageAsync("Failed to retrieve the C++ function delegate. Please check the C++ module integration.");
            return;
        }
        
        modifyComplexObject(ref complexObject);

        await context.Channel.SendMessageAsync("CppModuleDemo command executed successfully! New values are:\n" +
                                               $"Integer Value: {((ExampleComplexObject)complexObject).IntegerValue}\n" +
                                               $"Number Value: {((ExampleComplexObject)complexObject).DoubleValue}\n" +
                                               $"String Value: {((ExampleComplexObject)complexObject).StringValue}");

    }
}