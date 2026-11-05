#!/usr/bin/env bash
# Removes what a browser or live check left behind (AGENTS.md, "Browser tests clean up their own data"):
# every `zz ` profile with its devices (the delete cascades to its follows, Saved list, topics and requests), and the
# unassigned devices created since the run's recorded start time. Nothing else.
#
#   tools/sql/cleanup-zz.sh 2026-11-05T01:30:00                   show what would be removed (live database)
#   tools/sql/cleanup-zz.sh 2026-11-05T01:30:00 --apply           remove it, then run household-check.sh
#   tools/sql/cleanup-zz.sh 2026-11-05T01:30:00 --db WebScraper_Test --apply
#
# The start time is UTC, recorded before the run (SELECT SYSUTCDATETIME()). It must be within the last 7 days, so an
# old or mistyped value can't sweep up real devices. Refuses if the household admin profile is a `zz ` profile.
# Prefer Admin's profile delete for profiles during a run; this script is the teardown and the safety net.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

start="${1:-}"
if [[ "$start" == "-h" || "$start" == "--help" ]]; then
    sed -n '2,13p' "$0"
    exit 0
fi
[[ "$start" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}[T\ ][0-9]{2}:[0-9]{2}(:[0-9]{2}(\.[0-9]+)?)?$ ]] ||
    die "first argument must be the run's UTC start time, like 2026-11-05T01:30:00 (see --help)"
shift

database=WebScraper
apply=0
while [[ $# -gt 0 ]]; do
    case "$1" in
        --db) database="${2:?--db needs a database name}"; shift 2 ;;
        --apply) apply=1; shift ;;
        *) die "unknown argument '$1'" ;;
    esac
done

run_sql "$database" <<SQL | awk -F'|' '$1 == "item" || $1 ~ /^-+$/ { next } NF == 2 { printf "%-24s %s\n", $1 ":", $2; next } { print }'
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @start datetime2 = N'$start';
IF @start < DATEADD(day, -7, SYSUTCDATETIME()) OR @start > SYSUTCDATETIME()
    THROW 50010, N'The start time must be within the last 7 days and not in the future.', 1;
IF EXISTS (SELECT 1 FROM dbo.MyDailyFeedHousehold AS h JOIN dbo.MyDailyFeedProfiles AS p ON p.Id = h.AdminProfileId
           WHERE p.Name LIKE N'zz %')
    THROW 50011, N'The household admin profile is a zz profile. Restore the real admin profile first.', 1;

SELECT 'zz profiles' AS item, CAST(COUNT(*) AS varchar(20)) AS value FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %'
UNION ALL SELECT 'their devices', CAST(COUNT(*) AS varchar(20)) FROM dbo.MyDailyFeedProfileDevices
    WHERE ProfileId IN (SELECT Id FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %')
UNION ALL SELECT 'unassigned since start', CAST(COUNT(*) AS varchar(20)) FROM dbo.MyDailyFeedProfileDevices
    WHERE ProfileId IS NULL AND CreatedAt >= @start;

IF $apply = 1
BEGIN
    BEGIN TRANSACTION;
    DELETE FROM dbo.MyDailyFeedProfileDevices
        WHERE ProfileId IN (SELECT Id FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %');
    DELETE FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %';
    DELETE FROM dbo.MyDailyFeedProfileDevices WHERE ProfileId IS NULL AND CreatedAt >= @start;
    COMMIT TRANSACTION;
    PRINT 'Removed.';
END
ELSE
    PRINT 'Dry run: nothing removed. Add --apply to remove these.';
SQL

if [[ "$apply" == 1 ]]; then
    "$(dirname "${BASH_SOURCE[0]}")/household-check.sh" --db "$database" --since "$start"
fi
