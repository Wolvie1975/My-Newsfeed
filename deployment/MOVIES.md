# Deploy movie releases

Target: `jcarpio@mynewsfeed-prod`, project `/home/jcarpio/Projects/My-Newsfeed`, public port `8085`.

1. In that project folder, inspect `git status --short` and preserve any local changes. Fetch and fast-forward the `develop` branch to the release commit. Do not reset or overwrite server changes.
2. Use production's `.env` `SQL_CONNECTION` to select its database. Run `deployment/check-movie-schema.sql` with your existing SQL tooling, using error-on-failure mode (`sqlcmd -b`). Pass credentials privately; do not print passwords. This check is read-only. Stop if it fails. The movie schema is maintained by the separate scraper project; this application does not create those tables. For a missing Results conference schema, apply `db/013` through the normal backed-up database migration process before proceeding.
3. Record the current image for rollback and rebuild:

```bash
cd /home/jcarpio/Projects/My-Newsfeed
docker image tag "$(docker inspect mynewsfeed-web --format '{{.Image}}')" mynewsfeed-web:before-movies
docker compose up -d --build web
docker compose ps
```

4. Confirm that the container is healthy. Check `/healthz` and `/movies` return 200 at `http://localhost:8085`. Sign in and verify `/admin` displays Movie release sources; ensure the existing status tables still load. Empty movie data is valid.
5. If validation fails, preserve logs and roll back the image without touching the database:

```bash
docker image tag mynewsfeed-web:before-movies mynewsfeed-web:latest
docker compose up -d --no-build --pull never --force-recreate web
```

The release adds no database writes or new required environment variables. `Scraper:StaleAfterHours` controls the existing freshness threshold, also used for movie sources.
