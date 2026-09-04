using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// La caisse enregistreuse. Son tiroir s'ouvre quand un client passe
    /// commande et se referme quand il repart : c'est le seul repere visuel
    /// qui dise au joueur qu'une commande est en cours.
    ///
    /// Le tiroir sort vers +z, cote caissier — jamais vers les clients, qui
    /// font la queue de l'autre cote.
    /// </summary>
    public sealed class Caisse : MonoBehaviour
    {
        const float Ferme = 0f;
        const float Ouvert = 0.46f;
        const float Vitesse = 2.6f;

        Transform _tiroir;
        float _cible;

        public bool EstOuverte => _cible > 0.01f;

        public void Ouvrir() => _cible = Ouvert;
        public void Fermer() => _cible = Ferme;

        void Update()
        {
            if (_tiroir == null) return;
            var p = _tiroir.localPosition;
            p.z = Mathf.Lerp(p.z, _cible, Vitesse * Time.deltaTime);
            _tiroir.localPosition = p;
        }

        // ------------------------------------------------------------------

        public static Caisse Creer(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Caisse");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(0.78f, 0.78f, 0.78f);
            var t = go.transform;

            var corps = Bloc.Couleur(0x3A3A3C);
            var corpsClair = Bloc.Couleur(0x4E4E52);
            var orange = Bloc.Couleur(0xF0562A);
            var gris = Bloc.Couleur(0xC9CDD2);
            var billet = Bloc.Couleur(0x4BC24B);

            // socle
            Bloc.Boite("Socle", t, new Vector3(0f, 0.20f, 0f), new Vector3(1.30f, 0.40f, 0.90f), corps)
                .SansCollision();
            Bloc.Boite("Liseré", t, new Vector3(0f, 0.40f, 0f), new Vector3(1.34f, 0.06f, 0.94f), gris)
                .SansCollision();

            // tiroir : glisse vers le caissier
            var tiroir = new GameObject("Tiroir");
            tiroir.transform.SetParent(t, false);
            tiroir.transform.localPosition = new Vector3(0f, 0f, 0f);
            Bloc.Boite("Caisson", tiroir.transform, new Vector3(0f, 0.14f, 0.02f),
                       new Vector3(1.10f, 0.24f, 0.72f), corpsClair).SansCollision();
            Bloc.Boite("Facade", tiroir.transform, new Vector3(0f, 0.14f, 0.39f),
                       new Vector3(1.14f, 0.28f, 0.06f), corps).SansCollision();
            Bloc.Disque("Bouton", tiroir.transform, new Vector3(0f, 0.14f, 0.43f), 0.16f, 0.06f, orange)
                .SansCollision();
            for (int i = 0; i < 3; i++)
                Bloc.Boite("Liasse" + i, tiroir.transform, new Vector3(-0.34f + i * 0.34f, 0.27f, 0.04f),
                           new Vector3(0.28f, 0.05f, 0.42f), billet).SansCollision();

            // plateau oranger, a gauche
            Bloc.Boite("Plateau", t, new Vector3(-0.34f, 0.47f, -0.06f), new Vector3(0.56f, 0.10f, 0.56f),
                       orange).SansCollision();

            // imprimante a tickets, a droite
            Bloc.Boite("Imprimante", t, new Vector3(0.44f, 0.52f, 0.04f), new Vector3(0.44f, 0.20f, 0.52f),
                       corps).SansCollision();
            for (int i = 0; i < 3; i++)
                Bloc.Boite("Touche" + i, t, new Vector3(0.32f + i * 0.11f, 0.63f, -0.10f),
                           new Vector3(0.08f, 0.04f, 0.10f), i == 1 ? orange : gris).SansCollision();
            for (int i = 0; i < 5; i++)
                Bloc.Boite("Ticket" + i, t, new Vector3(0.44f, 0.66f + i * 0.09f, 0.10f + i * 0.02f),
                           new Vector3(0.34f, 0.04f, 0.30f), i % 2 == 0 ? orange : corps).SansCollision();

            // ecran sur son pied, incline vers le caissier
            Bloc.Boite("Pied", t, new Vector3(-0.20f, 0.60f, 0.16f), new Vector3(0.22f, 0.26f, 0.18f), corps)
                .SansCollision();
            var ecran = Bloc.Boite("Ecran", t, new Vector3(-0.20f, 1.02f, 0.20f),
                                   new Vector3(0.86f, 0.70f, 0.12f), corps).SansCollision();
            ecran.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            var afficheur = Bloc.Boite("Afficheur", t, new Vector3(-0.20f, 1.24f, 0.13f),
                                       new Vector3(0.62f, 0.16f, 0.02f), gris).SansCollision();
            afficheur.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            // clavier : trois rangees de quatre touches, grises et orange
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 4; c++)
            {
                var couleur = c >= 2 ? orange : gris;
                var touche = Bloc.Boite($"T{r}{c}", t,
                                        new Vector3(-0.47f + c * 0.18f, 1.06f - r * 0.16f, 0.13f),
                                        new Vector3(0.13f, 0.11f, 0.02f), couleur);
                touche.SansCollision();
                touche.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            }

            var caisse = go.AddComponent<Caisse>();
            caisse._tiroir = tiroir.transform;
            return caisse;
        }
    }
}
