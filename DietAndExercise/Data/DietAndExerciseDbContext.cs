using Microsoft.EntityFrameworkCore;
using DietAndExercise.Data.Entities;

namespace DietAndExercise.Data;

public class DietAndExerciseDbContext : DbContext
{
    public DietAndExerciseDbContext(DbContextOptions<DietAndExerciseDbContext> options) : base(options) { }

    public DbSet<DayRecordEntity> DayRecords { get; set; } = null!;
    public DbSet<FoodEntry> FoodEntries { get; set; } = null!;
    public DbSet<ExerciseEntry> ExerciseEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DayRecordEntity>(b =>
        {
            b.HasKey(e => e.Id);

            // Map DateOnly to date (persist as SQL date)
            b.Property(e => e.Date)
                .HasConversion(
                    d => d.ToDateTime(TimeOnly.MinValue),
                    dt => DateOnly.FromDateTime(dt))
                .HasColumnType("date");

            // Index and unique constraint to prevent duplicate DayRecords for the same date
            b.HasIndex(e => e.Date).IsUnique();

            // Navigation collections
            b.HasMany(e => e.FoodEntries)
                .WithOne(f => f.DayRecord)
                .HasForeignKey(f => f.DayRecordId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(e => e.ExerciseEntries)
                .WithOne(x => x.DayRecord)
                .HasForeignKey(x => x.DayRecordId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FoodEntry>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Category).IsRequired().HasMaxLength(100);
            b.Property(e => e.Note).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<ExerciseEntry>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).IsRequired().HasMaxLength(200);
            b.Property(e => e.Note).IsRequired().HasMaxLength(1000);
        });
    }
}
