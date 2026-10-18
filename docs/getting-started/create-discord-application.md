# Create the Discord application

The bot logs in to Discord with a **bot token**. You get one by creating an application in the Discord Developer Portal.

## 1. Create the application

1. Open the [Discord Developer Portal](https://discord.com/developers/applications) and sign in.
2. Click **New Application**, give it a name and accept the terms.

## 2. Get the bot token

1. Open the **Bot** page of the application.
2. Click **Reset Token**, confirm, and copy the token.

Keep the token secret. Anyone who has it can control your bot. If it leaks, reset it again; the old one stops working at once.

## 3. Enable the privileged gateway intents

The bot connects with **all** gateway intents (`GatewayIntents.All`), which includes three privileged ones. On the same **Bot** page, under *Privileged Gateway Intents*, turn on:

- **Presence Intent**
- **Server Members Intent**
- **Message Content Intent**

Then save. Without them Discord refuses the connection and the dashboard stays on *Connecting…* (see [Troubleshooting](../troubleshooting.md#the-bot-stays-on-connecting)). The Message Content intent is also what lets prefix commands such as `;help` read the message text.

> Bots in 100 or more servers need Discord's approval for privileged intents. For a bot in a few servers, the toggles are enough.

## 4. Invite the bot to your server

1. Open **OAuth2 → URL Generator**.
2. Under *Scopes*, select **bot** and **applications.commands** (the second one is needed for slash commands).
3. Under *Bot Permissions*, select what your plugins need. A practical start is *Send Messages*, *Embed Links*, *Read Message History*, *View Channels* and *Use Slash Commands*. *Administrator* also works for a private test server.
4. Open the generated URL, pick the server and authorize.

## 5. Copy your server ID (optional, recommended)

Slash commands can be registered either **globally** (every server, but Discord may take up to an hour to show them) or **per server** (instant). To register them per server you need the server's ID:

1. In Discord, open **User Settings → Advanced** and turn on **Developer Mode**.
2. Right-click the server icon and choose **Copy Server ID**.

You will paste it into the dashboard's Settings panel on [first start](first-start.md).

---

Next: [Run from source](run-from-source.md) or [Run with Docker](run-with-docker.md)
