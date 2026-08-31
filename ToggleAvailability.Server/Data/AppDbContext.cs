using Microsoft.EntityFrameworkCore;
using ToggleAvailability.Server.Models;

namespace ToggleAvailability.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }


    public DbSet<User> Users =>
        Set<User>();


    public DbSet<OfficeHistory> OfficeHistories =>
        Set<OfficeHistory>();


    public DbSet<OfficeHistoryOutOfOffice> OfficeHistoryOutOfOffice =>
        Set<OfficeHistoryOutOfOffice>();


    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);


        // ==================================================
        // User
        // ==================================================

        modelBuilder.Entity<User>(
            entity =>
            {
                entity.HasKey(
                    x => x.UserId);


                entity.Property(
                    x => x.UserId)
                    .ValueGeneratedOnAdd();


                entity.Property(
                    x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);


                entity.Property(
                    x => x.Status)
                    .HasConversion<string>()
                    .IsRequired();


                entity.Property(
                    x => x.IsAvailable)
                    .IsRequired();


                entity.Property(
                    x => x.IsActiveUser)
                    .IsRequired();


                // --------------------------------------------------
                // TotalTimeInOffice is calculated from OfficeHistory.
                // --------------------------------------------------

                entity.Ignore(
                    x => x.TotalTimeInOffice);
            });


        // ==================================================
        // Office History
        // ==================================================

        modelBuilder.Entity<OfficeHistory>(
            entity =>
            {
                // --------------------------------------------------
                // One history record per user per day.
                // --------------------------------------------------

                entity.HasKey(
                    x => new
                    {
                        x.UserId,
                        x.Date
                    });


                entity.Property(
                    x => x.Date)
                    .HasConversion(
                        date =>
                            date.ToString("yyyy-MM-dd"),

                        value =>
                            DateOnly.Parse(value))
                    .IsRequired();


                entity.Property(
                    x => x.TimeInOffice)
                    .HasConversion<long>()
                    .IsRequired();


                entity.Property(
                    x => x.StartTime);


                // --------------------------------------------------
                // User relationship
                // --------------------------------------------------

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(
                        x => x.UserId)
                    .OnDelete(
                        DeleteBehavior.Cascade);


                // --------------------------------------------------
                // Out-of-office relationship
                // --------------------------------------------------

                entity.HasMany(
                        x => x.OutOfOfficeEntries)
                    .WithOne(
                        x => x.OfficeHistory)
                    .HasForeignKey(
                        x => new
                        {
                            x.UserId,
                            x.Date
                        })
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });


        // ==================================================
        // Office History — Out of Office
        // ==================================================

        modelBuilder.Entity<OfficeHistoryOutOfOffice>(
            entity =>
            {
                // --------------------------------------------------
                // One row per user + date + status.
                // --------------------------------------------------

                entity.HasKey(
                    x => new
                    {
                        x.UserId,
                        x.Date,
                        x.Status
                    });


                entity.Property(
                    x => x.Date)
                    .HasConversion(
                        date =>
                            date.ToString("yyyy-MM-dd"),

                        value =>
                            DateOnly.Parse(value))
                    .IsRequired();


                entity.Property(
                    x => x.Status)
                    .HasConversion<string>()
                    .IsRequired();


                entity.Property(
                    x => x.Duration)
                    .HasConversion<long>()
                    .IsRequired();


                // --------------------------------------------------
                // Parent OfficeHistory relationship
                // --------------------------------------------------

                entity.HasOne(
                        x => x.OfficeHistory)
                    .WithMany(
                        x => x.OutOfOfficeEntries)
                    .HasForeignKey(
                        x => new
                        {
                            x.UserId,
                            x.Date
                        })
                    .OnDelete(
                        DeleteBehavior.Cascade);
            });
    }
}