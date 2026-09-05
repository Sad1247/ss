using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Un client : arrive, avance dans la file, recoit ses pizzas une a une,
    /// paie, puis s'en va. S'il attend trop longtemps, il part sans payer.
    /// </summary>
    public sealed class Client : MonoBehaviour
    {
        public int Pizzas { get; private set; }
        public bool EstServi => _recues >= Pizzas;
        public bool EstArrive { get; private set; }

        /// <summary>Le client parle au caissier : rien ne lui est remis avant la fin.</summary>
        public bool CommandeCommencee { get; private set; }
        public bool CommandeFinie => CommandeCommencee && _resteCommande <= 0f;

        Comptoir _comptoir;
        Pile _sac;
        Bulle _bulle;
        Vector3 _cible;
        bool _sEnVa;
        int _recues;
        float _patience;
        float _compteurRemise;
        float _resteCommande = Reglages.DureeCommande;

        public static Client Creer(Comptoir comptoir, int rang)
        {
            var go = new GameObject("Client");
            var c = go.AddComponent<Client>();
            c._comptoir = comptoir;
            c.Pizzas = Random.Range(Reglages.PizzasParClientMin, Reglages.PizzasParClientMax + 1);
            c._patience = Reglages.PatienceClient;

            // arrive depuis le fond, en dehors du champ
            go.transform.position = comptoir.PlaceDeLaFile(rang) + new Vector3(0f, 0f, -8f);
            c._cible = comptoir.PlaceDeLaFile(rang);
            c.Silhouette();
            return c;
        }

        void Silhouette()
        {
            var teinte = Color.Lerp(Bloc.Client, Bloc.Couleur(0x8FA3B4), Random.value);
            Bloc.Capsule("Corps", transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.62f, 0.62f, 0.62f), teinte)
                .SansCollision();
            Bloc.Bille("Tete", transform, new Vector3(0f, 1.45f, 0f), 0.5f, teinte).SansCollision();

            var sac = new GameObject("Sac");
            sac.transform.SetParent(transform, false);
            sac.transform.localPosition = new Vector3(0.55f, 1.1f, 0f);
            _sac = sac.AddComponent<Pile>();
            _sac.Max = Reglages.PizzasParClientMax;

            _bulle = Bulle.Creer(transform, 2.15f);
            _bulle.Afficher(Pizzas);
        }

        public void RangA(int rang)
        {
            if (_sEnVa) return;
            _cible = _comptoir.PlaceDeLaFile(rang);
        }

        void Update()
        {
            Avancer();
            if (_sEnVa || !EstArrive) return;

            _patience -= Time.deltaTime;
            if (_patience <= 0f)
            {
                _comptoir.Oublier(this);
                Partir();
            }
        }

        void Avancer()
        {
            var cible = _sEnVa ? _comptoir.PointDeSortie : _cible;
            var delta = cible - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude < 0.02f)
            {
                if (_sEnVa) { Destroy(gameObject); return; }
                EstArrive = true;
                return;
            }
            transform.position += delta.normalized * Reglages.VitesseClient * Time.deltaTime;
        }

        /// <summary>Egrene les secondes de commande une fois le client au comptoir.</summary>
        public void Commander(float deltaTemps)
        {
            CommandeCommencee = true;
            if (_resteCommande > 0f) _resteCommande -= deltaTemps;
        }

        /// <summary>Prend une pizza si le rythme de remise le permet.</summary>
        public bool Recevoir()
        {
            if (EstServi) return false;
            if (_compteurRemise > 0f) { _compteurRemise -= Time.deltaTime; return false; }
            _compteurRemise = Reglages.DelaiTransfert * 2f;
            _recues++;
            if (_sac != null) _sac.Ajouter();
            if (_bulle != null) _bulle.Afficher(Pizzas - _recues);
            return true;
        }

        public void Partir()
        {
            _sEnVa = true;
            if (_bulle != null) _bulle.Afficher(0);   // la commande n'a plus lieu d'etre
        }

        /// <summary>Ce qu'il reste a lui remettre, tel qu'affiche dans sa bulle.</summary>
        public int Restant => Pizzas - _recues;

        /// <summary>Vrai des qu'il quitte la file, servi ou lasse d'attendre.</summary>
        public bool SEnVa => _sEnVa;
    }
}
