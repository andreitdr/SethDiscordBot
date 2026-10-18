# Run with Docker

The repository has a `WebUI/Dockerfile` and a `docker-compose.yml` that build the WebUI into an image and keep the bot's data outside the container.

## Start with Docker Compose

From the repository root:

```bash
docker compose up --build
```

Then open <http://localhost:8080>.

What `docker-compose.yml` does:

```yaml
services:
  discord-bot:
    build:
      context: .
      dockerfile: ./WebUI/Dockerfile
    ports:
      - "8080:8080"        # dashboard
    volumes:
      - ./Data:/app/Data   # configuration, plugins, plugin database, logs
```

- The app runs in `/app`, so its `Data` folder is `/app/Data`, which is mounted from `./Data` on the host. Your token, installed plugins and logs survive rebuilding the container.
- The image listens on port 8080 (the ASP.NET Core default inside containers).

Run it in the background with `docker compose up -d --build`, and follow the logs with:

```bash
docker compose logs -f discord-bot
```

## Adding plugins to a container

Use the dashboard's **Add local** button ([Managing plugins](../using-plugins/managing-plugins.md#add-a-plugin-you-built)). It uploads the DLL into `/app/Data/Plugins`, which is on the mounted volume.

If a plugin needs extra assemblies, they go in `/app/Libraries/<plugin assembly name>/` (see [Dependencies](../creating-plugins/dependencies.md)). That folder is not mounted by default; add a second volume if you need it:

```yaml
    volumes:
      - ./Data:/app/Data
      - ./Libraries:/app/Libraries
```

## Security

The dashboard has no authentication. `ports: "8080:8080"` publishes it on every network interface of the host. On a machine reachable from the internet, bind it to localhost (`"127.0.0.1:8080:8080"`) and reach it through an SSH tunnel or an authenticating reverse proxy. See [Host settings](../configuration/host-settings.md#exposing-the-dashboard).

---

Next: [First start](first-start.md)
