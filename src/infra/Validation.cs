using NJsonSchema;

public class Validation
{
  public static Dictionary<Type, JsonSchema> _cache = [];

  public static (T @object, IList<string> errors) Parse<T>(string json)
  {
    JsonSchema schema;
    if (_cache.ContainsKey(typeof(T)))
    {
      schema = _cache[typeof(T)];
    }
    else
    {
      schema = JsonSchema.FromType<T>();
      schema.AllowAdditionalProperties = false;
      _cache[typeof(T)] = schema;
    }

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
