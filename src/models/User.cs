using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
[ExcludeFromCodeCoverage]
public class User
{
  public uint Id { get; set; }
  public string Email { get; set; } = null!;
  public string Username { get; set; } = null!;
  public string PasswordHash { get; set; } = null!;
  public string? Bio { get; set; }
  public string? Image { get; set; } = null!;

  public static User fromRegistrationDTO(UserRegistrationDTO userDTO) => new User
  {
    Email = userDTO.email.ToLower().Trim(),
    Username = userDTO.username.ToLower().Trim(),
    PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(userDTO.password),
  };

  public static User? getUserById(Db? db, uint userId)
  {
    return db?.Users.SingleOrDefault(u => u.Id == userId);
  }

  public static User? getUserByUsername(Db? db, string username)
  {
    return db?.Users.SingleOrDefault(u => u.Username == username);
  }

}

[ExcludeFromCodeCoverage]
public record UserRegistrationDTOEnvelope
{
  [Required]
  public UserRegistrationDTO user { get; set; } = null!;
}

[ExcludeFromCodeCoverage]
public record UserRegistrationDTO
{
  // Define inner DTO class for user registration
  [Required]
  public string email { get; set; } = null!;
  [Required]
  public string username { get; set; } = null!;
  [Required]
  public string password { get; set; } = null!;
}

public record AuthenticatedUserDTOEnvelope
{
  public AuthenticatedUserDTO user { get; set; } = null!;

  public static AuthenticatedUserDTOEnvelope fromUser(User user, string token) => new AuthenticatedUserDTOEnvelope
  {
    user = new AuthenticatedUserDTO
    {
      email = user.Email,
      token = token,
      username = user.Username,
      bio = user.Bio,
      image = user.Image,
    }
  };
}

public record AuthenticatedUserDTO
{
  public string email { get; set; } = null!;
  public string token { get; set; } = null!;
  public string username { get; set; } = null!;
  public string? bio { get; set; }
  public string? image { get; set; }
}

public record UserLoginDTOEnvelope
{
  public UserLoginDTO user { get; set; } = null!;
}

public record UserLoginDTO
{
  public string email { get; set; } = null!;
  public string password { get; set; } = null!;
}

public record UserUpdateDTOEnvelope
{
  [Required]
  public UserUpdateDTO user { get; set; } = null!;
}

public record UserUpdateDTO
{
  public string? email { get; set; }
  public string? bio { get; set; }
  public string? image { get; set; }
}

