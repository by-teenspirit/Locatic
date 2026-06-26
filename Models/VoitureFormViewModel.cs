using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Locatic.Models
{
    public class VoitureFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "L'immatriculation est obligatoire.")]
        [RegularExpression(@"^[A-Z]{2}-\d{3}-[A-Z]{2}$", ErrorMessage = "Le format doit être AA-123-AA")]
        public string Immatriculation { get; set; } = null!;

        [Required(ErrorMessage = "L'année est obligatoire.")]
        [Range(1900, 2027, ErrorMessage = "Année invalide.")]
        public int Annee { get; set; }

        [Required(ErrorMessage = "Le tarif est obligatoire.")]
        [Range(1, 1000, ErrorMessage = "Le tarif doit être compris entre 1€ et 1000€.")]
        public decimal TarifJournalier { get; set; }

        [Required(ErrorMessage = "Le nombre de places est obligatoire.")]
        [Range(1, 9, ErrorMessage = "Une voiture doit avoir entre 1 et 9 places.")]
        public int NbrPlaces { get; set; }

        [Required(ErrorMessage = "Le type de carburant est obligatoire.")]
        public string Carburant { get; set; } = null!;

        [Required(ErrorMessage = "Veuillez sélectionner un modèle.")]
        public int SelectedModeleId { get; set; }

        // Cette liste va servir à remplir la liste déroulante HTML (<select>)
        public List<SelectListItem>? Modeles { get; set; }
    }
}