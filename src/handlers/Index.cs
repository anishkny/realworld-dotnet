
public class RouteMapper
{
  public static void MapMethods(WebApplication app)
  {
    app.MapGet("/", () => Results.Ok());

    var apiGroup = app.MapGroup("/api");
    UserHandlers.MapMethods(apiGroup);
    ProfileHandlers.MapMethods(apiGroup);
    ArticleHandlers.MapMethods(apiGroup);
  }
}
