-- Follow the whole Big 12 in college football, men's basketball and women's volleyball, alongside the Kansas follows from
-- 012. Run after 013.
--
-- GroupId is ESPN's Big 12 id in the league its conference lineups come from (see 013): 4 in college football, 8 in men's
-- basketball, and 8 again for volleyball, which reads men's basketball's lineup. The member schools are not listed here:
-- the web app's sync reads them from ESPN on every run, so a school joining or leaving is picked up by itself.
--
-- Safe to run again: a follow that already exists (same sport, league and group) is left as it is.

INSERT INTO dbo.ResultFollows (Kind, Sport, League, GroupId, Name, LogoUrl)
SELECT v.Kind, v.Sport, v.League, v.GroupId, v.Name, v.LogoUrl
FROM (VALUES
    (N'conference', N'football',   N'college-football',          N'4', N'Big 12', N'https://a.espncdn.com/i/teamlogos/ncaa_conf/500/big_12.png'),
    (N'conference', N'basketball', N'mens-college-basketball',   N'8', N'Big 12', N'https://a.espncdn.com/i/teamlogos/ncaa_conf/500/big_12.png'),
    (N'conference', N'volleyball', N'womens-college-volleyball', N'8', N'Big 12', N'https://a.espncdn.com/i/teamlogos/ncaa_conf/500/big_12.png')
) AS v (Kind, Sport, League, GroupId, Name, LogoUrl)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ResultFollows f
    WHERE f.Sport = v.Sport AND f.League = v.League AND f.GroupId = v.GroupId
);
GO
