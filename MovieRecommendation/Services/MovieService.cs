using System.Text.Json;
using System.Text.Json.Serialization;
using MovieRecommendation.Interfaces;
using MovieRecommendation.Models;

namespace MovieRecommendation.Services
{
    public class MovieService : IMovieService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public MovieService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<T?> FetchFromApiAsync<T>(string requestUri)
        {
            try
            {
                var response = await _httpClient.GetAsync(requestUri);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API request failed: {response.StatusCode}");
                    return default;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON deserialization error: {ex.Message}");
            }

            return default;
        }

        public async Task<int[]> GetGenreIdsByMovieIdAsync(int movieId)
        {
            if (movieId <= 0) return Array.Empty<int>();

            var movieDetails = await FetchFromApiAsync<MovieResult>($"movie/{movieId}?language=en-US");
            return movieDetails?.Genres?.Select(g => g.Id).ToArray() ?? Array.Empty<int>();
        }

        public async Task<int[]> GetKeywordIdsByMovieIdAsync(int movieId)
        {
            if (movieId <= 0) return Array.Empty<int>();

            var keywordsResponse = await FetchFromApiAsync<MovieResult>($"movie/{movieId}/keywords");
            return keywordsResponse?.Keywords?.Select(k => k.Id).ToArray() ?? Array.Empty<int>();
        }

        public async Task<int> GetMovieIdByTitleAsync(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return 0;

            var searchResponse = await FetchFromApiAsync<MovieResponse>($"search/movie?query={Uri.EscapeDataString(title)}&language=en-US&page=1");
            return searchResponse?.Results?.FirstOrDefault()?.Id ?? 0;
        }

        public async Task<MovieResult?> GetMovieDetailsByIdAsync(int movieId)
        {
            if (movieId <= 0) return null;
            return await FetchFromApiAsync<MovieResult>($"movie/{movieId}?language=en-US");
        }

        private async Task<List<MovieResult>> SearchSimilarMoviesByKeywordsAsync(int[] keywordIds, int[] genreIds)
        {
            if (keywordIds.Length == 0 || genreIds.Length == 0) return new();

            var keywordsParam = string.Join("%2F", keywordIds.Take(4));
            var genresParam = string.Join("%2F", genreIds);
            var requestUri = $"discover/movie?include_adult=true&include_video=false&language=en-US&page=1&sort_by=vote_count.desc&vote_average.gte=6&vote_average.lte=10&with_genres={genresParam}&with_keywords={keywordsParam}";

            var similarMoviesResponse = await FetchFromApiAsync<MovieResponse>(requestUri);
            return similarMoviesResponse?.Results ?? new();
        }

        public async Task<List<MovieResult>> FindSimilarMoviesByMovieIdAsync(int movieId)
        {
            if (movieId <= 0) return new();

            int[] genreIds = await GetGenreIdsByMovieIdAsync(movieId);
            if (genreIds.Length == 0) return new();

            int[] originalKeywordIds = await GetKeywordIdsByMovieIdAsync(movieId);
            if (originalKeywordIds.Length == 0) return new();

            var similarMovies = await SearchSimilarMoviesByKeywordsAsync(originalKeywordIds, genreIds);
            if (!similarMovies.Any()) return new();

            var moviesWithKeywordMatches = new List<(MovieResult Movie, int MatchCount)>();

            foreach (var movie in similarMovies)
            {
                var movieKeywordIds = await GetKeywordIdsByMovieIdAsync(movie.Id);
                int matchCount = originalKeywordIds.Intersect(movieKeywordIds).Count();
                moviesWithKeywordMatches.Add((movie, matchCount));
            }

            return moviesWithKeywordMatches
                .OrderByDescending(m => m.MatchCount)
                .Select(m => m.Movie)
                .ToList();
        }

        public async Task<List<MovieResult>?> FindSimilarMoviesByTitleAsync(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return new();

            int movieId = await GetMovieIdByTitleAsync(title);
            if (movieId == 0) return new();

            return await FindSimilarMoviesByMovieIdAsync(movieId);
        }
    }
}
