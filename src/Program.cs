using Microsoft.EntityFrameworkCore;
using MiniValidation;
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

var apiGroup = app.MapGroup("/api");

// POST /users - Register a new user
apiGroup.MapPost("/users", registerUser);

// POST /users/login - Login
apiGroup.MapPost("/users/login", loginUser);

// GET /user - Get current user
apiGroup.MapGet("/user", getCurrentUser);

app.Run();

static async Task<IResult> registerUser(UserRegistrationDTOEnvelope userRegistrationDTOEnvelope, Db db)
{
  if (!MiniValidator.TryValidate(userRegistrationDTOEnvelope, out var errors))
  {
    return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
  }

  var user = User.fromRegistrationDTO(userRegistrationDTOEnvelope.user);
  db.Users.Add(user);
  await db.SaveChangesAsync();
  var token = Auth.generateToken(user);
  return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user, token));
}

static async Task<IResult> loginUser(UserLoginDTOEnvelope userLoginDTOEnvelope, Db db)
{
  var user = await db.Users.SingleOrDefaultAsync(u => u.Email == userLoginDTOEnvelope.user.email);
  if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(userLoginDTOEnvelope.user.password, user.PasswordHash))
  {
    return Results.Unauthorized();
  }
  var token = Auth.generateToken(user);
  return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user, token));
}

static IResult getCurrentUser(HttpContext httpContext)
{
  return Results.Ok(httpContext.Items["user"]);
}
