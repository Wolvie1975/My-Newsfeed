using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

// Hand-written half of the scaffolded context; survives re-scaffolding with --force.
public partial class WebScraperContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // The database defaults Enabled to 1. Without this, EF treats a CLR `false` as "unset" and would
        // insert the default (true), making it impossible to add a source in the disabled state.
        modelBuilder.Entity<Source>().Property(e => e.Enabled).HasDefaultValue(true).HasSentinel(true);
    }
}
