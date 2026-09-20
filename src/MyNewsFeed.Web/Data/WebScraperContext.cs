using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public partial class WebScraperContext : DbContext
{
    public WebScraperContext(DbContextOptions<WebScraperContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<Source> Sources { get; set; }

    public virtual DbSet<SourceCategory> SourceCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Pages__3214EC277F3F483F");

            entity.Property(e => e.ScrapedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UrlHash)
                .HasComputedColumnSql("(CONVERT([binary](32),hashbytes('SHA2_256',[Url])))", true)
                .IsFixedLength();

            entity.HasOne(d => d.Source).WithMany(p => p.Pages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pages_Sources");
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sources__3214EC273E1779C8");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.UrlHash)
                .HasComputedColumnSql("(CONVERT([binary](32),hashbytes('SHA2_256',[Url])))", true)
                .IsFixedLength();

            entity.HasOne(d => d.SourceCategory).WithMany(p => p.Sources).HasConstraintName("FK_Sources_SourceCategories");
        });

        modelBuilder.Entity<SourceCategory>(entity =>
        {
            entity.Property(e => e.DateAdded).HasDefaultValueSql("(sysutcdatetime())", "DF_SourceCategories_DateAdded");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
