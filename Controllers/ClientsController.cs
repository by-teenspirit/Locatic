using Microsoft.AspNetCore.Mvc;
using Locatic.Entities;
using Locatic.Interfaces;
using Locatic.Models;

namespace Locatic.Controllers
{
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task<IActionResult> Index()
        {
            var clients = await _clientService.GetAllClientsAsync();
            return View(clients);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var client = new Client
            {
                Nom = viewModel.Nom.ToUpper(), 
                Prenom = viewModel.Prenom,
                Email = viewModel.Email,
                Telephone = viewModel.Telephone
            };

            await _clientService.AddClientAsync(client);
            return RedirectToAction(nameof(Index));
        }
    }
}