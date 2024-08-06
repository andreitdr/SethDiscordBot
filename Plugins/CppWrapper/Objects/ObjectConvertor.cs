using DiscordBotCore;

namespace CppWrapper.Objects
{
    public static class ObjectConvertor
    {
        public static ApplicationStruct ToApplicationStruct(this Application application)
        {
            return new ApplicationStruct
            {
                Token = application.ApplicationEnvironmentVariables.Get<string>("token") ?? throw new Exception("Token not found"),
                Prefix = application.ApplicationEnvironmentVariables.Get<string>("prefix") ?? throw new Exception("Prefix not found"),
                ServerIds = application.ServerIDs.ToArray()
            };
        }
    }
}
