using System.Threading.Tasks;

public class FavoriteHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles/:slug/favorite - Favorite an article
    app.MapPost("/articles/{slug}/favorite", favoriteArticle);

    // DELETE /articles/:slug/favorite - Unfavorite an article
    app.MapDelete("/articles/{slug}/favorite", unfavoriteArticle);
  }

  public static async Task<IResult> favoriteArticle(HttpContext httpContext, string slug)
  {
    // Get the article from the database
    var article = await Article.getBySlug(httpContext.RequestServices.GetService<Db>(), slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Favorite the article
    Favorite.favoriteArticle(httpContext.RequestServices.GetService<Db>()!, currentUser!, article);

    // Return the article
    return Results.Ok(
      ArticleDTOEnvelope.fromArticle(
        httpContext.RequestServices.GetService<Db>()!,
        article,
        currentUser!
      )
    );
  }

  public static async Task<IResult> unfavoriteArticle(HttpContext httpContext, string slug)
  {
    // Get the article from the database
    var article = await Article.getBySlug(httpContext.RequestServices.GetService<Db>(), slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Unfavorite the article
    Favorite.unfavoriteArticle(
      httpContext.RequestServices.GetService<Db>()!,
      currentUser!,
      article
    );

    // Return the article
    return Results.Ok(
      ArticleDTOEnvelope.fromArticle(
        httpContext.RequestServices.GetService<Db>()!,
        article,
        currentUser!
      )
    );
  }
}
