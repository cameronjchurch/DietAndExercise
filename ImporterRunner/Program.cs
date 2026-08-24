using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DietAndExercise.Data;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("ImporterRunner: starting import runner...");

        string repoPath = @"D:\Nextcloud\Notes\Cameron\Diet and Exercise";
        string csvReport = @"C:\temp\diet_import_report.csv";
        var backupRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dae_import_backup_runner");

        // If DIET_DB_CONN is set, run a real import against Postgres. Otherwise perform a dry-run using InMemory DB.
        var dietConn = Environment.GetEnvironmentVariable("DIET_DB_CONN");
        bool realImport = !string.IsNullOrWhiteSpace(dietConn);

        DbContextOptions<DietAndExerciseDbContext> options;
        if (realImport)
        {
            Console.WriteLine("DIET_DB_CONN detected — running real import against Postgres.");
            options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
                .UseNpgsql(dietConn)
                .Options;
        }
        else
        {
            Console.WriteLine("No DIET_DB_CONN found — performing dry-run in InMemory database.");
            options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
                .UseInMemoryDatabase("ImporterRunnerDryRun")
                .Options;
        }

        using var db = new DietAndExerciseDbContext(options);
        var importer = new DataImporter(db, backupRoot);

        try
        {
            Console.WriteLine($"Reading markdown files from: {repoPath}");
            var report = await importer.ImportFromMarkdownAsync(repoPath, dryRun: !realImport, csvReportPath: csvReport);

            Console.WriteLine($"Import complete. ImportedCount: {report.ImportedCount}, SkippedCount: {report.SkippedCount}");
            Console.WriteLine("Report files:");
            if (System.IO.File.Exists(csvReport)) Console.WriteLine($"  CSV: {csvReport}");
            else Console.WriteLine("  CSV not generated.");

            Console.WriteLine("Sample imported files:");
            foreach (var f in report.ImportedFiles.Take(10)) Console.WriteLine("  " + f);

            // Print skipped files with reasons
            Console.WriteLine("Sample skipped files (reason):");
            foreach (var s in report.SkippedFiles.Take(20)) Console.WriteLine($"  {s.File}  -> {s.Reason}");

            // Print errors
            if (report.Errors.Count > 0)
            {
                Console.WriteLine("Errors:");
                foreach (var e in report.Errors.Take(20)) Console.WriteLine($"  {e.File}  -> {e.Error}");
            }

            // Print DB counts (helpful to verify what was written)
            try
            {
                var dayCount = await db.DayRecords.CountAsync();
                var foodCount = await db.FoodEntries.CountAsync();
                var exerciseCount = await db.ExerciseEntries.CountAsync();
                Console.WriteLine($"DB counts: DayRecords={dayCount}, FoodEntries={foodCount}, ExerciseEntries={exerciseCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to query DB counts: {ex.Message}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Import failed: " + ex.Message);
            return 2;
        }
    }
}
