using System.Text.Json.Serialization;

namespace UserManagementAPI.Models;

public sealed record ApiError(
    string Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Dictionary<string, string[]>? Errors = null);
