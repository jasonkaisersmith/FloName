namespace FloName.Api
{
    using System.Text.Json.Serialization;

    public record ApiResponse<T>(
        bool Success,
        T? Data,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? Error)
    {
        public static ApiResponse<T> Ok(T data) => new(true, data, null);
        public static ApiResponse<T> Fail(string error) => new(false, default, error);
    }
}
