using MiniValidation;

public class ArticleHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles - Create a new article
    app.MapPost("/articles", ArticleHandlers.createArticle);
  }

  public static async Task<IResult> createArticle(ArticleCreationDTOEnvelope articleCreationDTOEnvelope, HttpContext httpContext, Db db)
  {
    var (user, _) = Auth.getUserAndToken(httpContext);

    if (!MiniValidator.TryValidate(articleCreationDTOEnvelope, out var errors))
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    var article = Article.fromCreationDTO(articleCreationDTOEnvelope.article, user!);
    db.Articles.Add(article);
    await db.SaveChangesAsync();
    return Results.Ok(ArticleDTOEnvelope.fromArticle(article));
  }
}

