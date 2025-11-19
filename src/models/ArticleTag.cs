using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

[Table("ArticleTags")]
[Index(nameof(Name))]
[ExcludeFromCodeCoverage]
public class ArticleTag : BaseEntity
{
  public Guid ArticleId { get; set; }
  public Article Article { get; set; } = null!;
  public string Name { get; set; } = "";
}

public class TagsDTOEnvelope
{
  public List<string> tags { get; set; } = [];
}
