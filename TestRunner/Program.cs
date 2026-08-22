using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DietAndExercise.Data;
using DietAndExercise.Services;
using DietAndExercise.Models;

// Simple verification routine for EfDietAndExerciseService

using var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();

var options = new DbContextOptionsBuilder<DietAndExerciseDbContext>()
    .UseSqlite(connection)
    .Options;

using (var db = new DietAndExerciseDbContext(options))
{
    db.Database.EnsureCreated();

    var svc = new EfDietAndExerciseService(db);

    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    Console.WriteLine($"Creating record for {today}");
    var record = new DayRecord
    {
        Date = today,
        WeightLb = 180.5,
        CaloriesConsumed = 2200,
        CaloriesBurned = 600,
        FoodIntakeNotes = "Breakfast: eggs\nLunch: salad",
        ExerciseNotes = "Run: 30min"
    };

    svc.AddOrUpdateDayRecord(record);

    var fetched = svc.GetByDate(today);
    Console.WriteLine(fetched is null ? "Fetch failed" : $"Fetched: {fetched.Date} weight={fetched.WeightLb} consumed={fetched.CaloriesConsumed} burned={fetched.CaloriesBurned}");

    Console.WriteLine("Updating calories and notes");
    var updated = fetched with { CaloriesConsumed = 2100, FoodIntakeNotes = "Updated notes" };
    svc.AddOrUpdateDayRecord(updated);

    var afterUpdate = svc.GetByDate(today);
    Console.WriteLine(afterUpdate is null ? "Fetch after update failed" : $"After update: consumed={afterUpdate.CaloriesConsumed} notes={afterUpdate.FoodIntakeNotes}");

    var history = svc.GetHistory();
    Console.WriteLine($"History count: {history.Count}");

    Console.WriteLine("Deleting record");
    svc.DeleteDayRecord(today);

    var afterDelete = svc.GetByDate(today);
    Console.WriteLine(afterDelete is null ? "Delete succeeded" : "Delete failed");
}

connection.Close();
