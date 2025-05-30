FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebUI/WebUI.csproj", "WebUI/"]
COPY ["DiscordBotCore.Configuration/DiscordBotCore.Configuration.csproj", "DiscordBotCore.Configuration/"]
COPY ["DiscordBotCore.Logging/DiscordBotCore.Logging.csproj", "DiscordBotCore.Logging/"]
COPY ["DiscordBotCore.Database.Sqlite/DiscordBotCore.Database.Sqlite.csproj", "DiscordBotCore.Database.Sqlite/"]
COPY ["DiscordBotCore.PluginManagement/DiscordBotCore.PluginManagement.csproj", "DiscordBotCore.PluginManagement/"]
COPY ["DiscordBotCore.Networking/DiscordBotCore.Networking.csproj", "DiscordBotCore.Networking/"]
COPY ["DiscordBotCore.Utilities/DiscordBotCore.Utilities.csproj", "DiscordBotCore.Utilities/"]
COPY ["DiscordBotCore/DiscordBotCore.csproj", "DiscordBotCore/"]
COPY ["DiscordBotCore.PluginCore/DiscordBotCore.PluginCore.csproj", "DiscordBotCore.PluginCore/"]
COPY ["DiscordBotCore.PluginManagement.Loading/DiscordBotCore.PluginManagement.Loading.csproj", "DiscordBotCore.PluginManagement.Loading/"]
RUN dotnet restore "WebUI/WebUI.csproj"
COPY . .
WORKDIR "/src/WebUI"
RUN dotnet build "./WebUI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebUI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebUI.dll"]
