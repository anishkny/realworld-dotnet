using Microsoft.EntityFrameworkCore;

public class ArticleHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // GET /articles/feed - Get articles from followed users
    app.MapGet("/articles/feed", getFeed);

    // POST /articles - Create a new article
    app.MapPost("/articles", createArticle);

    // PUT /articles/{slug} - Update an article
    app.MapPut("/articles/{slug}", updateArticle);

    // DELETE /articles/{slug} - Delete an article
    app.MapDelete("/articles/{slug}", deleteArticle);

    // GET /articles/{slug} - Get an article
    app.MapGet("/articles/{slug}", getArticle);

    // GET /articles - List articles
    app.MapGet("/articles", listArticles);
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
    return Results.Ok(ArticleDTOEnvelope.fromArticle(db, article));
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
    article.updateFromDTO(articleUpdateDTOEnvelope!.article, db);
    await db.SaveChangesAsync();
    return Results.Ok(ArticleDTOEnvelope.fromArticle(db, article));
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
    var article = await Article.getBySlug(db, slug);
    if (article == null)
    {
      return Results.NotFound();
    }
    var (user, _) = Auth.getUserAndToken(httpContext);
    return Results.Ok(ArticleDTOEnvelope.fromArticle(db, article, user));
  }

  // Return most recent articles, optionally filtered by tag, author, or favorited by a user
  // Supports pagination via limit (default 20) and offset (default 0) query parameters
  public static IResult listArticles(HttpContext httpContext, Db db)
  {
    var (user, _) = Auth.getUserAndToken(httpContext);

    // Parse query parameters
    var query = httpContext.Request.Query;
    string? tag = query.ContainsKey("tag") ? query["tag"].ToString() : null;
    string? authorUsername = query.ContainsKey("author") ? query["author"].ToString() : null;
    string? favoritedByUsername = query.ContainsKey("favorited")
      ? query["favorited"].ToString()
      : null;
    int limit = int.TryParse(query["limit"], out var l) ? l : 20;
    int offset = int.TryParse(query["offset"], out var o) ? o : 0;

    // Construct the base query
    var articlesQuery = db.Articles.Include(a => a.Author).Include(a => a.Tags).AsQueryable();

    // Apply filters if present
    if (tag != null)
    {
      articlesQuery = articlesQuery.Where(a => a.Tags.Any(t => t.Name == tag));
    }
    else if (authorUsername != null)
    {
      articlesQuery = articlesQuery.Where(a => a.Author.Username == authorUsername);
    }
    else if (favoritedByUsername != null)
    {
      articlesQuery = articlesQuery.Where(a =>
        db.Favorites.Any(f => f.User.Username == favoritedByUsername && f.Article.Id == a.Id)
      );
    }

    // Apply sorting, pagination
    articlesQuery = articlesQuery.OrderByDescending(a => a.UpdatedAt).Skip(offset).Take(limit);

    // Execute the query
    var articles = articlesQuery.ToList();

    return Results.Ok(ArticlesDTOEnvelope.fromArticles(db, articles, user));
  }

  // Return most recent articles from users followed by the current user
  // Supports pagination via limit (default 20) and offset (default 0) query parameters
  public static IResult getFeed(HttpContext httpContext, Db db)
  {
    var (user, _) = Auth.getUserAndToken(httpContext);
    if (user == null)
    {
      return Results.StatusCode(StatusCodes.Status401Unauthorized);
    }

    // Parse query parameters
    var query = httpContext.Request.Query;
    int limit = int.TryParse(query["limit"], out var l) ? l : 20;
    int offset = int.TryParse(query["offset"], out var o) ? o : 0;

    // Get IDs of users followed by the current user
    var followedUserIds = db
      .Follows.Where(f => f.Follower.Id == user!.Id)
      .Select(f => f.Followed.Id)
      .ToList();

    // If no followed users, return empty list
    if (followedUserIds.Count == 0)
    {
      return Results.Ok(new ArticlesDTOEnvelope { articles = [], articlesCount = 0 });
    }

    // Get articles authored by followed users
    var articles = db
      .Articles.Include(a => a.Author)
      .Include(a => a.Tags)
      .Where(a => followedUserIds.Contains(a.Author.Id))
      .OrderByDescending(a => a.UpdatedAt)
      .Skip(offset)
      .Take(limit)
      .ToList();

    return Results.Ok(ArticlesDTOEnvelope.fromArticles(db, articles, user));
  }
}
