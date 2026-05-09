namespace FloName.Api
{
    // RequestValidator.cs in FloName.Api
    internal static class RequestValidator
    {
        public static IResult? ValidateGenerateRequest(string lang, string format,
            FilenameGenerator generator)
        {
            if (string.IsNullOrWhiteSpace(lang))
                return Results.BadRequest(ApiResponse<string>.Fail("Language must not be empty."));

            if (!generator.SupportedLanguages().Contains(lang, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(ApiResponse<string>.Fail(
                    $"Language '{lang}' is not supported. Supported languages: {string.Join(", ", generator.SupportedLanguages())}"));

            if (string.IsNullOrWhiteSpace(format))
                return Results.BadRequest(ApiResponse<string>.Fail("Format must not be empty."));

            if (format.Length > 500)
                return Results.BadRequest(ApiResponse<string>.Fail("Format must not exceed 500 characters."));

            return null;
        }

        public static IResult? ValidateBatchRequest(string lang, string format, int count,
            FilenameGenerator generator)
        {
            var baseValidation = ValidateGenerateRequest(lang, format, generator);
            if (baseValidation != null) return baseValidation;

            if (count <= 0)
                return Results.BadRequest(ApiResponse<string>.Fail("Count must be greater than zero."));
            if (count > 10_000)
                return Results.BadRequest(ApiResponse<string>.Fail("Count must not exceed 10,000."));

            return null;
        }
    }
}
