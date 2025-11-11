using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

[Table("ArticleTags")]
[Index(nameof(Name))]
[ExcludeFromCodeCoverage]
public class ArticleTag : BaseEntity
{
  public string Name { get; set; } = "";
}
