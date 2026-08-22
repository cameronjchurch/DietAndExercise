using DietAndExercise.Models;

namespace DietAndExercise.Services;

public interface IDietAndExerciseService
{
    List<DayRecord> GetHistory();

    DayRecord? GetByDate(DateOnly date);

    void AddOrUpdateDayRecord(DayRecord record);

    void DeleteDayRecord(DateOnly date);
}
