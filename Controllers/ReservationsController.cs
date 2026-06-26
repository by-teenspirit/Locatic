using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Locatic.Entities;
using Locatic.Interfaces;
using Locatic.Models;

namespace Locatic.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly IVoitureService _voitureService;
        private readonly IClientService _clientService;

        public ReservationsController(
            IReservationService reservationService,
            IVoitureService voitureService,
            IClientService clientService)
        {
            _reservationService = reservationService;
            _voitureService = voitureService;
            _clientService = clientService;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return View(reservations);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ReservationFormViewModel();
            await PopulerListesDeroulantesAsync(viewModel);
            return View(viewModel);
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PopulerListesDeroulantesAsync(viewModel);
                return View(viewModel);
            }

            var reservation = new Reservation
            {
                DateDebut = viewModel.DateDebut,
                DateFin = viewModel.DateFin,
                ClientId = viewModel.SelectedClientId,
                VoitureId = viewModel.SelectedVoitureId
            };

            var (success, message) = await _reservationService.CreateReservationAsync(reservation);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulerListesDeroulantesAsync(viewModel);
                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }

        // Méthode privée pour remplir les listes déroulantes
        private async Task PopulerListesDeroulantesAsync(ReservationFormViewModel model)
        {
            var clients = await _clientService.GetAllClientsAsync();
            var voitures = await _voitureService.GetAllVoituresAsync();

            model.Clients = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Nom} {c.Prenom}"
            }).ToList();

            model.Voitures = voitures.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"[{v.Immatriculation}] {v.Modele.Marque.Nom} {v.Modele.Nom}"
            }).ToList();
        }
    }
}