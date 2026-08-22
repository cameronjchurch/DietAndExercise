namespace DietAndExercise.Data.Entities;

public class FoodEntry
{
    public int Id { get; set; }
    public int DayRecordId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public DayRecordEntity? DayRecord { get; set; }
}
