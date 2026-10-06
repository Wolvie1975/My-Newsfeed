using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

// Hand-written half of the scaffolded context; survives re-scaffolding with --force.
public partial class WebScraperContext
{
    // Results tables (db/011, db/013) are written by this app, not the scraper, so they are mapped here by hand rather than
    // scaffolded; re-scaffolding would otherwise drop or duplicate them.
    public virtual DbSet<ResultFollow> ResultFollows { get; set; }

    public virtual DbSet<ResultGame> ResultGames { get; set; }

    public virtual DbSet<ResultFollowTeam> ResultFollowTeams { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
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
    }
}
