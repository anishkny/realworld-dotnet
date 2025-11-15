public class ProfileHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // GET /profiles/:username - Get a profile
    app.MapGet("/profiles/{username}", getProfile);

    // POST /profiles/:username/follow - Follow a user
    app.MapPost("/profiles/{username}/follow", followUser);

    // DELETE /profiles/:username/follow - Unfollow a user
    app.MapDelete("/profiles/{username}/follow", unfollowUser);
  }

  public static IResult getProfile(HttpContext httpContext, string username)
  {
    var db = httpContext.RequestServices.GetService<Db>();
    var user = User.getByUsername(db, username);
    if (user == null)
    {
      return Results.NotFound();
    }
    var (currentUser, _) = Auth.getUserAndToken(httpContext);
    return Results.Ok(new ProfileDTOEnvelope(ProfileDTO.fromUser(db!, user, currentUser!)));
  }

  public static IResult followUser(HttpContext httpContext, string username)
  {
    // Get the user from the database
    var user = User.getByUsername(httpContext.RequestServices.GetService<Db>(), username);
    if (user == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Follow the user
    Follow.followUser(httpContext.RequestServices.GetService<Db>(), currentUser!, user);

    // Return the profile
    return Results.Ok(new ProfileDTOEnvelope(ProfileDTO.fromUser(user, true)));
  }

  public static IResult unfollowUser(HttpContext httpContext, string username)
  {
    // Get the user from the database
    var user = User.getByUsername(httpContext.RequestServices.GetService<Db>(), username);
    if (user == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Unfollow the user
    Follow.unfollowUser(httpContext.RequestServices.GetService<Db>(), currentUser!, user);

    // Return the profile
    return Results.Ok(new ProfileDTOEnvelope(ProfileDTO.fromUser(user, false)));
  }
}
