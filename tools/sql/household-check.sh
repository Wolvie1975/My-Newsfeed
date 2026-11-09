#!/usr/bin/env bash
# Read-only report of the household: profiles, the admin profile, devices and anything a test run may have left
# behind. Run it after every browser or live check (AGENTS.md, "Browser tests clean up their own data").
#
#   tools/sql/household-check.sh                                   live database (WebScraper)
#   tools/sql/household-check.sh --db WebScraper_Test              the test copy
#   tools/sql/household-check.sh --since 2026-11-05T01:30:00       also count unassigned devices created since then (UTC)
#
# Exits 1 if any `zz ` profile is left, or if the admin profile points at a missing or `zz ` profile.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

database=WebScraper
since=
while [[ $# -gt 0 ]]; do
    case "$1" in
        --db) database="${2:?--db needs a database name}"; shift 2 ;;
        --since) since="${2:?--since needs a UTC time}"; shift 2 ;;
        -h | --help) sed -n '2,9p' "$0"; exit 0 ;;
        *) die "unknown argument '$1'" ;;
    esac
done
[[ -z "$since" || "$since" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}([T\ ][0-9]{2}:[0-9]{2}(:[0-9]{2}(\.[0-9]+)?)?)?$ ]] ||
    die "--since must look like 2026-11-05T01:30:00 (UTC)"

report="$(
    run_sql "$database" <<SQL
SET NOCOUNT ON;
DECLARE @since datetime2 = NULLIF(N'$since', N'');
DECLARE @admin int = (SELECT TOP (1) AdminProfileId FROM dbo.MyDailyFeedHousehold);
SELECT 'database' AS item, DB_NAME() AS value
UNION ALL SELECT 'profiles', CAST(COUNT(*) AS varchar(20)) FROM dbo.MyDailyFeedProfiles
UNION ALL SELECT 'zz profiles', CAST(COUNT(*) AS varchar(20)) FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %'
UNION ALL SELECT 'admin profile', COALESCE((SELECT CONCAT(Name, ' (Id ', Id, ')') FROM dbo.MyDailyFeedProfiles WHERE Id = @admin),
                                           CASE WHEN @admin IS NULL THEN 'none' ELSE CONCAT('missing (Id ', @admin, ')') END)
UNION ALL SELECT 'devices', CAST(COUNT(*) AS varchar(20)) FROM dbo.MyDailyFeedProfileDevices
UNION ALL SELECT 'unassigned devices', CAST(COUNT(*) AS varchar(20)) FROM dbo.MyDailyFeedProfileDevices WHERE ProfileId IS NULL
UNION ALL SELECT 'unassigned since', CASE WHEN @since IS NULL THEN '-' ELSE CAST((SELECT COUNT(*) FROM dbo.MyDailyFeedProfileDevices
                                     WHERE ProfileId IS NULL AND CreatedAt >= @since) AS varchar(20)) END;
SELECT 'profile' AS item, CONCAT(Id, ' ', Name, CASE WHEN Id = @admin THEN ' [admin]' ELSE '' END, CASE WHEN Hidden = 1 THEN ' [hidden]' ELSE '' END) AS value
FROM dbo.MyDailyFeedProfiles ORDER BY SortOrder, Id;
SQL
)"

# Drop sqlcmd's header and dashes lines, print "item: value".
awk -F'|' '$1 != "item" && $1 !~ /^-+$/ && NF == 2 { printf "%-20s %s\n", $1 ":", $2 }' <<<"$report"

status=0
if grep -q '^zz profiles|[1-9]' <<<"$report"; then
    echo "FAIL: zz profiles are left (tools/sql/cleanup-zz.sh)" >&2
    status=1
fi
if grep -Eq '^admin profile\|(missing|zz )' <<<"$report"; then
    echo "FAIL: the household admin profile is missing or is a zz test profile" >&2
    status=1
fi
exit "$status"
