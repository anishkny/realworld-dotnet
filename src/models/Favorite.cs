public class Favorite : BaseEntity
{
  public User User { get; set; } = null!;
  public Article Article { get; set; } = null!;

  public static bool isFavorited(Db db, Guid userId, Guid articleId)
  {
    return db.Favorites.Any(f => f.User.Id == userId && f.Article.Id == articleId);
  }

  public static void favoriteArticle(Db db, User user, Article article)
  {
    if (isFavorited(db, user.Id, article.Id))
      return;
    db.Favorites.Add(new Favorite { User = user, Article = article });
    db.SaveChanges();
  }

  public static void unfavoriteArticle(Db db, User user, Article article)
  {
    var favorite = db.Favorites.FirstOrDefault(f =>
      f.User.Id == user.Id && f.Article.Id == article.Id
    );
    if (favorite == null)
      return;
    db.Favorites.Remove(favorite);
    db.SaveChanges();
  }
}
