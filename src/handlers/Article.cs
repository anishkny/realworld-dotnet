
public class ArticleHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles - Create a new article
    app.MapPost("/articles", ArticleHandlers.createArticle);
  }

  public static async Task<IResult> createArticle(HttpContext httpContext, Db db)
  {
    var (articleCreationDTOEnvelope, errors) = Validation.Parse<ArticleCreationDTOEnvelope>(await new StreamReader(httpContext.Request.Body).ReadToEndAsync());
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }
    var (user, _) = Auth.getUserAndToken(httpContext);

    var article = Article.fromCreationDTO(articleCreationDTOEnvelope!.article, user!);
    db.Articles.Add(article);
    await db.SaveChangesAsync();
    return Results.Ok(ArticleDTOEnvelope.fromArticle(article));
  }
}

