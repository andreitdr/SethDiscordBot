FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5055

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiscordBot/DiscordBot.csproj", "DiscordBot/"]
COPY ["DiscordBotCore/DiscordBotCore.csproj", "DiscordBotCore/"]
RUN dotnet restore "DiscordBot/DiscordBot.csproj"
COPY . .
WORKDIR "/src/DiscordBot"
RUN dotnet build "DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiscordBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5055
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
