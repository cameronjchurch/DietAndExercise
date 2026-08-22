using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DietAndExercise.Data;

namespace DietAndExercise.Tests;

public class DataImporterTests
{
    private DietAndExerciseDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DietAndExerciseDbContext(options);
    }

    private string CreateSampleMd(string folder, string fileName, string content)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task DryRun_ShouldParseFilesButNotPersist()
    {
        using var db = CreateInMemoryDb();
        var temp = Path.Combine(Path.GetTempPath(), "dietimporttest_" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        var backup = Path.Combine(temp, "backup");
        Directory.CreateDirectory(backup);

        var sample1 = "##### Weight\n229.2\n\n##### Food\n- Breakfast: eggs, bagel\n##### Exercise\n- Walking";

        var sample2 = "##### Weight\n227.0\n##### Food\n- Lunch: salad\n##### Exercise\n- Running";

        CreateSampleMd(temp, "2026-01-01.md", sample1);
        CreateSampleMd(temp, "2026-01-02.md", sample2);

        var logger = NullLogger<DataImporter>.Instance;
        var importer = new DataImporter(db, backup, logger);
        var csvPath = Path.Combine(temp, "report.csv");
        var report = await importer.ImportFromMarkdownAsync(temp, dryRun: true, csvReportPath: csvPath);

        Assert.Equal(2, report.ImportedCount);
        Assert.Empty(db.DayRecords);
        Assert.True(File.Exists(csvPath));
        var csv = File.ReadAllText(csvPath);
        Assert.Contains("Imported", csv);
    }

    [Fact]
    public async Task Import_ShouldPersistAndMoveFiles()
    {
        using var db = CreateInMemoryDb();
        var temp = Path.Combine(Path.GetTempPath(), "dietimporttest_" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        var backup = Path.Combine(temp, "backup");
        Directory.CreateDirectory(backup);

        var sample1 = "##### Weight\n229.2\n\n##### Food\n- Breakfast: eggs, bagel\n##### Exercise\n- Walking";

        CreateSampleMd(temp, "2026-01-03.md", sample1);

        var logger = NullLogger<DataImporter>.Instance;
        var importer = new DataImporter(db, backup, logger);
        var report = await importer.ImportFromMarkdownAsync(temp, dryRun: false, csvReportPath: null);

        Assert.Equal(1, report.ImportedCount);
        Assert.Equal(1, await db.DayRecords.CountAsync());
        var moved = Directory.GetFiles(backup, "*.md", SearchOption.AllDirectories);
        Assert.NotEmpty(moved);
    }
}
