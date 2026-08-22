using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DietAndExercise.Data;
using DietAndExercise.Data.Entities;
using DietAndExercise.Services;

public class IntegrationTests
{
    [Fact]
    public void DbContext_ModelCreation()
    {
        var options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new DietAndExerciseDbContext(options);
        var model = db.Model;

        Assert.NotNull(model.FindEntityType(typeof(DayRecordEntity)));
        Assert.NotNull(model.FindEntityType(typeof(FoodEntry)));
        Assert.NotNull(model.FindEntityType(typeof(ExerciseEntry)));
    }

    [Fact]
    public void EfDietAndExerciseService_GetHistory_ReturnsMappedRecords()
    {
        var options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new DietAndExerciseDbContext(options);

        var entity = new DayRecordEntity
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            WeightLb = 180.5,
            CaloriesConsumed = 2000,
            CaloriesBurned = 500,
            FoodIntakeNotes = string.Empty,
            ExerciseNotes = string.Empty
        };
        entity.FoodEntries.Add(new FoodEntry { Note = "Apple" });
        entity.ExerciseEntries.Add(new ExerciseEntry { Name = "Run", Note = "5km" });

        db.DayRecords.Add(entity);
        db.SaveChanges();

        var svc = new EfDietAndExerciseService(db);
        var result = svc.GetHistory();

        Assert.Single(result);
        var rec = result.First();
        Assert.Equal(entity.Date, rec.Date);
        Assert.Equal(entity.WeightLb, rec.WeightLb);
        Assert.Contains("Apple", rec.FoodIntakeNotes);
        Assert.Contains("Run", rec.ExerciseNotes);
    }

    [Fact]
    public async Task DataImporter_ImportFromMarkdownAsync_ImportsFilesAndBacksUp()
    {
        var options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var repoPath = Path.Combine(Path.GetTempPath(), "dae_import_test_repo_" + Guid.NewGuid().ToString("N"));
        var backupRoot = Path.Combine(Path.GetTempPath(), "dae_import_backup_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(repoPath);
        Directory.CreateDirectory(backupRoot);

        try
        {
            var date1 = DateOnly.FromDateTime(DateTime.UtcNow);
            var file1 = Path.Combine(repoPath, date1.ToString("yyyy-MM-dd") + ".md");
            await File.WriteAllTextAsync(file1, "##### Weight\n180\n##### Food\n- Apple\n##### Exercise\n- Run: 5km\n");

            var date2 = date1.AddDays(-1);
            var file2 = Path.Combine(repoPath, date2.ToString("yyyy-MM-dd") + ".md");
            await File.WriteAllTextAsync(file2, "##### Weight\n179\n##### Food\n- Banana\n##### Exercise\n- Bike: 10mi\n");

            using var db = new DietAndExerciseDbContext(options);
            var importer = new DataImporter(db, backupRoot);

            var imported = await importer.ImportFromMarkdownAsync(repoPath);

            Assert.Equal(2, imported);

            // Ensure files were moved into a timestamped folder under backupRoot
            var backupContents = Directory.GetDirectories(backupRoot, "*", SearchOption.TopDirectoryOnly);
            Assert.NotEmpty(backupContents);

            var movedFiles = Directory.GetFiles(backupRoot, "*.md", SearchOption.AllDirectories);
            Assert.Equal(2, movedFiles.Length);
        }
        finally
        {
            // cleanup
            try { Directory.Delete(repoPath, recursive: true); } catch { }
            try { Directory.Delete(backupRoot, recursive: true); } catch { }
        }
    }
}
