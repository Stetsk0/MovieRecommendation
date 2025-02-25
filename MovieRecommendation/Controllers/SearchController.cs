using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Interfaces;

namespace MovieRecommendation.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
