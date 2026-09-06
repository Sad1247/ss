using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Escamote les murs qui se dressent entre la camera et le joueur quand
    /// celui-ci entre dans la petite piece. La vue est isometrique et fixe :
    /// sans cela, le personnage disparait derriere le mur du fond des qu'il
    /// franchit la porte, et l'on joue a l'aveugle.
    ///
    /// Ne sont escamotes que les pans du cote de la camera — x croissant et z
    /// decroissant. Ceux du fond restent : ils donnent son volume a la piece.
    /// </summary>
    public sealed class MursDiscrets : MonoBehaviour
    {
        public Joueur Joueur;
        public GameObject[] Murs = new GameObject[0];

        /// <summary>Emprise de la piece, en coordonnees du monde.</summary>
        public Vector3 Centre;
        public float DemiX = 2f, DemiZ = 2f;
        /// <summary>
        /// Marge devant la porte. Courte a dessein : large, les murs
        /// s'effacaient alors que le joueur etait encore dans la salle, et la
        /// piece paraissait amputee de son cote droit.
        /// </summary>
        public float Marge = 0.2f;

        bool _escamotes;

        void OnEnable() => Appliquer(false);

        void Update()
        {
            if (Joueur == null) return;
            bool dedans = Dedans(Joueur.Position);
            if (dedans != _escamotes) Appliquer(dedans);
        }

        public bool Dedans(Vector3 p)
            => Mathf.Abs(p.x - Centre.x) <= DemiX &&
               p.z >= Centre.z - DemiZ - Marge && p.z <= Centre.z + DemiZ;

        void Appliquer(bool escamotes)
        {
            _escamotes = escamotes;
            foreach (var mur in Murs)
            {
                if (mur == null) continue;
                var rendu = mur.GetComponent<MeshRenderer>();
                if (rendu != null) rendu.enabled = !escamotes;
            }
        }

    }
}
