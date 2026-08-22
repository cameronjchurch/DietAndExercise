using System.Collections.Generic;

namespace DietAndExercise.Data.Entities;

public class DayRecordEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int CaloriesConsumed { get; set; }
    public int CaloriesBurned { get; set; }
    public double WeightLb { get; set; }
    public string FoodIntakeNotes { get; set; } = string.Empty;
    public string ExerciseNotes { get; set; } = string.Empty;

    public List<FoodEntry> FoodEntries { get; set; } = new();
    public List<ExerciseEntry> ExerciseEntries { get; set; } = new();
}
