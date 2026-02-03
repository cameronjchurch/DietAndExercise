namespace DietAndExercise.Models;

public record DayRecord
{
    public DateOnly Date { get; init; }
    public int CaloriesConsumed { get; init; }
    public int CaloriesBurned { get; init; }
    public double WeightLb { get; init; }
    public string FoodIntakeNotes { get; init; } = string.Empty;
    public string ExerciseNotes { get; init; } = string.Empty;

}
