using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace DietAndExercise.Data;

// Design-time factory used by EF tools. It prefers DIET_DB_CONN or ConnectionStrings__DietAndExercise env vars.
// If not present, it falls back to a local SQLite file 'design_time.db' so migrations can be generated.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DietAndExerciseDbContext>
{
    public DietAndExerciseDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<DietAndExerciseDbContext>();

        // Prefer explicit environment variable for migrations
        var conn = Environment.GetEnvironmentVariable("DIET_DB_CONN")
                   ?? Environment.GetEnvironmentVariable("ConnectionStrings__DietAndExercise");

        if (!string.IsNullOrEmpty(conn))
        {
            builder.UseNpgsql(conn);
        }
        else
        {
            // Fallback to SQLite file for design-time only (relational provider required for migrations)
            var dbPath = System.IO.Path.Combine(Environment.CurrentDirectory, "design_time.db");
            var sqliteConn = $"Data Source={dbPath}";
            builder.UseSqlite(sqliteConn);
        }

        return new DietAndExerciseDbContext(builder.Options);
    }
}
