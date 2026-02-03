using DietAndExercise.Models;

namespace DietAndExercise.Services;

public interface IDietAndExerciseService
{
    List<DayRecord> GetHistory();
}
