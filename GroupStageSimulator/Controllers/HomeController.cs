using GroupStageSimulator.Services;
using GroupStageSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GroupStageSimulator.Controllers
{
    public class HomeController : Controller
    {
        private readonly SimulationService _simulationService;

        public HomeController(SimulationService simulationService)
        {
            _simulationService = simulationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Simulate()
        {
            var matches = await _simulationService.SimulateGroupStageAsync();
            var simulationId = matches.First().SimulationId;
            var standings = await _simulationService.GetStandingsAsync(simulationId);

            var viewModel = new SimulationViewModel
            {
                MatchesByRound = matches.GroupBy(m => m.Round)
                                        .ToDictionary(g => g.Key, g => g.ToList()),
                Standings = standings
            };

            return View("Index", viewModel);
        }
    }
}
