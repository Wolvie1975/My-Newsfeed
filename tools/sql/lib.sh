# Shared helper for the tools/sql scripts. Source it; don't run it.
#
# Reads SQL_CONNECTION from the repository's .env and runs sqlcmd inside the SQL Server container. The password only
# ever travels as the SQLCMDPASSWORD environment variable: never on a command line, where it would show up in
# permission prompts, transcripts, shell history and the process list (AGENTS.md).
#
#   SQL_CONTAINER   container that has sqlcmd (default sql2025)
#   ENV_FILE        file holding SQL_CONNECTION (default <repo>/.env)

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
sql_container="${SQL_CONTAINER:-sql2025}"
env_file="${ENV_FILE:-$repo_root/.env}"

die() {
    echo "error: $*" >&2
    exit 1
}

# Value of one Key=Value part of SQL_CONNECTION (case-insensitive key).
connection_part() {
    local key="$1" conn
    conn="$(grep -m1 '^SQL_CONNECTION=' "$env_file" | cut -d= -f2-)" || true
    [[ -n "$conn" ]] || die "SQL_CONNECTION not found in $env_file"
    tr ';' '\n' <<<"$conn" | awk -v k="$key" -F= 'tolower($1) == tolower(k) { sub(/^[^=]*=/, ""); print; exit }'
}

# Only the test copy and the live database are valid targets.
check_database() {
    case "$1" in
        WebScraper | WebScraper_Test) ;;
        *) die "unknown database '$1' (use WebScraper or WebScraper_Test)" ;;
    esac
}

# run_sql <database>: runs the SQL on stdin, prints '|'-separated rows, fails on any SQL error.
run_sql() {
    local database="$1" user
    check_database "$database"
    [[ -r "$env_file" ]] || die "can't read $env_file"
    user="$(connection_part 'User Id')"
    [[ -n "$user" ]] || die "SQL_CONNECTION has no User Id"
    SQLCMDPASSWORD="$(connection_part 'Password')"
    export SQLCMDPASSWORD
    docker exec -i -e SQLCMDPASSWORD "$sql_container" /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U "$user" -d "$database" -C -b -W -s '|' -i /dev/stdin
}
