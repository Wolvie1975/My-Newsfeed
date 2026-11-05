#!/usr/bin/env bash
# Deploys the current commit with Docker Compose: the only deploy path for web and admin (DEPLOY.md).
#
#   deployment/deploy.sh                 both services
#   deployment/deploy.sh web             or admin
#   deployment/deploy.sh all --dry-run   print what would happen, change nothing
#   options: --skip-verify (tools/verify.sh --quick already ran on this commit), --allow-dirty (deploy uncommitted
#            changes: the images are then labelled <sha>-dirty and /version can't prove what's running),
#            --env-change (.env was changed on purpose, e.g. a rotated password: accept a different environment
#            and list the changed variable names, never values)
#
# Steps: refuse a dirty tree → tools/verify.sh --quick → build with GIT_COMMIT/BUILD_TIME (image label and /version)
# → tag the new images <sha> and the running ones before-<sha> → recreate with Compose → wait for healthy, check the
# environment is unchanged and /version reports this commit → on any failure, roll back automatically and exit 1.
# Keeps the newest two before-<sha> rollback images per service (older hand-named tags are never touched); prints a
# block for release history.
#
# Roll back later with deployment/rollback.sh <sha>.

set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

declare -A container=([web]=mynewsfeed-web [admin]=mynewspaper-admin)
declare -A repo=([web]=mynewspaper-web [admin]=mynewspaper-admin)
keep=2

services=()
dry=0
verify=1
allow_dirty=0
env_change=0
for arg in "$@"; do
    case "$arg" in
        web | admin) services+=("$arg") ;;
        all) services+=(web admin) ;;
        --dry-run) dry=1 ;;
        --skip-verify) verify=0 ;;
        --allow-dirty) allow_dirty=1 ;;
        --env-change) env_change=1 ;;
        -h | --help) sed -n '2,18p' "$0"; exit 0 ;;
        *) echo "unknown argument '$arg' (see --help)" >&2; exit 2 ;;
    esac
done
[[ ${#services[@]} -gt 0 ]] || services=(web admin)
mapfile -t services < <(printf '%s\n' "${services[@]}" | awk '!seen[$0]++')

say() { printf '\n== %s\n' "$*"; }
# run <command…>: executes it, or prints it on a dry run.
run() {
    if [[ $dry -eq 1 ]]; then
        printf '   would run: %s\n' "$*"
    else
        "$@"
    fi
}

# GET <path> from inside the container (port 8080), the same way its health check does; prints the body.
container_get() {
    docker exec "$1" bash -c "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET $2 HTTP/1.0\r\n\r\n' >&3 && cat <&3" 2>/dev/null |
        awk 'body { print } /^\r?$/ { body = 1 }'
}

# The environment Compose gives the container (minus what the image itself sets), one "NAME <value hash>" per line.
env_lines() {
    local c="$1" image
    image="$(docker inspect -f '{{.Image}}' "$c")"
    comm -23 <(docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$c" | grep -v '^$' | sort) \
        <(docker image inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$image" | grep -v '^$' | sort) |
        while IFS= read -r line; do printf '%s %s\n' "${line%%=*}" "$(printf %s "${line#*=}" | sha256sum | cut -c1-16)"; done
}
# All of it hashed; values are never printed.
env_hash() { env_lines "$1" | sha256sum | cut -c1-16; }

wait_healthy() {
    local c="$1" want_image="$2" status
    for _ in $(seq 1 60); do
        status="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$c" 2>/dev/null || echo missing)"
        if [[ "$status" == healthy && "$(docker inspect -f '{{.Image}}' "$c")" == "$want_image" ]]; then return 0; fi
        sleep 2
    done
    echo "   $c: not healthy on the expected image (status: $status)" >&2
    return 1
}

# ---- 1. preflight ----
sha_full="$(git rev-parse HEAD)"
sha="$(git rev-parse --short=12 HEAD)"
if [[ -n "$(git status --porcelain)" ]]; then
    if [[ $allow_dirty -eq 0 ]]; then
        echo "The working tree has uncommitted changes. Commit first (AGENTS.md), or pass --allow-dirty." >&2
        git status --short >&2
        exit 1
    fi
    sha="$sha-dirty"
    sha_full="$sha_full-dirty"
fi
say "Deploying $sha ($(git log -1 --format=%s)) to: ${services[*]}$([[ $dry -eq 1 ]] && echo '  [dry run]')"

declare -A old_image old_env old_env_lines
for s in "${services[@]}"; do
    old_image[$s]="$(docker inspect -f '{{.Image}}' "${container[$s]}")"
    old_env[$s]="$(env_hash "${container[$s]}")"
    old_env_lines[$s]="$(env_lines "${container[$s]}")"
    echo "   ${container[$s]}: running ${old_image[$s]:7:12}, env ${old_env[$s]}"
done

# ---- 2. verify ----
if [[ $verify -eq 1 ]]; then
    say "tools/verify.sh --quick"
    run tools/verify.sh --quick
fi

# ---- 3. build ----
say "Build"
export GIT_COMMIT="$sha_full" BUILD_TIME
BUILD_TIME="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
run docker compose build "${services[@]}"

declare -A new_image
for s in "${services[@]}"; do
    run docker image tag "${repo[$s]}:latest" "${repo[$s]}:$sha"
    run docker image tag "${old_image[$s]}" "${repo[$s]}:before-$sha"
    new_image[$s]="$(docker image inspect -f '{{.Id}}' "${repo[$s]}:latest")"
done

if [[ $dry -eq 1 ]]; then
    say "Dry run: would recreate ${services[*]}, wait for healthy, compare env hashes and /version, prune old before-* tags."
    exit 0
fi

# ---- 4. recreate and check; roll back on failure ----
rollback() {
    say "FAILED: rolling back ${services[*]} to the previous images"
    for s in "${services[@]}"; do docker image tag "${repo[$s]}:before-$sha" "${repo[$s]}:latest"; done
    docker compose up -d --no-build --pull never --force-recreate "${services[@]}"
    for s in "${services[@]}"; do wait_healthy "${container[$s]}" "${old_image[$s]}" && echo "   ${container[$s]}: back on ${old_image[$s]:7:12}, healthy"; done
    exit 1
}

say "Recreate"
docker compose up -d --no-build --pull never "${services[@]}" || rollback

declare -A version env_notes
for s in "${services[@]}"; do
    wait_healthy "${container[$s]}" "${new_image[$s]}" || rollback
    new_env="$(env_hash "${container[$s]}")"
    env_note="env unchanged"
    if [[ "$new_env" != "${old_env[$s]}" ]]; then
        changed="$(diff <(echo "${old_env_lines[$s]}") <(env_lines "${container[$s]}") | awk '/^[<>]/ { print $2 }' | sort -u | paste -sd, -)"
        if [[ $env_change -eq 0 ]]; then
            echo "   ${container[$s]}: environment changed ($changed); pass --env-change if .env was changed on purpose" >&2
            rollback
        fi
        env_note="env changed as intended: $changed"
    fi
    version[$s]="$(container_get "${container[$s]}" /version)"
    if [[ "${version[$s]}" != *"\"commit\":\"$sha_full\""* ]]; then
        echo "   ${container[$s]}: /version doesn't report $sha_full: ${version[$s]}" >&2
        rollback
    fi
    echo "   ${container[$s]}: healthy on ${new_image[$s]:7:12}, $env_note, /version $sha"
    env_notes[$s]="$env_note"
done

# ---- 5. retention: the newest $keep before-<sha> tags per service; release tags only for images still kept ----
# Only tags this script creates (before-<12 hex>, <12 hex>). Older hand-made tags (before-data-access-20261104 …) are
# left alone: removing rollback images is the user's call (AGENTS.md).
say "Retention (keep $keep rollback images per service)"
for s in "${services[@]}"; do
    mapfile -t befores < <(docker images "${repo[$s]}" --format '{{.CreatedAt}}|{{.Tag}}' | grep -E '\|before-[0-9a-f]{12}(-dirty)?$' | sort -r | cut -d'|' -f2)
    for tag in "${befores[@]:$keep}"; do
        docker image rm "${repo[$s]}:$tag" >/dev/null && echo "   removed ${repo[$s]}:$tag"
    done
    kept_ids="$( { docker image inspect -f '{{.Id}}' "${repo[$s]}:latest"
        for tag in "${befores[@]:0:$keep}"; do docker image inspect -f '{{.Id}}' "${repo[$s]}:$tag"; done; } | sort -u)"
    while read -r tag; do
        id="$(docker image inspect -f '{{.Id}}' "${repo[$s]}:$tag")"
        grep -q "$id" <<<"$kept_ids" || { docker image rm "${repo[$s]}:$tag" >/dev/null && echo "   removed ${repo[$s]}:$tag"; }
    done < <(docker images "${repo[$s]}" --format '{{.Tag}}' | grep -E '^[0-9a-f]{12}(-dirty)?$')
done

# ---- 6. release-history block ----
say "For release history"
echo "Deployment: commit \`$sha\` via \`deployment/deploy.sh ${services[*]}\` at $BUILD_TIME."
for s in "${services[@]}"; do
    echo "- ${repo[$s]}:$sha (\`${new_image[$s]:7:12}\`): healthy, ${env_notes[$s]}, /version ${version[$s]}"
    echo "  Rollback: \`deployment/rollback.sh $sha $s\` → ${repo[$s]}:before-$sha (\`${old_image[$s]:7:12}\`)"
done
echo "Next: node tools/live-check.cjs"
