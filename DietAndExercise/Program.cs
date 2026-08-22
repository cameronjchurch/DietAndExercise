using System;
using System.IO;
using Bit.BlazorUI;
using Microsoft.EntityFrameworkCore;
using DietAndExercise.Components;
using DietAndExercise.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure EF Core DbContext (connection string via ConnectionStrings:DietAndExercise or DIET_DB_CONN env)
var _dietConn = builder.Configuration.GetConnectionString("DietAndExercise")
    ?? Environment.GetEnvironmentVariable("DIET_DB_CONN") ?? string.Empty;
var _hasDb = !string.IsNullOrEmpty(_dietConn);

if (_hasDb)
{
    builder.Services.AddDbContext<DietAndExercise.Data.DietAndExerciseDbContext>(options =>
    {
        options.UseNpgsql(_dietConn);
    });
}
else
{
    // Do NOT register a DbContext when no provider/connection is configured to avoid runtime resolution errors.
    // This keeps the DI container clean in environments where the database is optional (e.g. static markdown mode).
}

// Toggle which implementation to use via UseDatabase config key (bool). Default: false (keep markdown service).
var _useDb = builder.Configuration.GetValue<bool?>("UseDatabase") ?? false;
if (_useDb && _hasDb)
{
    // Only register EF-backed service if database is available.
    builder.Services.AddScoped<DietAndExercise.Services.IDietAndExerciseService, DietAndExercise.Services.EfDietAndExerciseService>();
}
else
{
    // Fall back to the markdown-backed service when either UseDatabase is false or no DB connection is configured.
    builder.Services.AddSingleton<DietAndExercise.Services.IDietAndExerciseService, DietAndExercise.Services.DietAndExerciseService>();
    if (_useDb && !_hasDb)
    {
        // If UseDatabase was requested but no connection provided, surface a debug entry so developers notice in logs at startup.
        // Logging is available after Build(); we add a simple hosted startup check below to log this condition.
        builder.Services.AddSingleton(new Action<IServiceProvider>(sp =>
        {
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("Startup");
            logger?.LogWarning("UseDatabase=true but no DietAndExercise connection string found; falling back to markdown service.");
        }));
    }
}

// Data importer (optional) - backup path can be configured via ImportBackupPath
// Only register DataImporter when a DbContext/provider is configured. DataImporter depends on the DbContext and must share the scoped lifetime.
if (_hasDb)
{
    builder.Services.AddScoped<DietAndExercise.Data.DataImporter>(sp => new DietAndExercise.Data.DataImporter(
        sp.GetRequiredService<DietAndExercise.Data.DietAndExerciseDbContext>(),
        builder.Configuration["ImportBackupPath"] ?? Path.Combine(Environment.CurrentDirectory, "md-backup"),
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DietAndExercise.Data.DataImporter>>()
    ));
}

builder.Services.AddBitBlazorUIServices();
builder.Services.AddBitBlazorUIExtrasServices();

var app = builder.Build();

// If we registered the startup warning action, run it now so the log is emitted early.
var startupChecker = app.Services.GetService<Action<IServiceProvider>>();
if (startupChecker is not null)
{
    try { startupChecker(app.Services); } catch { }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
