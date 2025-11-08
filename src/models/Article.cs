using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class Article : BaseEntity
{
  public string Slug { get; set; } = "";
  public string Title { get; set; } = "";
  public string Description { get; set; } = "";
  public string Body { get; set; } = "";

  public List<ArticleTag> Tags { get; set; } = new();
  public User Author { get; set; } = null!;
}
