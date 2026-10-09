# Android installation with private Tailscale HTTPS

Tailscale Serve proxies the site's HTTP port 8085 through the server's HTTPS `*.ts.net` name. It is tailnet-only; Funnel is not used. Serve configuration lives in the Tailscale container's persistent state volume.

On Android, connect Tailscale, open the HTTPS address in Chrome, and choose Install app / Add to Home screen from Chrome's menu. The manifest launches Home in a standalone window. The PWA still needs the server for current news and schedules; its service worker caches only identity assets and a reconnect screen. It does not cache articles, schedules, login responses, or admin pages.

Manage Serve on this host:

```bash
docker exec tailscale tailscale serve status
docker exec tailscale tailscale serve --bg http://127.0.0.1:8085
```

Enable Serve through the account link reported by Tailscale if requested. Do not replace an existing Serve configuration without reviewing it.

`REVERSE_PROXY_IP` in production's `.env` is the web container network gateway used by the trusted host proxy. Compose passes this to `ReverseProxy:KnownProxies`; the app accepts forwarded scheme/client headers only from configured proxies and the framework's default loopback proxies. This lets HTTPS requests receive secure authentication cookies while LAN HTTP remains available. Check the gateway if the Docker network is recreated:

```bash
docker inspect mynewsfeed-web --format '{{range .NetworkSettings.Networks}}{{.Gateway}}{{end}}'
```

After deployment, verify the HTTPS certificate, manifest, icon dimensions, active service worker, offline screen, and admin login through HTTPS. Service-worker updates remove only prior `mynewsfeed-pwa-*` caches. Change the cache version if bundled offline assets change.
