using Microsoft.EntityFrameworkCore;
using DietAndExercise.Data;
using DietAndExercise.Models;
using DietAndExercise.Data.Entities;

namespace DietAndExercise.Services;

public class EfDietAndExerciseService : IDietAndExerciseService
{
    private readonly DietAndExerciseDbContext _db;

    public EfDietAndExerciseService(DietAndExerciseDbContext db)
    {
        _db = db;
    }

    public List<DayRecord> GetHistory()
    {
        var records = _db.DayRecords
            .Include(d => d.FoodEntries)
            .Include(d => d.ExerciseEntries)
                    .ToList()
                    .OrderBy(d => d.Date.ToDateTime(TimeOnly.MinValue))
                    .ToList();

        return records.Select(MapEntityToDomain).ToList();
    }

    public DayRecord? GetByDate(DateOnly date)
    {
        var d = _db.DayRecords
            .Include(x => x.FoodEntries)
            .Include(x => x.ExerciseEntries)
            .FirstOrDefault(x => x.Date == date);

        return d == null ? null : MapEntityToDomain(d);
    }

    public void AddOrUpdateDayRecord(DayRecord record)
    {
        var entity = _db.DayRecords.FirstOrDefault(d => d.Date == record.Date);
        if (entity == null)
        {
            entity = new DayRecordEntity
            {
                Date = record.Date,
                CaloriesConsumed = record.CaloriesConsumed,
                CaloriesBurned = record.CaloriesBurned,
                WeightLb = record.WeightLb,
                FoodIntakeNotes = record.FoodIntakeNotes ?? string.Empty,
                ExerciseNotes = record.ExerciseNotes ?? string.Empty
            };

            _db.DayRecords.Add(entity);
        }
        else
        {
            entity.CaloriesConsumed = record.CaloriesConsumed;
            entity.CaloriesBurned = record.CaloriesBurned;
            entity.WeightLb = record.WeightLb;
            entity.FoodIntakeNotes = record.FoodIntakeNotes ?? string.Empty;
            entity.ExerciseNotes = record.ExerciseNotes ?? string.Empty;

            _db.DayRecords.Update(entity);
        }

        _db.SaveChanges();
    }

    public void DeleteDayRecord(DateOnly date)
    {
        var entity = _db.DayRecords
            .Include(d => d.FoodEntries)
            .Include(d => d.ExerciseEntries)
            .FirstOrDefault(d => d.Date == date);

        if (entity == null)
            return;

        // Remove related entries first to be explicit about deletes
        if (entity.FoodEntries?.Any() == true)
            _db.FoodEntries.RemoveRange(entity.FoodEntries);
        if (entity.ExerciseEntries?.Any() == true)
            _db.ExerciseEntries.RemoveRange(entity.ExerciseEntries);

        _db.DayRecords.Remove(entity);
        _db.SaveChanges();
    }

    private static DayRecord MapEntityToDomain(DayRecordEntity d)
    {
        // Handle missing or default calorie fields gracefully by treating null/0 as 0
        int consumed = d.CaloriesConsumed;
        int burned = d.CaloriesBurned;

        var foodNotes = string.IsNullOrEmpty(d.FoodIntakeNotes) && d.FoodEntries != null && d.FoodEntries.Any()
            ? string.Join(Environment.NewLine, d.FoodEntries.Select(f => f.Note))
            : d.FoodIntakeNotes ?? string.Empty;

        var exerciseNotes = string.IsNullOrEmpty(d.ExerciseNotes) && d.ExerciseEntries != null && d.ExerciseEntries.Any()
            ? string.Join(Environment.NewLine, d.ExerciseEntries.Select(e => $"{e.Name}: {e.Note}"))
            : d.ExerciseNotes ?? string.Empty;

        return new DayRecord
        {
            Date = d.Date,
            WeightLb = d.WeightLb,
            CaloriesConsumed = consumed,
            CaloriesBurned = burned,
            FoodIntakeNotes = foodNotes,
            ExerciseNotes = exerciseNotes
        };
    }
}
