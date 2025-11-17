using System.ComponentModel.DataAnnotations;

public class Comment : BaseEntity
{
  public string Body { get; set; } = "";
  public Article Article { get; set; } = null!;
  public User Author { get; set; } = null!;
}

public class CommentCreationDTOEnvelope
{
  [Required]
  public CommentCreationDTO comment { get; set; } = new CommentCreationDTO();
}

public class CommentCreationDTO
{
  [Required]
  public string body { get; set; } = "";
}

public class CommentDTOEnvelope
{
  public CommentDTO comment { get; set; } = null!;

  public CommentDTOEnvelope(CommentDTO comment) => this.comment = comment;

  public static CommentDTOEnvelope fromComment(Db db, Comment comment, User viewer)
  {
    return new CommentDTOEnvelope(CommentDTO.fromComment(db, comment, viewer));
  }
}

public class CommentDTO
{
  public Guid id { get; set; }
  public string body { get; set; } = "";
  public DateTime createdAt { get; set; }
  public DateTime updatedAt { get; set; }
  public ProfileDTO author { get; set; } = null!;

  public static CommentDTO fromComment(Db db, Comment comment, User viewer) =>
    new CommentDTO
    {
      id = comment.Id,
      body = comment.Body,
      createdAt = comment.CreatedAt,
      updatedAt = comment.UpdatedAt,
      author = ProfileDTO.fromUserAsViewer(db, comment.Author, viewer),
    };
}

public class CommentsDTOEnvelope
{
  public List<CommentDTO> comments { get; set; } = [];

  public CommentsDTOEnvelope(List<CommentDTO> comments) => this.comments = comments;

  public static CommentsDTOEnvelope fromComments(Db db, List<Comment> comments, User viewer)
  {
    var commentDTOs = comments
      .Select(comment => CommentDTO.fromComment(db, comment, viewer))
      .ToList();
    return new CommentsDTOEnvelope(commentDTOs);
  }
}
