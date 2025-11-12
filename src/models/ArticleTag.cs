using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

[Table("ArticleTags")]
[Index(nameof(Name))]
[ExcludeFromCodeCoverage]
public class ArticleTag : BaseEntity
{
  public Guid ArticleId { get; set; }
  public Article Article { get; set; } = null!;
  public string Name { get; set; } = "";
}
