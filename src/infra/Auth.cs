using System.Diagnostics.CodeAnalysis;
using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;

public class Auth
{

  static public Task AuthenticateRequest(HttpContext context, Func<Task> next)
  {
    // Allowlist of methods and paths that are allowed without authentication
    string[] allowlistUnathenticated = {
      "HEAD /",
      "POST /api/users",
      "POST /api/users/login",
      "GET /api/profiles/",
    };
    Console.WriteLine($"{context.Request.Method} {context.Request.Path}");

    // If authorization header is present, check for authentication
    var authorizationHeaders = context.Request.Headers["Authorization"];
    var authorization = authorizationHeaders.Count > 0 ? authorizationHeaders[0] : null;
    User? user = null;
    if (authorization != null)
    {
      context.Items.Add("token", authorization);
      user = getUserFromAuthorizationHeader(authorization, context.RequestServices.GetService<Db>());
      if (user == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return Task.CompletedTask;
      }
      context.Items.Add("user", user);
    }

    // If the request is not in the allowlist, ensure that the user is authenticated
    if (!allowlistUnathenticated.Any(e => $"{context.Request.Method} {context.Request.Path}".StartsWith(e)))
    {
      if (user == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return Task.CompletedTask;
      }
    }

    return next();
  }

  static string getJwtSecretKey() => Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? "0e219856-b0d7-45bc-bb0f-b4aa24989943";

  static public string generateToken(User user) =>
    new JwtEncoder(new HMACSHA256Algorithm(), new JsonNetSerializer(), new JwtBase64UrlEncoder())
      .Encode(new Dictionary<string, object> { { "id", user.Id } }, getJwtSecretKey());

  static public uint verifyToken(string token)
  {
    var claims = new JwtDecoder(new JsonNetSerializer(), new JwtValidator(new JsonNetSerializer(), new UtcDateTimeProvider()), new JwtBase64UrlEncoder(), new HMACSHA256Algorithm())
      .DecodeToObject<Dictionary<string, object>>(token, getJwtSecretKey(), verify: true);
    return (uint)(long)claims["id"];
  }

  static public User? getUserFromAuthorizationHeader(string authorization, Db? db)
  {
    User? user;
    try
    {
      uint userId = verifyToken(authorization);
      user = User.getUserById(db, userId);
    }
    catch (Exception)
    {
      return null;
    }
    return user;
  }

  [ExcludeFromCodeCoverage]
  public static (User, string) getUserAndToken(HttpContext httpContext)
  {
    var user = (User?)httpContext.Items["user"];
    var token = (string?)httpContext.Items["token"];
    if (user == null || token == null) { throw new Exception("User and token not found in context"); }
    return (user, token);
  }

  // Check if authorization header is present
  public static bool hasAuthorizationHeader(HttpContext httpContext)
  {
    var authorizationHeaders = httpContext.Request.Headers["Authorization"];
    var authorization = authorizationHeaders.Count > 0 ? authorizationHeaders[0] : null;
    return authorization != null;
  }

}
