-- The leagues and teams the public results page follows: NFL, MLB, MLS and NWSL as whole leagues, and Kansas (ESPN team
-- id 2305) in college football, men's basketball and women's volleyball. Run after 011.
--
-- Safe to run again: a follow that already exists (same sport, league and team) is left as it is, so one you have
-- disabled or renamed in the admin is never changed back. The background results sync fetches each new follow's games
-- from ESPN within a minute or two of the site running; nothing else needs to be loaded.

INSERT INTO dbo.ResultFollows (Kind, Sport, League, TeamId, Name, LogoUrl)
SELECT v.Kind, v.Sport, v.League, v.TeamId, v.Name, v.LogoUrl
FROM (VALUES
    (N'league', N'football',   N'nfl',                       NULL,    N'NFL',             NULL),
    (N'league', N'baseball',   N'mlb',                       NULL,    N'MLB',             NULL),
    (N'league', N'soccer',     N'usa.1',                     NULL,    N'MLS',             NULL),
    (N'league', N'soccer',     N'usa.nwsl',                  NULL,    N'NWSL',            NULL),
    (N'team',   N'football',   N'college-football',          N'2305', N'Kansas Jayhawks', N'https://a.espncdn.com/i/teamlogos/ncaa/500/2305.png'),
    (N'team',   N'basketball', N'mens-college-basketball',   N'2305', N'Kansas Jayhawks', N'https://a.espncdn.com/i/teamlogos/ncaa/500/2305.png'),
    (N'team',   N'volleyball', N'womens-college-volleyball', N'2305', N'Kansas Jayhawks', N'https://a.espncdn.com/i/teamlogos/ncaa/500/2305.png')
) AS v (Kind, Sport, League, TeamId, Name, LogoUrl)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ResultFollows f
    WHERE f.Sport = v.Sport AND f.League = v.League
      AND (f.TeamId = v.TeamId OR (f.TeamId IS NULL AND v.TeamId IS NULL))
);
GO
