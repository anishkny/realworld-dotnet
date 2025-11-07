using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class Follow : BaseEntity
{
  public User Follower { get; set; } = null!;
  public User Followed { get; set; } = null!;

  public static bool isFollowing(Db? db, Guid followerId, Guid followeeId)
  {
    return db?.Follows.Any(f => f.Follower.Id == followerId && f.Followed.Id == followeeId) ?? false;
  }

  public static void followUser(Db? db, User follower, User followed)
  {
    if (isFollowing(db, follower.Id, followed.Id)) return;
    db?.Follows.Add(new Follow { Follower = follower, Followed = followed, });
    db?.SaveChanges();
  }

  public static void unfollowUser(Db? db, User follower, User followed)
  {
    var follow = db?.Follows.FirstOrDefault(f => f.Follower.Id == follower.Id && f.Followed.Id == followed.Id);
    if (follow == null) return;
    db?.Follows.Remove(follow);
    db?.SaveChanges();
  }

}

[ExcludeFromCodeCoverage]
public record ProfileDTOEnvelope
{
  public ProfileDTO profile { get; set; } = null!;

  public ProfileDTOEnvelope(User user, bool following) => profile = new ProfileDTO
  {
    username = user.Username,
    bio = user.Bio ?? "",
    image = user.Image ?? "",
    following = following,
  };
}

[ExcludeFromCodeCoverage]
public record ProfileDTO
{
  public string username { get; set; } = null!;
  public string bio { get; set; } = null!;
  public string image { get; set; } = null!;
  public bool following { get; set; }
}
