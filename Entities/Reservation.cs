using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        // Lien avec la voiture
        public int VoitureId { get; set; }
        public Voiture Voiture { get; set; } = null!;

        // Lien avec le client
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;
    }
}