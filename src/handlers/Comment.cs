using Microsoft.EntityFrameworkCore;

public class CommentHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles/:slug/comments - Add a comment to an article
    app.MapPost("/articles/{slug}/comments", addComment);

    // DELETE /articles/:slug/comments/:id - Delete a comment from an article
    app.MapDelete("/articles/{slug}/comments/{id:guid}", deleteComment);
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

  public static async Task<IResult> deleteComment(
    HttpContext httpContext,
    Db db,
    string slug,
    Guid id
  )
  {
    // Get the comment from the database including its author and article
    var comment = await db
      .Comments.Include(c => c.Author)
      .Include(c => c.Article)
      .FirstOrDefaultAsync(c => c.Id == id && c.Article.Slug == slug);
    if (comment == null)
    {
      return Results.NotFound();
    }

    // Get the current user
    var (currentUser, _) = Auth.getUserAndToken(httpContext);

    // Check if the current user is the author of the comment
    if (comment.Author.Id != currentUser!.Id)
    {
      return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    // Delete the comment
    db.Comments.Remove(comment);
    await db.SaveChangesAsync();

    return Results.Ok();
  }
}
