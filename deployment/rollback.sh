#!/usr/bin/env bash
# Undoes a deploy: puts back the images that ran before deployment/deploy.sh deployed <sha>, without building.
#
#   deployment/rollback.sh <sha>            both services that have a before-<sha> image
#   deployment/rollback.sh <sha> web        or admin
#   deployment/rollback.sh --list           the rollback images available
#
# <sha> is the 12-character commit deploy.sh printed (`git rev-parse --short=12 <commit>`). Leaves the database
# alone: rolling back an image never drops data or schema (DEPLOY.md, Rollback).

set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

declare -A container=([web]=mynewsfeed-web [admin]=mynewspaper-admin)
declare -A repo=([web]=mynewspaper-web [admin]=mynewspaper-admin)

case "${1:-}" in
    '' | -h | --help) sed -n '2,10p' "$0"; exit 0 ;;
    --list)
        for s in web admin; do docker images "${repo[$s]}" --format "{{.Repository}}:{{.Tag}}  {{.ID}}  {{.CreatedSince}}" | grep ':before-' || true; done
        exit 0 ;;
esac
sha="$1"
shift
services=("$@")
[[ ${#services[@]} -gt 0 ]] || services=(web admin)

targets=()
for s in "${services[@]}"; do
    [[ -n "${repo[$s]:-}" ]] || { echo "unknown service '$s' (web or admin)" >&2; exit 2; }
    if docker image inspect "${repo[$s]}:before-$sha" >/dev/null 2>&1; then
        targets+=("$s")
    elif [[ $# -gt 0 ]]; then
        echo "No ${repo[$s]}:before-$sha image (deployment/rollback.sh --list)" >&2
        exit 1
    fi
done
[[ ${#targets[@]} -gt 0 ]] || { echo "No before-$sha images (deployment/rollback.sh --list)" >&2; exit 1; }

echo "Rolling back ${targets[*]} to the images that ran before $sha"
for s in "${targets[@]}"; do docker image tag "${repo[$s]}:before-$sha" "${repo[$s]}:latest"; done
docker compose up -d --no-build --pull never --force-recreate "${targets[@]}"

failed=0
for s in "${targets[@]}"; do
    want="$(docker image inspect -f '{{.Id}}' "${repo[$s]}:before-$sha")"
    ok=0
    for _ in $(seq 1 60); do
        if [[ "$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{end}}' "${container[$s]}")" == healthy &&
            "$(docker inspect -f '{{.Image}}' "${container[$s]}")" == "$want" ]]; then ok=1; break; fi
        sleep 2
    done
    if [[ $ok -eq 1 ]]; then
        echo "   ${container[$s]}: healthy on ${want:7:12}"
    else
        echo "   ${container[$s]}: NOT healthy on ${want:7:12}; check docker logs ${container[$s]}" >&2
        failed=1
    fi
done
[[ $failed -eq 0 ]] && echo "Rolled back. Run node tools/live-check.cjs, and record the rollback in release history."
exit $failed
