using Microsoft.EntityFrameworkCore;

public class ArticleHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles - Create a new article
    app.MapPost("/articles", ArticleHandlers.createArticle);

    // PUT /articles/{slug} - Update an article
    app.MapPut("/articles/{slug}", ArticleHandlers.updateArticle);

    // DELETE /articles/{slug} - Delete an article
    app.MapDelete("/articles/{slug}", ArticleHandlers.deleteArticle);

    // GET /articles/{slug} - Get an article
    app.MapGet("/articles/{slug}", ArticleHandlers.getArticle);
  }

  public static async Task<IResult> createArticle(HttpContext httpContext, Db db)
  {
    var (articleCreationDTOEnvelope, errors) = Validation.Parse<ArticleCreationDTOEnvelope>(
      await new StreamReader(httpContext.Request.Body).ReadToEndAsync()
    );
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

  public static async Task<IResult> updateArticle(HttpContext httpContext, Db db, string slug)
  {
    var (articleUpdateDTOEnvelope, errors) = Validation.Parse<ArticleUpdateDTOEnvelope>(
      await new StreamReader(httpContext.Request.Body).ReadToEndAsync()
    );
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }
    var (user, _) = Auth.getUserAndToken(httpContext);

    var article = await db
      .Articles.Include(a => a.Author)
      .Include(a => a.Tags)
      .FirstOrDefaultAsync(a => a.Slug == slug);
    if (article == null)
    {
      return Results.NotFound();
    }
    if (article.Author.Id != user!.Id)
    {
      return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    article.UpdateFromDTO(articleUpdateDTOEnvelope!.article, db);
    await db.SaveChangesAsync();
    return Results.Ok(ArticleDTOEnvelope.fromArticle(article));
  }

  public static async Task<IResult> deleteArticle(HttpContext httpContext, Db db, string slug)
  {
    var (user, _) = Auth.getUserAndToken(httpContext);
    var article = await db.Articles.Include(a => a.Author).FirstOrDefaultAsync(a => a.Slug == slug);
    if (article == null)
    {
      return Results.NotFound();
    }
    if (article.Author.Id != user!.Id)
    {
      return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    db.Articles.Remove(article);
    await db.SaveChangesAsync();
    return Results.Ok();
  }

  public static async Task<IResult> getArticle(HttpContext httpContext, Db db, string slug)
  {
    var article = await db
      .Articles.Include(a => a.Author)
      .Include(a => a.Tags)
      .FirstOrDefaultAsync(a => a.Slug == slug);
    if (article == null)
    {
      return Results.NotFound();
    }
    return Results.Ok(ArticleDTOEnvelope.fromArticle(article));
  }
}
