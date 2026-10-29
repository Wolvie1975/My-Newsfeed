using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Data;

// Hand-written half of the scaffolded context; survives re-scaffolding with --force.
public partial class WebScraperContext
{
    // Results tables (db/011, db/013) are written by this app, not the scraper, so they are mapped here by hand rather than
    // scaffolded; re-scaffolding would otherwise drop or duplicate them.
    public virtual DbSet<ResultFollow> ResultFollows { get; set; }

    public virtual DbSet<ResultGame> ResultGames { get; set; }

    public virtual DbSet<ResultFollowTeam> ResultFollowTeams { get; set; }

    public DbSet<SportsRankingSource> SportsRankingSources { get; set; }
    public DbSet<SportsRankingPoll> SportsRankingPolls { get; set; }
    public DbSet<SportsRankingTeam> SportsRankingTeams { get; set; }

    public DbSet<SportsStandingsSource> SportsStandingsSources { get; set; }
    public DbSet<SportsStanding> SportsStandings { get; set; }
    public DbSet<SportsMatchResult> SportsMatchResults { get; set; }

    public DbSet<SportsEventSource> SportsEventSources { get; set; }

    public DbSet<TvSeries> TvSeries { get; set; }
    public DbSet<TvEpisode> TvEpisodes { get; set; }
    public DbSet<TvTrackedSeries> TvTrackedSeries { get; set; }

    public DbSet<Movie> Movies { get; set; }
    public DbSet<MovieRelease> MovieReleases { get; set; }
    public DbSet<MovieReleaseSource> MovieReleaseSources { get; set; }
    public DbSet<MovieSourceLink> MovieSourceLinks { get; set; }
    public DbSet<MovieCatalog> MovieCatalogs { get; set; }

    // Profile tables (db/020) are app-owned and mapped by hand, like the Results tables.
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<ProfileDevice> ProfileDevices { get; set; }
    public DbSet<ProfileTvFollow> ProfileTvFollows { get; set; }
    public DbSet<ProfileChannelFollow> ProfileChannelFollows { get; set; }
    public DbSet<ProfileTopic> ProfileTopics { get; set; }
    public DbSet<ChannelRollout> ChannelRollouts { get; set; }
    public DbSet<ContentRequest> ContentRequests { get; set; }
    public DbSet<Household> Household { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<F1Source>().HasNoKey().ToView("F1Source", "dbo");
        modelBuilder.Entity<F1Race>().HasNoKey().ToView("F1Schedule", "dbo");
        modelBuilder.Entity<F1Classification>().HasNoKey().ToView("F1Results", "dbo");
        modelBuilder.Entity<F1DriverStanding>().HasNoKey().ToView("F1DriverStandings", "dbo");
        modelBuilder.Entity<F1ConstructorStanding>().HasNoKey().ToView("F1ConstructorStandings", "dbo");
        modelBuilder.Entity<MovieCommerceRow>().HasNoKey().ToView("MovieCommerce", "dbo");
        modelBuilder.Entity<TvEpisode>().HasOne(e => e.Series).WithMany(s => s.Episodes)
            .HasForeignKey(e => e.SeriesId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TvTrackedSeries>(entity =>
        {
            entity.HasOne(e => e.Series).WithMany().HasForeignKey(e => e.SeriesId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasSentinel(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
            entity.HasIndex(e => e.TvmazeShowId).IsUnique();
        });
        modelBuilder.Entity<SportsEventsType>().Property(e => e.Enabled).HasDefaultValue(true).HasSentinel(true);
        modelBuilder.Entity<SportsEventSource>(entity =>
        {
            entity.HasOne(e => e.Event).WithMany(e => e.EventSources).HasForeignKey(e => e.SportsEventId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Feed).WithMany(e => e.EventSources).HasForeignKey(e => e.SportsEventsTypeId).OnDelete(DeleteBehavior.NoAction);
        });
        // The database defaults Enabled to 1. Without this, EF treats a CLR `false` as "unset" and would
        // insert the default (true), making it impossible to add a source in the disabled state.
        modelBuilder.Entity<Source>().Property(e => e.Enabled).HasDefaultValue(true).HasSentinel(true);

        modelBuilder.Entity<ResultFollow>(entity =>
        {
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasSentinel(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_ResultFollows_CreatedAt");
        });

        modelBuilder.Entity<ResultFollowTeam>(entity =>
        {
            entity.HasKey(e => new { e.FollowId, e.TeamId }).HasName("PK_ResultFollowTeams");
            entity.HasOne<ResultFollow>().WithMany().HasForeignKey(e => e.FollowId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ResultFollowTeams_Follow");
        });

        modelBuilder.Entity<ResultGame>(entity =>
        {
            entity.Property(e => e.FirstSeenAt).HasDefaultValueSql("(sysutcdatetime())", "DF_ResultGames_FirstSeenAt");
            entity.Property(e => e.LastSeenAt).HasDefaultValueSql("(sysutcdatetime())", "DF_ResultGames_LastSeenAt");
        });

        // Timestamps come from the database defaults; without these EF would insert DateTime.MinValue.
        modelBuilder.Entity<Profile>().Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
        modelBuilder.Entity<ProfileDevice>(entity =>
        {
            entity.HasOne(e => e.Profile).WithMany().HasForeignKey(e => e.ProfileId).OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
            entity.Property(e => e.LastSeenAt).HasDefaultValueSql("sysutcdatetime()");
        });
        modelBuilder.Entity<ProfileTvFollow>().Property(e => e.FollowedAt).HasDefaultValueSql("sysutcdatetime()");
        modelBuilder.Entity<ProfileChannelFollow>().Property(e => e.FollowedAt).HasDefaultValueSql("sysutcdatetime()");
        modelBuilder.Entity<ProfileTopic>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
            entity.HasOne<Profile>().WithMany().HasForeignKey(e => e.ProfileId).OnDelete(DeleteBehavior.Cascade);
        });
        // Single int keys would otherwise be treated as identity columns and left out of inserts.
        modelBuilder.Entity<ChannelRollout>(entity =>
        {
            entity.Property(e => e.YoutubeVideoFeedId).ValueGeneratedNever();
            entity.Property(e => e.RolledOutAt).HasDefaultValueSql("sysutcdatetime()");
        });
        modelBuilder.Entity<ContentRequest>().Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
        modelBuilder.Entity<Household>().Property(e => e.Id).ValueGeneratedNever();
    }
}
