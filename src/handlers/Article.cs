using Microsoft.EntityFrameworkCore;

public class ArticleHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles - Create a new article
    app.MapPost("/articles", ArticleHandlers.createArticle);

    // PUT /articles/:slug - Update an article
    app.MapPut("/articles/{slug}", ArticleHandlers.updateArticle);
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

  public static async Task<IResult> updateArticle(HttpContext httpContext, Db db, string slug)
  {
    var (articleUpdateDTOEnvelope, errors) = Validation.Parse<ArticleUpdateDTOEnvelope>(await new StreamReader(httpContext.Request.Body).ReadToEndAsync());
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    // Ensure at least one field is being updated
    if (articleUpdateDTOEnvelope!.article.title == null
      && articleUpdateDTOEnvelope.article.description == null
      && articleUpdateDTOEnvelope.article.body == null)
    {
      return Results.UnprocessableEntity(new ErrorDTO("article", "At least one field must be updated"));
    }

    // Get the current user
    var (user, _) = Auth.getUserAndToken(httpContext);

    // Find the article by slug with Author and Tags
    var article = await db.Articles
      .Include(a => a.Author)
      .Include(a => a.Tags)
      .FirstOrDefaultAsync(a => a.Slug == slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Check if the current user is the author
    if (article.Author.Id != user!.Id)
    {
      return Results.Unauthorized();
    }

    // Update the article fields
    if (articleUpdateDTOEnvelope.article.title != null)
    {
      article.Title = articleUpdateDTOEnvelope.article.title;
      article.Slug = new Slugify.SlugHelper().GenerateSlug(article.Title + "-" + Guid.NewGuid().ToString().Substring(0, 8));
    }
    if (articleUpdateDTOEnvelope.article.description != null)
    {
      article.Description = articleUpdateDTOEnvelope.article.description;
    }
    if (articleUpdateDTOEnvelope.article.body != null)
    {
      article.Body = articleUpdateDTOEnvelope.article.body;
    }

    await db.SaveChangesAsync();
    return Results.Ok(ArticleDTOEnvelope.fromArticle(article));
  }
}

