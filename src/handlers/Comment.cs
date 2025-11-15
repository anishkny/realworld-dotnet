using Microsoft.EntityFrameworkCore;

public class CommentHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // POST /articles/:slug/comments - Add a comment to an article
    app.MapPost("/articles/{slug}/comments", CommentHandlers.createComment);
  }

  public static async Task<IResult> createComment(HttpContext httpContext, Db db, string slug)
  {
    var (commentCreationDTOEnvelope, errors) = Validation.Parse<CommentCreationDTOEnvelope>(
      await new StreamReader(httpContext.Request.Body).ReadToEndAsync()
    );
    if (errors.Count > 0)
    {
      return Results.UnprocessableEntity(new ErrorDTO { Errors = errors });
    }

    var (user, _) = Auth.getUserAndToken(httpContext);

    // Get the article by slug
    var article = await Article.getBySlug(db, slug);
    if (article == null)
    {
      return Results.NotFound();
    }

    // Create the comment
    var comment = new Comment
    {
      Body = commentCreationDTOEnvelope!.comment.body,
      Author = user!,
      Article = article,
    };

    db.Comments.Add(comment);
    await db.SaveChangesAsync();

    return Results.Ok(CommentDTOEnvelope.fromComment(db, comment, user));
  }
}
