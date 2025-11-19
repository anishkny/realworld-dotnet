public class RouteMapper
{
  public static void MapMethods(WebApplication app)
  {
    var apiGroup = app.MapGroup("/api");
    apiGroup.MapGet("/", () => Results.Ok());
    UserHandlers.MapMethods(apiGroup);
    ProfileHandlers.MapMethods(apiGroup);
    ArticleHandlers.MapMethods(apiGroup);
    FavoriteHandlers.MapMethods(apiGroup);
    CommentHandlers.MapMethods(apiGroup);
    TagHandlers.MapMethods(apiGroup);
  }
}
