using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La porte interieure qui relie deux pieces entre elles, sans passer par
    /// la salle a manger. A la difference de PetitePiece, elle ne depend pas
    /// d'un seul achat : son passage ne s'ouvre que quand LES DEUX pieces
    /// qu'elle relie sont batiees, sinon on marcherait dans le vide de celle
    /// qui ne l'est pas encore. C'est pourquoi elle vit hors de l'une et de
    /// l'autre — sur un objet toujours actif — et surveille les deux.
    /// </summary>
    public sealed class PorteEntrePieces : MonoBehaviour
    {
        public GameObject PieceA;
        public GameObject PieceB;
        /// <summary>Numero de l'obstacle qui bouche le passage.</summary>
        public int Passage = -1;
        public Transform Gond;
        /// <summary>Le pas de la porte, en coordonnees du monde.</summary>
        public Vector3 Seuil;
        /// <summary>Le sens dans lequel on est repute « entre » — voir Profondeur.</summary>
        public Vector3 DirectionEntree = new Vector3(1f, 0f, 0f);
        public float RayonOuverture = 2.4f;
        public float Profondeur = 1.3f;
        public float AngleOuvert = 96f;
        public float VitesseBattant = 220f;
        public Joueur Joueur;

        float _angle;
        bool _passageOuvert;

        /// <summary>Vrai des que les deux pieces existent, l'une comme l'autre.</summary>
        public bool PretesToutesLesDeux
            => PieceA != null && PieceA.activeSelf && PieceB != null && PieceB.activeSelf;

        void Update()
        {
            if (PretesToutesLesDeux && !_passageOuvert)
            {
                Obstacles.Ouvrir(Passage);
                _passageOuvert = true;
            }

            float voulu = _passageOuvert && DoitSOuvrir() ? AngleOuvert : 0f;
            _angle = Mathf.MoveTowards(_angle, voulu, VitesseBattant * Time.deltaTime);
            if (Gond != null) Gond.localRotation = Quaternion.Euler(0f, _angle, 0f);
        }

        bool DoitSOuvrir()
        {
            if (Joueur == null) return false;
            var p = Joueur.Position;
            var delta = p - Seuil;
            if (Vector3.Dot(delta, DirectionEntree) > Profondeur) return false;   // il est entre
            delta.y = 0f;
            return delta.sqrMagnitude <= RayonOuverture * RayonOuverture;
        }
    }
}
