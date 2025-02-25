using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Models;

namespace Business.Services
{
    public class MovieService
    {
        private const string ApiUrl = "https://api.themoviedb.org/3/search/movie";
        private const string ApiKey = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiI0OGQ5ZTEwNTg0ZDNjYzllOTBmZDNiYWZhZmEzMzA5MCIsIm5iZiI6MTczNDI2NTE2My40MjMsInN1YiI6IjY3NWVjOTRiZjk4ODgzNjE2MGIwMmEyOSIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.t3_BrLQlZ9CCNha7XrtOpYWMPWZF_Hae2tZYqk32paY";

        private readonly HttpClient _httpClient;

        public MovieService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Authorization", ApiKey);
        }

        public async Task<MovieResponse> SearchMovieAsync(string query)
        {
            var requestUri = $"{ApiUrl}?query={Uri.EscapeDataString(query)}&include_adult=false&language=en-US&page=1";
            var response = await _httpClient.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MovieResponse>(responseBody);
        }

        private async Task<List<MovieResult>> SearchSimilarMoviesByKeywordsAsync(int[] keywordIds, int[] genreIds)
        {
            var topKeywords = keywordIds.Take(4);
            var keywordsParam = string.Join("%2F", topKeywords); // %2C - URL-код для запятой
            var genresParam = string.Join("%2F", genreIds);
            var allMovies = new List<MovieResult>();
            int currentPage = 1;
            int totalPages;

            do
            {
                var requestUri = $"https://api.themoviedb.org/3/discover/movie?include_adult=true&include_video=false&language=en-US&page={currentPage}&sort_by=vote_count.desc&vote_average.gte=6&vote_average.lte=10&with_genres={genresParam}&with_keywords={keywordsParam}";
                var response = await _httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var similarMoviesResponse = JsonSerializer.Deserialize<SimilarMoviesResponse>(responseBody);

                if (similarMoviesResponse?.Results != null)
                {
                    allMovies.AddRange(similarMoviesResponse.Results);
                }

                totalPages = similarMoviesResponse?.TotalPages ?? 0;
                currentPage++;
            }
            while (currentPage <= 5/*totalPages*/);

            return allMovies;
        }
        public async Task<int[]> GetGenreIdsByMovieIdAsync(int movieId)
        {
            var requestUri = $"https://api.themoviedb.org/3/movie/{movieId}?language=en-US";
            var response = await _httpClient.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var movieDetailsResponse = JsonSerializer.Deserialize<MovieDetailsResponse>(responseBody);

            // Возвращаем массив ID жанров или пустой массив, если жанров нет
            return movieDetailsResponse?.Genres?.Select(g => g.Id).ToArray() ?? Array.Empty<int>();
        }
        public async Task<int[]> GetKeywordIdsByMovieIdAsync(int id)
        {

            var requestUri = $"https://api.themoviedb.org/3/movie/{id}/keywords";
            var response = await _httpClient.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            //return JsonSerializer.Deserialize<MovieKeywordsResponse>(responseBody);
            var movieKeywordResponse = JsonSerializer.Deserialize<MovieKeywordsResponse>(responseBody);

            // Возвращаем массив ID ключевых слов или пустой массив
            return movieKeywordResponse?.Keywords?.Select(k => k.Id).ToArray() ?? Array.Empty<int>();
        }

        public async Task<List<MovieResult>> FindSimilarMoviesByMovieIdAsync(int movieId)
        {
            // Шаг 1: Получаем жанры фильма
            int[] genreIds = await GetGenreIdsByMovieIdAsync(movieId);

            if (genreIds.Length == 0)
            {
                Console.WriteLine("Жанры не найдены для указанного фильма.");
                return new List<MovieResult>();
            }

            // Шаг 2: Получаем ключевые слова фильма
            var originalKeywordIds = await GetKeywordIdsByMovieIdAsync(movieId);

            if (originalKeywordIds.Length == 0)
            {
                Console.WriteLine("Ключевые слова отсутствуют для указанного фильма.");
                return new List<MovieResult>();
            }

            // Шаг 3: Поиск похожих фильмов
            var similarMovies = await SearchSimilarMoviesByKeywordsAsync(originalKeywordIds, genreIds);

            if (similarMovies.Count == 0)
            {
                Console.WriteLine("Похожие фильмы не найдены.");
                return new List<MovieResult>();
            }

            // Шаг 4: Сравнение и сортировка фильмов по количеству совпадений ключевых слов
            var moviesWithKeywordMatches = new List<(MovieResult Movie, int MatchCount)>();

            foreach (var movie in similarMovies)
            {
                var movieKeywordIds = await GetKeywordIdsByMovieIdAsync(movie.Id);
                // Подсчёт пересечения ключевых слов
                int matchCount = originalKeywordIds.Intersect(movieKeywordIds).Count();
                moviesWithKeywordMatches.Add((movie, matchCount));
            }

            // Сортировка по количеству совпадений ключевых слов
            var sortedMovies = moviesWithKeywordMatches
                .OrderByDescending(m => m.MatchCount)
                .Select(m => m.Movie)
                .ToList();

            return sortedMovies;
        }
        private async Task<int> GetMovieIdByTitleAsync(string title)
        {
            var requestUri = $"{ApiUrl}?query={Uri.EscapeDataString(title)}&language=en-US&page=1";
            var response = await _httpClient.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<MovieResponse>(responseBody);

            if (searchResponse?.Results != null && searchResponse.Results.Any())
            {
                // Возвращаем ID первого фильма из результатов поиска
                return searchResponse.Results.First().Id;
            }

            return 0; // Возвращаем 0, если фильм не найден
        }

        public async Task<List<MovieResult>?> FindSimilarMoviesByTitleAsync(string title)
        {
            // Шаг 1: Получаем ID фильма по названию
            int movieId = await GetMovieIdByTitleAsync(title);

            if (movieId == 0)
            {
                Console.WriteLine("Фильм не найден.");
                return new List<MovieResult>();
            }

            Console.WriteLine($"ID найденного фильма: {movieId}");

            // Шаг 2: Используем ID для получения ключевых слов
            int[] keywordIds = await GetKeywordIdsByMovieIdAsync(movieId);

            if (keywordIds.Length == 0)
            {
                Console.WriteLine("Ключевые слова не найдены для указанного фильма.");
                return new List<MovieResult>();
            }

            // Шаг 3: Используем ключевые слова для поиска похожих фильмов
            var similarMoviesResponse = await FindSimilarMoviesByMovieIdAsync(movieId);

            if (similarMoviesResponse?.Any() == true)
            {
                Console.WriteLine("Похожие фильмы найдены:");
                foreach (var movie in similarMoviesResponse)
                {
                    Console.WriteLine($"ID: {movie.Id}, Название: {movie.Title}");
                }
            }
            else
            {
                Console.WriteLine("Похожие фильмы не найдены.");
            }

            return similarMoviesResponse;
        }
    }
