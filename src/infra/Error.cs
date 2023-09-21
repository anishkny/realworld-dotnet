public record ErrorDTO
{
  public object? Errors { get; set; }

  public ErrorDTO() { }

  public ErrorDTO(string errorArea, string errorMessage)
  {
    Errors = new Dictionary<string, string[]> { { errorArea, new[] { errorMessage } } };
  }
}
