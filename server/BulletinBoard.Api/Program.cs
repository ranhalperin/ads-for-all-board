using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Repositories;
using BulletinBoard.Api.Services;
using BulletinBoard.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// Storage: one folder (local, or a shared network mount) holding ads.json, ads.version and ads.lock.
var storage = builder.Configuration.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions();
var contentRoot = builder.Environment.ContentRootPath;
var folder = StorageFolder.Resolve(storage.FolderPath, contentRoot);
var seedFile = storage.SeedFile is null ? null : Path.Combine(contentRoot, storage.SeedFile);

builder.Services.AddSingleton(storage);
builder.Services.AddSingleton(folder);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IFileStore>(_ => new SharedFolderFileStore(folder, seedFile));
builder.Services.AddSingleton<IDistributedLock>(_ => new SharedFileLock(folder, storage.LockRetryMinMs, storage.LockRetryMaxMs));
// Singleton on purpose: one in-memory snapshot and one local lock for the whole process.
builder.Services.AddSingleton<IAdRepository, JsonFileAdRepository>();
builder.Services.AddScoped<IAdService, AdService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => JsonDefaults.Configure(o.JsonSerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorHandler>();

// In development the React dev server proxies /api, so CORS only matters when the client is hosted elsewhere.
var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyMethod()
    .WithHeaders("Content-Type", "X-Owner-Id")
    .WithExposedHeaders("Location", "Retry-After")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

app.Logger.LogInformation("Ads folder: {Folder}", folder.Path);
app.Run();

/// <summary>Visible to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
