using System.Collections.Concurrent;
using NJsonSchema;

public class Validation
{
  public static ConcurrentDictionary<Type, JsonSchema> _cache =
    new ConcurrentDictionary<Type, JsonSchema>();

  public static (T @object, IList<string> errors) Parse<T>(string json)
  {
    var schema = _cache.GetOrAdd(
      typeof(T),
      t =>
      {
        var s = JsonSchema.FromType<T>();
        s.AllowAdditionalProperties = false;
        return s;
      }
    );
    ICollection<NJsonSchema.Validation.ValidationError> errors;
    IList<string> errorMessages;
    T? obj;
    try
    {
      errors = schema.Validate(json);
      errorMessages = errors.Select(e => $"{e.Path}: {e.Kind}").ToList();
      obj = System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }
    catch (Exception)
    {
      return (default(T)!, new List<string> { "Error parsing JSON" });
    }
    if (errorMessages.Count == 0)
    {
      return (obj!, errorMessages);
    }
    else
    {
      return (default(T)!, errorMessages);
    }
  }
}
