using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Locatic.Models
{
    public class ReservationFormViewModel
    {
        [Required(ErrorMessage = "La date de début est obligatoire.")]
        [DataType(DataType.Date)]
        public DateTime DateDebut { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La date de fin est obligatoire.")]
        [DataType(DataType.Date)]
        public DateTime DateFin { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Veuillez sélectionner un client.")]
        public int SelectedClientId { get; set; }

        [Required(ErrorMessage = "Veuillez sélectionner un véhicule.")]
        public int SelectedVoitureId { get; set; }

        public List<SelectListItem>? Clients { get; set; }
        public List<SelectListItem>? Voitures { get; set; }
    }
}