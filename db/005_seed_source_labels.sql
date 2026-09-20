-- Publication names shown on the public feed. Only fills labels that are still empty, matched by URL,
-- so a name you have set yourself in the admin is never overwritten.

UPDATE dbo.Sources SET Label = N'Polygon'           WHERE Label IS NULL AND Url = N'https://www.polygon.com/';
UPDATE dbo.Sources SET Label = N'Engadget'          WHERE Label IS NULL AND Url = N'https://www.engadget.com/';
UPDATE dbo.Sources SET Label = N'Linux Today'       WHERE Label IS NULL AND Url = N'https://www.linuxtoday.com/';
UPDATE dbo.Sources SET Label = N'9to5Linux'         WHERE Label IS NULL AND Url = N'https://9to5linux.com/';
UPDATE dbo.Sources SET Label = N'Phoronix'          WHERE Label IS NULL AND Url = N'https://www.phoronix.com/';
UPDATE dbo.Sources SET Label = N'GamingOnLinux'     WHERE Label IS NULL AND Url = N'https://www.gamingonlinux.com/';
UPDATE dbo.Sources SET Label = N'It''s FOSS'        WHERE Label IS NULL AND Url = N'https://itsfoss.com/';
UPDATE dbo.Sources SET Label = N'Linuxiac'          WHERE Label IS NULL AND Url = N'https://linuxiac.com/';
UPDATE dbo.Sources SET Label = N'XDA Developers'    WHERE Label IS NULL AND Url = N'https://www.xda-developers.com/';
UPDATE dbo.Sources SET Label = N'ESPN'              WHERE Label IS NULL AND Url = N'https://www.espn.com/espn/rss/news';
UPDATE dbo.Sources SET Label = N'CBS Sports'        WHERE Label IS NULL AND Url = N'https://www.cbssports.com/rss/headlines/';
UPDATE dbo.Sources SET Label = N'Deadline'          WHERE Label IS NULL AND Url = N'https://deadline.com/';
UPDATE dbo.Sources SET Label = N'TVLine'            WHERE Label IS NULL AND Url = N'https://www.tvline.com/';
UPDATE dbo.Sources SET Label = N'How-To Geek'       WHERE Label IS NULL AND Url = N'https://www.howtogeek.com/';
UPDATE dbo.Sources SET Label = N'/Film'             WHERE Label IS NULL AND Url = N'https://www.slashfilm.com/';
UPDATE dbo.Sources SET Label = N'Android Authority' WHERE Label IS NULL AND Url = N'https://www.androidauthority.com/';
GO
