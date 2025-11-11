using Microsoft.EntityFrameworkCore;

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

  public static async Task<IResult> registerUser(HttpContext httpContext, Db db)
  {
    var (userRegistrationDTOEnvelope, errors) = Validation.Parse<UserRegistrationDTOEnvelope>(await new StreamReader(httpContext.Request.Body).ReadToEndAsync());
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    var user = User.fromRegistrationDTO(userRegistrationDTOEnvelope!.user);
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
    var (user, token) = Auth.getUserAndToken(httpContext);
    return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user!, token!));
  }

  public static async Task<IResult> updateCurrentUser(HttpContext httpContext, Db db)
  {
    var (userUpdateDTOEnvelope, errors) = Validation.Parse<UserUpdateDTOEnvelope>(await new StreamReader(httpContext.Request.Body).ReadToEndAsync());
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    // Ensure at least one field is being updated
    if (userUpdateDTOEnvelope!.user.email == null
      && userUpdateDTOEnvelope.user.bio == null
      && userUpdateDTOEnvelope.user.image == null)
    {
      return Results.UnprocessableEntity(new ErrorDTO("user", "At least one field must be updated"));
    }

    // Update the user
    var (user, token) = Auth.getUserAndToken(httpContext);
    if (userUpdateDTOEnvelope.user.email != null) user!.Email = userUpdateDTOEnvelope.user.email;
    if (userUpdateDTOEnvelope.user.bio != null) user!.Bio = userUpdateDTOEnvelope.user.bio;
    if (userUpdateDTOEnvelope.user.image != null) user!.Image = userUpdateDTOEnvelope.user.image;
    db.SaveChanges();

    return Results.Ok(AuthenticatedUserDTOEnvelope.fromUser(user!, token!));
  }

}
