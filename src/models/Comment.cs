using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class Comment : BaseEntity
{
  public string Body { get; set; } = "";
  public User Author { get; set; } = null!;
  public Article Article { get; set; } = null!;
}

[ExcludeFromCodeCoverage]
public record CommentCreationDTOEnvelope
{
  [Required]
  public CommentCreationDTO comment { get; set; } = new CommentCreationDTO();
}

[ExcludeFromCodeCoverage]
public record CommentCreationDTO
{
  [Required]
  public string body { get; set; } = "";
}

[ExcludeFromCodeCoverage]
public record CommentDTOEnvelope
{
  public CommentDTO comment { get; set; } = null!;

  public static CommentDTOEnvelope fromComment(Db db, Comment comment, User? viewer = null)
  {
    return new CommentDTOEnvelope
    {
      comment = new CommentDTO
      {
        id = comment.Id,
        createdAt = comment.CreatedAt.ToString("o"),
        updatedAt = comment.UpdatedAt.ToString("o"),
        body = comment.Body,
        author = ProfileDTO.fromUserAsViewer(db, comment.Author, viewer),
      },
    };
  }
}

[ExcludeFromCodeCoverage]
public record CommentDTO
{
  public Guid id { get; set; }
  public string createdAt { get; set; } = "";
  public string updatedAt { get; set; } = "";
  public string body { get; set; } = "";
  public ProfileDTO author { get; set; } = null!;
}
