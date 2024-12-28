using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using VideoGameAPI.Services;
using VideoGameAPI.VideoGameModel;

namespace VideoGameAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoGameController : ControllerBase
    {
        private readonly IDeltaCacheService _deltaCacheService;
        private const string CacheKey = "VideoGames";

        private static readonly List<VideoGame> videoGames =
        [
            new()
            {
                Id = 1,
                Title = "LOL",
                Platform = "Desktop",
                Developer = "Riot",
                Publisher = "Riot",
            },
            new()
            {
                Id = 2,
                Title = "Crossfire",
                Platform = "Desktop",
                Developer = "VTC",
                Publisher = "VTC",
            },
            new()
            {
                Id = 3,
                Title = "gumble ball",
                Platform = "Desktop",
                Developer = "Korea",
                Publisher = "Long",
            },
            new()
            {
                Id = 4,
                Title = "ZingSpeed",
                Platform = "Mobile",
                Developer = "VNG",
                Publisher = "Zing",
            },
            new()
            {
                Id = 5,
                Title = "BANG BANG",
                Platform = "Desktop",
                Developer = "VNG",
                Publisher = "Zing",
            },
        ];

        public VideoGameController(IDeltaCacheService deltaCacheService)
        {
            _deltaCacheService = deltaCacheService;
        }

        [HttpGet]
        [OutputCache(Duration = 300)]
        [ResponseCache(CacheProfileName = "Default")]
        public ActionResult<List<VideoGame>> GetVideoGames()
        {
            return _deltaCacheService.GetOrSetCache(CacheKey, videoGames, HttpContext);
        }
    }
}
