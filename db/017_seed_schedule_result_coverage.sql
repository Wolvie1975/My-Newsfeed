-- Match the schedule's WNBA and Big 12 women's basketball coverage.
-- Existing follows, including intentionally disabled follows, are preserved.
INSERT INTO dbo.ResultFollows (Kind, Sport, League, GroupId, Name, LogoUrl)
SELECT v.Kind, v.Sport, v.League, v.GroupId, v.Name, v.LogoUrl
FROM (VALUES
 (N'league', N'basketball', N'wnba', CAST(NULL AS nvarchar(50)), N'WNBA', CAST(NULL AS nvarchar(2048))),
 (N'conference', N'basketball', N'womens-college-basketball', N'8', N'Big 12', N'https://a.espncdn.com/i/teamlogos/ncaa_conf/500/big_12.png')
) v(Kind, Sport, League, GroupId, Name, LogoUrl)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ResultFollows f
 WHERE f.Sport=v.Sport AND f.League=v.League AND f.Kind=v.Kind
 AND (f.GroupId=v.GroupId OR (f.GroupId IS NULL AND v.GroupId IS NULL)));
