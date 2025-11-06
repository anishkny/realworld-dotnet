using Microsoft.EntityFrameworkCore;
using Serilog;

string POSTGRES_CONNECTION_STRING = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
  ?? "Host=localhost;Port=5432;Username=postgres;Password=password;";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Db>(options => options.UseNpgsql(POSTGRES_CONNECTION_STRING));
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();
app.Use(Auth.AuthenticateRequest);
app.MapMethods("/", new[] { "HEAD" }, () => "");
// app.MapGet("/", () => Results.Ok());

var apiGroup = app.MapGroup("/api");
UserHandlers.MapMethods(apiGroup);
ProfileHandlers.MapMethods(apiGroup);

app.Run();
