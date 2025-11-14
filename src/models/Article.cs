using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Slugify;

[ExcludeFromCodeCoverage]
public class Article : BaseEntity
{
  public string Slug { get; set; } = "";
  public string Title { get; set; } = "";
  public string Description { get; set; } = "";
  public string Body { get; set; } = "";
  public List<ArticleTag> Tags { get; set; } = [];
  public User Author { get; set; } = null!;

  public static Article fromCreationDTO(ArticleCreationDTO article, User user)
  {
    return new Article
    {
      Slug = new SlugHelper().GenerateSlug(
        article.title + "-" + Guid.NewGuid().ToString().Substring(0, 8)
      ),
      Title = article.title,
      Description = article.description,
      Body = article.body,
      Author = user,
      Tags = article.tagList.Select(tag => new ArticleTag { Name = tag }).ToList(),
    };
  }

  public static async Task<Article?> getBySlug(Db? db, string slug)
  {
    if (db == null)
      return null;
    return await db
      .Articles.Include(a => a.Author)
      .Include(a => a.Tags)
      .FirstOrDefaultAsync(a => a.Slug == slug);
  }

  public void updateFromDTO(ArticleUpdateDTO dto, Db db)
  {
    if (dto.title != null)
    {
      Title = dto.title;
      Slug = new SlugHelper().GenerateSlug(
        dto.title + "-" + Guid.NewGuid().ToString().Substring(0, 8)
      );
    }
    if (dto.description != null)
      Description = dto.description;
    if (dto.body != null)
      Body = dto.body;
    if (dto.tagList != null)
    {
      Tags.Clear();
      dto.tagList.ForEach(tagName =>
      {
        var articleTag = new ArticleTag
        {
          Name = tagName,
          Article = this,
          ArticleId = Id,
        };
        // Explicitly set the state to Added to avoid EF Core tracking issues
        db.Entry(articleTag).State = EntityState.Added;
      });
    }
  }
}

public class ArticleCreationDTOEnvelope
{
  [Required]
  public ArticleCreationDTO article { get; set; } = new ArticleCreationDTO();
}

public class ArticleCreationDTO
{
  [Required]
  public string title { get; set; } = "";

  [Required]
  public string description { get; set; } = "";

  [Required]
  public string body { get; set; } = "";
  public List<string> tagList { get; set; } = new List<string>();
}

public class ArticleUpdateDTOEnvelope
{
  [Required]
  public ArticleUpdateDTO article { get; set; } = new ArticleUpdateDTO();
}

public class ArticleUpdateDTO
{
  public string? title { get; set; }
  public string? description { get; set; }
  public string? body { get; set; }
  public List<string>? tagList { get; set; }
}

public class ArticleDTOEnvelope
{
  public ArticleDTO article { get; set; } = null!;

  public static ArticleDTOEnvelope fromArticle(Db db, Article article, User? viewer = null)
  {
    bool isFollowing = false;
    bool isFavorited = false;
    if (viewer != null)
    {
      isFollowing = Follow.isFollowing(db, viewer.Id, article.Author.Id);
      isFavorited = Favorite.isFavorited(db, viewer.Id, article.Id);
    }
    var favoritesCount = db.Favorites.Count(f => f.Article.Id == article.Id);

    return new ArticleDTOEnvelope
    {
      article = new ArticleDTO
      {
        slug = article.Slug,
        title = article.Title,
        description = article.Description,
        body = article.Body,
        tagList = article.Tags.Select(t => t.Name).ToList(),
        createdAt = article.CreatedAt.ToString("o"),
        updatedAt = article.UpdatedAt.ToString("o"),
        favorited = isFavorited,
        favoritesCount = favoritesCount,
        author = new ArticleAuthor
        {
          username = article.Author.Username,
          bio = article.Author.Bio,
          image = article.Author.Image,
          following = isFollowing,
        },
      },
    };
  }
}

public class ArticleDTO
{
  public string slug { get; set; } = "";
  public string title { get; set; } = "";
  public string description { get; set; } = "";
  public string body { get; set; } = "";
  public List<string> tagList { get; set; } = [];
  public string createdAt { get; set; } = "";
  public string updatedAt { get; set; } = "";
  public bool favorited { get; set; } = false;
  public int favoritesCount { get; set; } = 0;
  public ArticleAuthor author { get; set; } = null!;
}

public class ArticleAuthor
{
  public string username { get; set; } = "";
  public string bio { get; set; } = "";
  public string image { get; set; } = "";
  public bool following { get; set; } = false;
}
