using YandexCloudCourseTask.API.Models;
using YandexCloudCourseTask.API.Repositories;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var sc = builder.Services;

sc.AddControllers();
sc.AddSingleton<YdbRepository>();
sc.AddSingleton(new ReplicaInfo(configuration.GetValue<string>("BACKEND_VERSION", "1.0.0")!, Guid.NewGuid()));

var app = builder.Build();
var repository = app.Services.GetRequiredService<YdbRepository>();
await repository.Initialize();

if (builder.Configuration.GetValue<string>("RUN_MIGRATIONS") == "true")
{
    await repository.CreateSchema();
}

app.MapControllers();

var port = app.Configuration.GetValue<string>("PORT", "8080");
app.Run($"http://0.0.0.0:{port}");