namespace Locatic.Entities
{
    public class Modele
    {
        public int Id { get; set; }
        public string Nom { get; set; } = null!;

        public int MarqueId { get; set; }
        public Marque Marque { get; set; } = null!;
    }
}