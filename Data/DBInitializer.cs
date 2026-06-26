using Locatic.Entities;

namespace Locatic.Data
{
    public static class DbInitializer
    {
        public static void Initialize(LocaticDbContext context)
        {
            // On s'assure que la base existe
            context.Database.EnsureCreated();

            // Si la base contient déjà des marques, on ne fait rien (on a déjà seed)
            if (context.Marques.Any())
            {
                return;
            }

            // 1. Création des Marques
            var renault = new Marque { Nom = "Renault", PaysOrigine = "France" };
            var peugeot = new Marque { Nom = "Peugeot", PaysOrigine = "France" };
            var bmw = new Marque { Nom = "BMW", PaysOrigine = "Allemagne" };

            context.Marques.AddRange(renault, peugeot, bmw);

            // 2. Création des Modèles liés aux Marques
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

            // On sauvegarde le tout en base
            context.SaveChanges();
        }
    }
}