using System.Collections.Generic;

namespace Pizzeria3D
{
    /// <summary>
    /// Le registre des tables de la salle. Les clients ne connaissent plus
    /// une table en particulier : ils demandent ici s'il en reste une de
    /// libre. Une table verrouillee — pas encore payee — n'y repond jamais,
    /// et une table debloquee devient utilisable sans que rien d'autre ait a
    /// etre prevenu : elle est deja inscrite, c'est son etat qui change.
    ///
    /// Ajouter une table plus tard tient donc en une ligne : la batir, puis
    /// l'inscrire.
    /// </summary>
    public static class Salles
    {
        static readonly List<TableRepas> _tables = new List<TableRepas>();

        public static IReadOnlyList<TableRepas> Tables => _tables;

        /// <summary>Repart de zero : le banc d'essai remonte la scene.</summary>
        public static void Vider() => _tables.Clear();

        public static void Inscrire(TableRepas table)
        {
            if (table == null || _tables.Contains(table)) return;
            _tables.Add(table);
        }

        /// <summary>Combien de tables sont batiees, verrouillees comprises.</summary>
        public static int Nombre => _tables.Count;

        /// <summary>Combien sont debloquees, qu'elles soient occupees ou non.</summary>
        public static int Deverrouillees
        {
            get
            {
                int n = 0;
                foreach (var t in _tables) if (t != null && t.Etat != EtatTable.Verrouillee) n++;
                return n;
            }
        }

        /// <summary>
        /// La premiere table ou un client peut s'installer : ni verrouillee,
        /// ni prise par un autre groupe, ni encombree des restes du
        /// precedent. Null s'il n'y en a aucune.
        /// </summary>
        public static TableRepas Libre()
        {
            foreach (var t in _tables)
                if (t != null && t.PeutAccueillir) return t;
            return null;
        }

        /// <summary>
        /// La premiere table debloquee qui attend d'etre debarrassee : c'est
        /// celle-la que le caissier va nettoyer.
        /// </summary>
        public static TableRepas ASale()
        {
            foreach (var t in _tables)
                if (t != null && t.Etat != EtatTable.Verrouillee && t.ADesOrdures) return t;
            return null;
        }
    }
}
