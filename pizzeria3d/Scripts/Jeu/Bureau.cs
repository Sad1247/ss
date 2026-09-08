using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le bureau de la petite piece et son fauteuil de direction. Le patron
    /// s'y installe des qu'il s'arrete devant, les mains vides ; la manette le
    /// remet debout. Pas de bouton a apprendre : on se pose ou l'on s'assoit.
    /// </summary>
    public sealed class Bureau : MonoBehaviour
    {
        public Joueur Joueur;
        /// <summary>Le fauteuil : c'est lui qui donne la place assise.</summary>
        public Transform Siege;
        /// <summary>A quelle distance du siege on s'y installe.</summary>
        public float Rayon = 0.75f;

        public Vector3 Place => Siege != null ? Siege.position : transform.position;

        /// <summary>Vrai quand le joueur est installe a ce bureau.</summary>
        public bool Occupe => Joueur != null && Joueur.Assis;

        void Update()
        {
            if (Joueur == null || Siege == null) return;

            if (Joueur.Assis)
            {
                // Ecarte du fauteuil autrement qu'a la manette, il resterait
                // assis dans le vide, jambes pliees au milieu de la piece.
                if (!Joueur.EstPres(Place, Rayon)) Joueur.SeLever();
                return;
            }

            // Les mains pleines, il reste debout : sa pile de pizzas resterait
            // suspendue en l'air pendant que le buste plonge dans le fauteuil.
            if (Joueur.Portee != null && !Joueur.Portee.EstVide) return;

            // Tant qu'il pousse la manette il traverse la piece : s'asseoir en
            // passant devant le fauteuil serait un croche-pied.
            var m = Manette.Active;
            if (m != null && (m.Direction.x != 0f || m.Direction.y != 0f)) return;

            if (!Joueur.EstPres(Place, Rayon)) return;
            Joueur.Asseoir(Place, VersLeBureau(Place));
        }

        /// <summary>Le regard de celui qui s'installe : vers le plan de travail.</summary>
        public Vector3 VersLeBureau(Vector3 depuis)
        {
            var d = transform.position - depuis;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }
    }
}
