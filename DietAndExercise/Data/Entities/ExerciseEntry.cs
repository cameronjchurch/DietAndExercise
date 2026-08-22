namespace DietAndExercise.Data.Entities;

public class ExerciseEntry
{
    public int Id { get; set; }
    public int DayRecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public DayRecordEntity? DayRecord { get; set; }
}
