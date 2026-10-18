# Run with Docker

The repository has a `WebUI/Dockerfile` and a `docker-compose.yml` that build the WebUI into an image and keep the bot's data outside the container.

## Start with Docker Compose

From the repository root, choose a dashboard password and put it in a `.env` file next to `docker-compose.yml` (`.env` is ignored by git):

```bash
echo "DASHBOARD_PASSWORD=$(openssl rand -base64 24)" > .env
```

Then build and start:

```bash
docker compose up --build
```

Open <http://localhost:8080> and sign in with that password (`cat .env` shows it). Compose refuses to start without `DASHBOARD_PASSWORD`.

What `docker-compose.yml` does:

```yaml
services:
  discord-bot:
    build:
      context: .
      dockerfile: ./WebUI/Dockerfile
    ports:
      - "8080:8080"                       # dashboard
    environment:
      Dashboard__Password: ${DASHBOARD_PASSWORD:?...}
    volumes:
      - seth-data:/app/Data               # configuration, plugins, plugin database, logs, sign-in keys
      - seth-libraries:/app/Libraries     # plugin dependencies from the marketplace
    restart: unless-stopped

volumes:
  seth-data:
  seth-libraries:
```

- The app runs in `/app` as a non-root user. Its `Data` and `Libraries` folders are Docker volumes, so your token, installed plugins and logs survive rebuilding the container.
- The image listens on port 8080 (the ASP.NET Core default inside containers).
- `restart: unless-stopped` brings the container back after a crash or a reboot. Turn on **Start automatically** in **Settings** so the bot also starts and loads its plugins by itself.

Run it in the background with `docker compose up -d --build`, and follow the logs with:

```bash
docker compose logs -f discord-bot
```

### Using a host folder instead of a volume

To see the files directly on the host, replace `seth-data:/app/Data` with a bind mount such as `./Data:/app/Data`. The container user (UID 1654) must be able to write there, so create the folder first and give it to that user:

```bash
mkdir -p Data && sudo chown 1654 Data
```

On Linux, a folder that Docker creates for a bind mount belongs to root, and the bot can't write to it.

## Adding plugins to a container

Use the dashboard's **Add local** button ([Managing plugins](../using-plugins/managing-plugins.md#add-a-plugin-you-built)). It uploads the DLL into `/app/Data/Plugins`, which is on the data volume.

If a plugin needs extra assemblies, they go in `/app/Libraries/<plugin assembly name>/` (see [Dependencies](../creating-plugins/dependencies.md)). Copy them into the running container with:

```bash
docker compose cp ./MyDependency.dll discord-bot:/app/Libraries/MyPlugin/
```

## Security

The dashboard is password-protected (see [Dashboard password](../configuration/host-settings.md#dashboard-password)), but `ports: "8080:8080"` serves it over plain HTTP on every network interface of the host. On a machine reachable from the internet, put it behind a reverse proxy that adds HTTPS, or bind it to localhost (`"127.0.0.1:8080:8080"`) and reach it through an SSH tunnel.

---

Next: [First start](first-start.md)
