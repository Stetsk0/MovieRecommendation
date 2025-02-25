using System.Text.Json.Serialization;

namespace MovieRecommendation.Models
{
    public class MovieResult
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("original_title")]
        public string OriginalTitle { get; init; } = string.Empty;

        [JsonPropertyName("overview")]
        public string Overview { get; init; } = string.Empty;

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; init; } = string.Empty;

        [JsonPropertyName("popularity")]
        public double Popularity { get; init; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; init; }

        public string FullPosterUrl => string.IsNullOrEmpty(PosterPath)
            ? "/images/no-image.png"
            : $"https://image.tmdb.org/t/p/w500{PosterPath}";

        [JsonPropertyName("adult")]
        public bool Adult { get; init; }

        [JsonPropertyName("genres")]
        public List<Genre> Genres { get; init; } = new();

        [JsonPropertyName("keywords")]
        public List<Keyword> Keywords { get; init; } = new();
    }
}
