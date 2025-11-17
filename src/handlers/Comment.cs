public class CommentHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles/:slug/comments - Add a comment to an article
    app.MapPost("/articles/{slug}/comments", addComment);
  }

  public static async Task<IResult> addComment(HttpContext httpContext, Db db, string slug)
  {
    // Validate the request body
    var (commentEnvelope, errors) = Validation.Parse<CommentCreationDTOEnvelope>(
      await new StreamReader(httpContext.Request.Body).ReadToEndAsync()
    );
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    // Get the article from the database
    var article = await Article.getBySlug(db, slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Create the comment
    var comment = new Comment
    {
      Body = commentEnvelope.comment.body,
      Article = article,
      Author = currentUser!,
    };
    db.Comments.Add(comment);
    await db.SaveChangesAsync();

    // Return the comment
    return Results.Ok(CommentDTOEnvelope.fromComment(db, comment, currentUser!));
  }
}
