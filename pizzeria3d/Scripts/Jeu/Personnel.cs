using System;
using System.Collections.Generic;

namespace Pizzeria3D
{
    /// <summary>
    /// Le registre du personnel : qui travaille ici, a quel poste, pour quel
    /// salaire. Il alimente l'ecran du bureau — c'est la seule liste, il n'y
    /// a pas de copie ailleurs a tenir a jour.
    /// </summary>
    public static class Personnel
    {
        public sealed class Fiche
        {
            public string Nom;
            public string Poste;
            /// <summary>Salaire journalier, tel qu'affiche sur l'ecran du bureau.</summary>
            public int Salaire;
            /// <summary>Vrai quand la place est pourvue : le caissier peut n'etre pas encore embauche.</summary>
            public Func<bool> Embauche;

            public bool EstEmbauche => Embauche == null || Embauche();
        }

        static readonly List<Fiche> _fiches = new List<Fiche>();

        public static IReadOnlyList<Fiche> Fiches => _fiches;

        /// <summary>La paie du jour : seuls ceux qui sont en poste comptent.</summary>
        public static int MasseSalariale
        {
            get
            {
                int total = 0;
                foreach (var f in _fiches) if (f.EstEmbauche) total += f.Salaire;
                return total;
            }
        }

        public static void Inscrire(string nom, string poste, int salaire, Func<bool> embauche = null)
            => _fiches.Add(new Fiche { Nom = nom, Poste = poste, Salaire = salaire, Embauche = embauche });

        /// <summary>Raye une fiche du registre, par son nom.</summary>
        public static void Retirer(string nom)
            => _fiches.RemoveAll(f => f.Nom == nom);

        /// <summary>Vide le registre : la scene peut etre remontee d'un essai a l'autre.</summary>
        public static void Vider() => _fiches.Clear();
    }
}
