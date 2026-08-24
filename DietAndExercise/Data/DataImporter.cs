using DietAndExercise.Data.Entities;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace DietAndExercise.Data;

public class DataImporter
{
    private readonly DietAndExerciseDbContext _db;
    private readonly string _backupRoot;
    private readonly ILogger<DataImporter> _logger;

    // Back-compat constructor: uses a null logger instance
    public DataImporter(DietAndExerciseDbContext db, string backupRoot) : this(db, backupRoot, Microsoft.Extensions.Logging.Abstractions.NullLogger<DataImporter>.Instance)
    {
    }

    public DataImporter(DietAndExerciseDbContext db, string backupRoot, ILogger<DataImporter> logger)
    {
        _db = db;
        _backupRoot = backupRoot;
        _logger = logger;
    }

    public class ImportReport
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> ImportedFiles { get; set; } = new();
        public List<(string File, string Reason)> SkippedFiles { get; set; } = new();
        public List<(string File, string Error)> Errors { get; set; } = new();
    }

    /// <summary>
    /// Imports markdown files from the given repoPath. If dryRun is true, no changes are persisted and files are not moved.
    /// Optionally writes a CSV report to csvReportPath.
    /// </summary>
    public async Task<ImportReport> ImportFromMarkdownAsync(string repoPath, bool dryRun = false, string? csvReportPath = null)
    {
        var report = new ImportReport();

        if (!Directory.Exists(repoPath))
            throw new DirectoryNotFoundException(repoPath);

        try
        {
            Directory.CreateDirectory(_backupRoot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create backup root {BackupRoot}", _backupRoot);
        }

        var files = Directory.GetFiles(repoPath, "*.md", SearchOption.AllDirectories);
        _logger.LogInformation("Starting import from {RepoPath}. Files found: {Count}. DryRun: {DryRun}", repoPath, files.Length, dryRun);

        foreach (var file in files)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                string currentSection = string.Empty;

                double? parsedWeight = null;
                var foods = new List<string>();
                var exercises = new List<KeyValuePair<string, string>>();

                foreach (var raw in lines)
                {
                    var line = raw?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("#####", StringComparison.Ordinal))
                    {
                        currentSection = line[5..].Trim().ToLowerInvariant();
                        continue;
                    }

                    if (currentSection == "weight")
                    {
                        var token = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (!string.IsNullOrEmpty(token) && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) parsedWeight = w;
                        else if (double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out var w2)) parsedWeight = w2;
                    }
                    else if (currentSection == "food")
                    {
                        if (line.StartsWith('-'))
                        {
                            var content = line.TrimStart('-', ' ').Trim();
                            if (!string.IsNullOrEmpty(content)) foods.Add(content);
                        }
                    }
                    else if (currentSection == "exercise")
                    {
                        if (line.StartsWith('-'))
                        {
                            var content = line.TrimStart('-', ' ').Trim();
                            if (!string.IsNullOrEmpty(content))
                            {
                                var parts = content.Split(':', 2);
                                var name = parts[0].Trim();
                                var note = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                                exercises.Add(new KeyValuePair<string, string>(name, note));
                            }
                        }
                    }
                }

                var filename = Path.GetFileNameWithoutExtension(file);
                DateOnly date;
                // Try direct parse from filename (e.g. "2023-01-02")
                if (!DateOnly.TryParse(filename, out date))
                {
                    // Look for yyyy-MM-dd anywhere in the filename
                    var m = Regex.Match(filename, @"\d{4}-\d{2}-\d{2}");
                    if (m.Success && DateOnly.TryParse(m.Value, out var d2))
                    {
                        date = d2;
                    }
                    else if (DateTime.TryParse(filename, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        date = DateOnly.FromDateTime(dt);
                    }
                    else
                    {
                        throw new FormatException($"Could not parse date from filename '{filename}'");
                    }
                }

                // Skip if already exists
                if (await _db.DayRecords.AnyAsync(d => d.Date == date))
                {
                    report.SkippedCount++;
                    report.SkippedFiles.Add((file, "already exists"));
                    _logger.LogInformation("Skipping {File} - record for {Date} exists", file, date);

                    if (!dryRun)
                    {
                        try
                        {
                            var dest = Path.Combine(_backupRoot, Path.GetFileName(file));
                            File.Move(file, dest, overwrite: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to move existing file {File} to backup", file);
                        }
                    }

                    continue;
                }

                var entity = new DayRecordEntity
                {
                    Date = date,
                    WeightLb = parsedWeight ?? 0,
                    FoodIntakeNotes = string.Join(Environment.NewLine, foods),
                    ExerciseNotes = string.Join(Environment.NewLine, exercises.Select(kv => $"{kv.Key}: {kv.Value}"))
                };

                foreach (var f in foods) entity.FoodEntries.Add(new FoodEntry { Category = "Imported", Note = f });
                foreach (var ex in exercises) entity.ExerciseEntries.Add(new ExerciseEntry { Name = ex.Key, Note = ex.Value });

                if (!dryRun)
                {
                    _db.DayRecords.Add(entity);
                    await _db.SaveChangesAsync();
                    report.ImportedCount++;
                    report.ImportedFiles.Add(file);

                    try
                    {
                        var backupFolder = Path.Combine(_backupRoot, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
                        Directory.CreateDirectory(backupFolder);
                        var destFile = Path.Combine(backupFolder, Path.GetFileName(file));
                        File.Move(file, destFile, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to move imported file {File} to backup", file);
                    }
                }
                else
                {
                    report.ImportedFiles.Add(file);
                    report.ImportedCount++;
                    _logger.LogInformation("Dry-run: would import {File} as {Date}", file, date);
                }
            }
            catch (Exception ex)
            {
                var err = ex.ToString();
                report.Errors.Add((file, err));
                report.SkippedCount++;
                report.SkippedFiles.Add((file, $"error: {ex.Message}"));
                _logger.LogError(ex, "Error importing file {File}", file);
            }
        }

        // CSV report
        if (!string.IsNullOrEmpty(csvReportPath))
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("File,Status,Reason");
                foreach (var f in report.ImportedFiles) sb.AppendLine($"\"{f}\",Imported,");
                foreach (var s in report.SkippedFiles) sb.AppendLine($"\"{s.File}\",Skipped,\"{s.Reason.Replace("\"","\"\"")}\"");
                if (dryRun) sb.AppendLine($",,DryRun");
                File.WriteAllText(csvReportPath, sb.ToString());
                _logger.LogInformation("Wrote CSV report to {Csv}", csvReportPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write CSV report to {Csv}", csvReportPath);
            }
        }

        _logger.LogInformation("Import complete. Imported {Imported} Skipped {Skipped} Errors {Errors}", report.ImportedCount, report.SkippedCount, report.Errors.Count);
        return report;
    }

    // Back-compat overload matching original signature
    public async Task<int> ImportFromMarkdownAsync(string repoPath)
    {
        var r = await ImportFromMarkdownAsync(repoPath, dryRun: false, csvReportPath: null);
        return r.ImportedCount;
    }
}
