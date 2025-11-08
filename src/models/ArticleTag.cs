using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("ArticleTags")]
[Index(nameof(Name))]
public class ArticleTag : BaseEntity
{
  public string Name { get; set; } = "";
}
