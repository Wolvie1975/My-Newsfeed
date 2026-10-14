-- Conference follows for the results page: follow a whole college conference (the Big 12) in one sport instead of one
-- team at a time.
--
--   dbo.ResultFollows.GroupId   ESPN's conference ("group") id, set only on a conference follow. It is numbered in the
--                               league ESPN's conference lineups are read from (EspnLeagues.ConferencesFrom): football
--                               and men's basketball use their own, other college sports use men's basketball's, because
--                               ESPN does not keep those lineups current. So a Big 12 volleyball follow has GroupId 8,
--                               men's basketball's Big 12.
--   dbo.ResultFollowTeams       the member schools of a conference follow, as ESPN team ids, replaced by the web app's
--                               sync on every run. The results page matches games to a conference through these.
--
-- Notes
--   * Kind is now 'team', 'league' or 'conference'. A conference has a GroupId and no TeamId; the others have no GroupId.
--   * The unique key gains GroupId, so a conference can be followed once per league, alongside the whole league.
-- Run once, after 011.

ALTER TABLE dbo.ResultFollows ADD GroupId nvarchar(20) NULL;
GO

ALTER TABLE dbo.ResultFollows DROP CONSTRAINT CK_ResultFollows_Kind;
ALTER TABLE dbo.ResultFollows ADD CONSTRAINT CK_ResultFollows_Kind CHECK (
       (Kind = N'team'       AND TeamId IS NOT NULL AND GroupId IS NULL)
    OR (Kind = N'league'     AND TeamId IS NULL     AND GroupId IS NULL)
    OR (Kind = N'conference' AND TeamId IS NULL     AND GroupId IS NOT NULL));

ALTER TABLE dbo.ResultFollows DROP CONSTRAINT UQ_ResultFollows_Team;
ALTER TABLE dbo.ResultFollows ADD CONSTRAINT UQ_ResultFollows_Team UNIQUE (Sport, League, TeamId, GroupId);
GO

CREATE TABLE dbo.ResultFollowTeams (
    FollowId int          NOT NULL CONSTRAINT FK_ResultFollowTeams_Follow REFERENCES dbo.ResultFollows (ID) ON DELETE CASCADE,
    TeamId   nvarchar(20) NOT NULL,
    CONSTRAINT PK_ResultFollowTeams PRIMARY KEY (FollowId, TeamId)
);
GO
