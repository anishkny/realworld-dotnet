using Microsoft.EntityFrameworkCore;
using Serilog;

string POSTGRES_CONNECTION_STRING = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
  ?? "Host=localhost;Port=5432;Username=postgres;Password=password;";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Db>(options => options.UseNpgsql(POSTGRES_CONNECTION_STRING));
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Db>();
    db.Database.Migrate();
}

app.Use(Auth.AuthenticateRequest);
RouteMapper.MapMethods(app);

app.Run();
