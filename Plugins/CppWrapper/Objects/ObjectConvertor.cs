using DiscordBotCore;

namespace CppWrapper.Objects
{
    public static class ObjectConvertor
    {
        public static ApplicationStruct ToApplicationStruct(this Application application)
        {
            return new ApplicationStruct
            {
                Token = application.ApplicationEnvironmentVariables["token"],
                Prefix = application.ApplicationEnvironmentVariables["prefix"],
                ServerId = application.ServerID
            };
        }
    }
}
