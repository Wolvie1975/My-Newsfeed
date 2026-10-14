-- Run after 013. Soccer uses separate team IDs; the sync maps the basketball conference schools by exact ESPN names.
-- Idempotent; existing enabled/disabled settings are preserved.
INSERT INTO dbo.ResultFollows (Kind, Sport, League, GroupId, Name, LogoUrl)
SELECT N'conference', N'soccer', N'usa.ncaa.w.1', N'8', N'Big 12', N'https://a.espncdn.com/i/teamlogos/ncaa_conf/500/big_12.png'
WHERE NOT EXISTS (SELECT 1 FROM dbo.ResultFollows WHERE Sport=N'soccer' AND League=N'usa.ncaa.w.1' AND GroupId=N'8');
