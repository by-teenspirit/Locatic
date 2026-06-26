using Locatic.Entities;

namespace Locatic.Data
{
    public static class DbInitializer
    {
        public static void Initialize(LocaticDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Marques.Any())
            {
                return;
            }

            // 1. Création des Marques
            var renault = new Marque { Nom = "Renault", PaysOrigine = "France" };
            var peugeot = new Marque { Nom = "Peugeot", PaysOrigine = "France" };
            var bmw = new Marque { Nom = "BMW", PaysOrigine = "Allemagne" };

            context.Marques.AddRange(renault, peugeot, bmw);

            var modeles = new List<Modele>
            {
                new Modele { Nom = "Clio", Marque = renault },
                new Modele { Nom = "Megane", Marque = renault },
                new Modele { Nom = "208", Marque = peugeot },
                new Modele { Nom = "3008", Marque = peugeot },
                new Modele { Nom = "Série 3", Marque = bmw },
                new Modele { Nom = "X5", Marque = bmw }
            };

            context.Modeles.AddRange(modeles);

            context.SaveChanges();
        }
    }
}