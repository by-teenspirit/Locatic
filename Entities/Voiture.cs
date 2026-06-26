using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities
{
    public class Voiture
    {
        public int Id { get; set; }

        [Required]
        public string Immatriculation { get; set; } = null!;

        public int Annee { get; set; }

        public decimal TarifJournalier { get; set; }

        public int NbrPlaces { get; set; }

        public string Carburant { get; set; } = null!;

        // Clé étrangère vers le Modèle
        public int ModeleId { get; set; }
        public Modele Modele { get; set; } = null!;

        // Une voiture peut avoir plusieurs réservations
        public List<Reservation> Reservations { get; set; } = new();
    }
}