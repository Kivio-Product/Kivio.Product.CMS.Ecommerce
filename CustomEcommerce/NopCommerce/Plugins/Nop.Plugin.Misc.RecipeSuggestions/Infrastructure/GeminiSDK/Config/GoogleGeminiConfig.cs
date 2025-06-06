namespace DotnetGeminiSDK.Config
{
    /// <summary>
    /// Configuration for the Gemini client
    ///
    /// If you don't have an API key, you can get one from the Google AI Studio.
    /// </summary>
    public class GoogleGeminiConfig
    {
        public string ApiKey { get; set; }
        public string TextBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20";
        public string ImageBaseUrl { get; set; } =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20";
        public string GenerateImageBaseURL { get; set; } =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-preview-image-generation";
        public string ModelBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
        public string EmbeddingBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    }
}