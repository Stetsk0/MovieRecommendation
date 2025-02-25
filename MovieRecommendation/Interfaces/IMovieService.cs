using MovieRecommendation.Models;

namespace MovieRecommendation.Interfaces
{
    public interface IMovieService
    {
        Task<List<MovieResult>?> FindSimilarMoviesByTitleAsync(string title);
        Task<int> GetMovieIdByTitleAsync(string title);
        Task<MovieResult?> GetMovieDetailsByIdAsync(int movieId);
    }
}
