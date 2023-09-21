using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MiniValidation;

public class UserHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /users - Register a new user
    app.MapPost("/users", UserHandlers.registerUser);

    // POST /users/login - Login
    app.MapPost("/users/login", UserHandlers.loginUser);

    // GET /user - Get current user
    app.MapGet("/user", UserHandlers.getCurrentUser);

    // PUT /user - Update current user
    app.MapPut("/user", UserHandlers.updateCurrentUser);

  }

  public static async Task<IResult> registerUser(UserRegistrationDTOEnvelope userRegistrationDTOEnvelope, Db db)
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

  public static async Task<IResult> loginUser(UserLoginDTOEnvelope userLoginDTOEnvelope, Db db)
  {
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == userLoginDTOEnvelope.user.email);
    if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(userLoginDTOEnvelope.user.password, user.PasswordHash))
    {
      return Results.Unauthorized();
    }
    var token = Auth.generateToken(user);
    return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user, token));
  }

  public static IResult getCurrentUser(HttpContext httpContext)
  {
    var (user, token) = getUserAndToken(httpContext);
    return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user, token));
  }

  public static IResult updateCurrentUser(HttpContext httpContext, UserUpdateDTOEnvelope userUpdateDTOEnvelope, Db db)
  {
    // Validate the user update DTO
    if (!MiniValidator.TryValidate(userUpdateDTOEnvelope, out var errors))
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    // Ensure at least one field is being updated
    if (userUpdateDTOEnvelope.user.email == null
      && userUpdateDTOEnvelope.user.bio == null
      && userUpdateDTOEnvelope.user.image == null)
    {
      return Results.UnprocessableEntity(new ErrorDTO("user", "At least one field must be updated"));
    }

    // Update the user
    var (user, token) = getUserAndToken(httpContext);
    if (userUpdateDTOEnvelope.user.email != null) user.Email = userUpdateDTOEnvelope.user.email;
    if (userUpdateDTOEnvelope.user.bio != null) user.Bio = userUpdateDTOEnvelope.user.bio;
    if (userUpdateDTOEnvelope.user.image != null) user.Image = userUpdateDTOEnvelope.user.image;
    db.SaveChanges();

    return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user, token));
  }

  [ExcludeFromCodeCoverage]
  static (User, string) getUserAndToken(HttpContext httpContext)
  {
    var user = (User?)httpContext.Items["user"];
    var token = (string?)httpContext.Items["token"];
    if (user == null || token == null) throw new Exception("User and token not found in request context");
    return (user, token);
  }

}
