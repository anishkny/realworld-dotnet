public class TagHandlers
{
  public static void MapMethods(IEndpointRouteBuilder app)
  {
    // GET /tags - Get all tags
    app.MapGet("/tags", getTags);
  }

  public static IResult getTags(Db db)
  {
    return Results.Ok(
      new TagsDTOEnvelope { tags = [.. db.ArticleTags.Select(at => at.Name).Distinct()] }
    );
  }
}
