using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Locatic.Entities;
using Locatic.Interfaces;
using Locatic.Models;

namespace Locatic.Controllers
{
    public class VoituresController : Controller
    {
        private readonly IVoitureService _voitureService;

        public VoituresController(IVoitureService voitureService)
        {
            _voitureService = voitureService;
        }

// GET: Voitures
        public async Task<IActionResult> Index()
        {
            var voitures = await _voitureService.GetAllVoituresAsync();
            return View(voitures);
        }


// GET: Voitures/Create
        public async Task<IActionResult> Create()
        {
            var modeles = await _voitureService.GetAllModelesWithMarquesAsync();
            
            var viewModel = new VoitureFormViewModel
            {
                Annee = DateTime.Now.Year, 
                Modeles = modeles.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.Marque.Nom} - {m.Nom}"
                }).ToList()
            };

            return View(viewModel);
        }

// POST: Voitures/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoitureFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var modeles = await _voitureService.GetAllModelesWithMarquesAsync();
                viewModel.Modeles = modeles.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.Marque.Nom} - {m.Nom}"
                }).ToList();

                return View(viewModel);
            }

            var voiture = new Voiture
            {
                Immatriculation = viewModel.Immatriculation.ToUpper(),
                Annee = viewModel.Annee,
                TarifJournalier = viewModel.TarifJournalier,
                NbrPlaces = viewModel.NbrPlaces,
                Carburant = viewModel.Carburant,
                ModeleId = viewModel.SelectedModeleId
            };

            await _voitureService.AddVoitureAsync(voiture);
            return RedirectToAction(nameof(Index));
        }
    }
}