using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Interfaces;
using MovieRecommendation.Models;
using System.Diagnostics;

namespace MovieRecommendation.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieService _movieService;

        public HomeController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(string movieTitle)
        {
            if (string.IsNullOrEmpty(movieTitle))
            {
                ModelState.AddModelError("", "Please enter a movie title.");
                return View("Index");
            }

            var movieId = await _movieService.GetMovieIdByTitleAsync(movieTitle);
            var movieData = await _movieService.GetMovieDetailsByIdAsync(movieId);

            ViewBag.MovieData = movieData;

            var similarMovies = await _movieService.FindSimilarMoviesByTitleAsync(movieTitle);

            if (similarMovies == null || !similarMovies.Any())
            {
                ViewBag.Message = "No similar movies found.";
                return View("Index");
            }
            
            return View("Views/Search/Index.cshtml", similarMovies);
        }
    }
}
