using System;

namespace Pizzeria3D
{
    /// <summary>La caisse. Tout passe par ici pour que le HUD suive.</summary>
    public static class Banque
    {
        public static int Solde { get; private set; }
        public static event Action Change;

        public static void Reinitialiser()
        {
            Solde = Reglages.ArgentDepart;   // zero en dehors de la mise au point
            Change?.Invoke();
        }

        public static void Encaisser(int montant)
        {
            if (montant <= 0) return;
            Solde += montant;
            Change?.Invoke();
        }

        /// <summary>Retire ce qu'on peut, jusqu'a <paramref name="voulu"/>, et dit combien.</summary>
        public static int Retirer(int voulu)
        {
            int pris = voulu < Solde ? voulu : Solde;
            if (pris <= 0) return 0;
            Solde -= pris;
            Change?.Invoke();
            return pris;
        }
    }
}
