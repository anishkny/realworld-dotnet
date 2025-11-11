using NJsonSchema;

public class Validation
{
  public static Dictionary<Type, JsonSchema> _cache = [];

  static public (T @object, IList<string> errors) Parse<T>(string json)
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
    var errors = schema.Validate(json);
    var errorMessages = errors.Select(e => $"{e.Path}: {e.Kind}").ToList();
    var obj = System.Text.Json.JsonSerializer.Deserialize<T>(json);
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
