using System.Globalization;
using DietAndExercise.Models;

namespace DietAndExercise.Services;

public class DietAndExerciseService(IConfiguration configuration) : IDietAndExerciseService
{
    private readonly string? _dataPath = configuration["DietAndExerciseRepo"];
    public List<DayRecord> GetHistory()
    {
        if (_dataPath == null)
            throw new InvalidOperationException("Data file path is not configured.");

        List<DayRecord> records = [];
        string[] files = Directory.GetFiles(_dataPath, "*.md", SearchOption.AllDirectories);

        if (files.Length == 0)
            return records;

        foreach (string filePath in files)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string currentSection = string.Empty;

                double? parsedWeight = null;
                List<string> foods = [];
                List<KeyValuePair<string, string>> exercises = [];

                foreach (var raw in lines)
                {
                    string line = raw?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // Section headers like "##### Weight"
                    if (line.StartsWith("#####", StringComparison.Ordinal))
                    {
                        string heading = line[5..].Trim();
                        currentSection = heading.ToLowerInvariant();
                        continue;
                    }

                    // Parse line based on current section
                    if (currentSection == "weight")
                    {
                        // Expect a number like "227.2"
                        // Try to extract first token that looks like a number
                        var token = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (!string.IsNullOrEmpty(token) && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                        {
                            parsedWeight = w;
                        }
                        else
                        {
                            // Try parse the whole line with invariant culture
                            if (double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out var w2))
                                parsedWeight = w2;
                        }
                    }
                    else if (currentSection == "food")
                    {
                        // Expect lines like "- Breakfast: eggs and a bagel"
                        if (line.StartsWith('-'))
                        {
                            string content = line.TrimStart('-', ' ').Trim();
                            if (!string.IsNullOrEmpty(content))
                                foods.Add(content);
                        }
                    }
                    else if (currentSection == "exercise")
                    {
                        // Expect lines like "- Squats: " or "- Window pushups: "
                        if (line.StartsWith('-'))
                        {
                            string content = line.TrimStart('-', ' ').Trim();
                            if (!string.IsNullOrEmpty(content))
                            {
                                var parts = content.Split([':'], 2);
                                string name = parts[0].Trim();
                                string note = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                                exercises.Add(new KeyValuePair<string, string>(name, note));
                            }
                        }
                    }
                }

                records.Add(new DayRecord
                {
                    Date = DateOnly.Parse(Path.GetFileNameWithoutExtension(filePath)),
                    WeightLb = parsedWeight ?? 0,
                    FoodIntakeNotes = string.Join(Environment.NewLine, foods),
                    ExerciseNotes = string.Join(Environment.NewLine, exercises.Select(kv => $"{kv.Key}: {kv.Value}".Trim()))
                });
            }
            catch
            {
                // Skip problematic files but continue processing others
                continue;
            }
        }

        return records;
    }
}
