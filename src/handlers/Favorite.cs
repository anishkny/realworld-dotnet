public class FavoriteHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles/:slug/favorite - Favorite an article
    app.MapPost("/articles/{slug}/favorite", favoriteArticle);

    // DELETE /articles/:slug/favorite - Unfavorite an article
    app.MapDelete("/articles/{slug}/favorite", unfavoriteArticle);
  }

  public static async Task<IResult> favoriteArticle(HttpContext httpContext, Db db, string slug)
  {
    // Get the article from the database
    var article = await Article.getBySlug(db, slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Favorite the article
    Favorite.favoriteArticle(db, currentUser!, article);

    // Return the article
    return Results.Ok(ArticleDTOEnvelope.fromArticle(db, article, currentUser!));
  }

  public static async Task<IResult> unfavoriteArticle(HttpContext httpContext, Db db, string slug)
  {
    // Get the article from the database
    var article = await Article.getBySlug(db, slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Unfavorite the article
    Favorite.unfavoriteArticle(db, currentUser!, article);

    // Return the article
    return Results.Ok(ArticleDTOEnvelope.fromArticle(db, article, currentUser!));
  }
}
