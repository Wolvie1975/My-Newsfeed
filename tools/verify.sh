#!/usr/bin/env bash
# The pre-commit suite in one command, with one pass/fail summary. Run it before every commit (coding-standards.md,
# Releases). Logs go to test-results/verify/.
#
#   tools/verify.sh            build (0 warnings), all .NET tests against WebScraper_Test, JS unit tests, lint, format
#   tools/verify.sh --quick    build, JS unit tests and the repository hygiene tests (about a minute)
#
# The .NET tests need a connection string: user secrets, ConnectionStrings__DefaultConnection, or (used here when
# neither is set) SQL_CONNECTION from .env. TestDatabase always switches it to WebScraper_Test.
# Not included: `dotnet format --verify-no-changes`, which reports CS8618 noise on WebScraperContext (see the handoff).

set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

quick=0
case "${1:-}" in
    --quick) quick=1 ;;
    -h | --help) sed -n '2,10p' "$0"; exit 0 ;;
    '') ;;
    *) echo "unknown argument '$1'" >&2; exit 2 ;;
esac

logs=test-results/verify
mkdir -p "$logs"

if [[ -z "${ConnectionStrings__DefaultConnection:-}" && -r .env ]] &&
    ! dotnet user-secrets list --project src/myNewspaper.Web 2>/dev/null | grep -q '^ConnectionStrings:DefaultConnection'; then
    ConnectionStrings__DefaultConnection="$(grep -m1 '^SQL_CONNECTION=' .env | cut -d= -f2- | sed "s/^['\"]//; s/['\"]$//")"
    export ConnectionStrings__DefaultConnection
fi

names=()
results=()
failed=0

# step <name> <command…>: runs it, logs output, records pass/fail and time.
step() {
    local name="$1" log started status
    shift
    log="$logs/$(tr ' /' '--' <<<"$name").log"
    started=$SECONDS
    printf '%-22s ' "$name"
    "$@" >"$log" 2>&1
    status=$?
    if [[ $status -eq 0 ]]; then
        printf 'pass  %4ss\n' "$((SECONDS - started))"
        results+=("pass")
    else
        printf 'FAIL  %4ss  (%s)\n' "$((SECONDS - started))" "$log"
        tail -n 15 "$log" | sed 's/^/    /'
        results+=("FAIL")
        failed=1
    fi
    names+=("$name")
}

step "dotnet build" dotnet build myNewspaper.sln -nologo -clp:NoSummary
if [[ $quick -eq 1 ]]; then
    step "hygiene tests" dotnet test tests/myNewspaper.Tests/myNewspaper.Tests.csproj --no-build \
        --filter 'FullyQualifiedName~RepositoryHygieneTests'
else
    step "dotnet test" dotnet test myNewspaper.sln --no-build
fi
step "js unit tests" npm run --silent test:unit
if [[ $quick -eq 0 ]]; then
    step "lint" npm run --silent lint
    step "format check" npm run --silent format:check
fi

echo
if [[ $failed -eq 0 ]]; then
    echo "verify: all ${#names[@]} steps passed$([[ $quick -eq 1 ]] && echo ' (quick)')"
else
    echo "verify: FAILED"
fi
exit $failed
