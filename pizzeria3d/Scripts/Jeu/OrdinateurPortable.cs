using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Le portable du bureau : capot rabattu tant que personne n'est la. Le
    /// patron s'assoit, tend la main, le releve — et c'est seulement une fois
    /// le capot ouvert que l'ecran s'affiche. Un ordinateur qui s'allume tout
    /// seul, ferme, ne voudrait rien dire.
    /// </summary>
    public sealed class OrdinateurPortable : MonoBehaviour
    {
        public Bureau Bureau;
        /// <summary>Le pivot de la charniere : c'est lui qui porte l'ecran.</summary>
        public Transform Capot;

        /// <summary>Angle du capot rabattu, en degres autour de la charniere.</summary>
        public float AngleFerme = -90f;
        /// <summary>Ouvertures par seconde : un peu moins d'une demi-seconde.</summary>
        public float Vitesse = 2.2f;
        /// <summary>
        /// Le temps qu'il s'installe avant de tendre le bras. Sans ce battement
        /// le capot se levait dans la meme image que l'assise, et l'on ne
        /// voyait pas le geste.
        /// </summary>
        public float DelaiAvantOuverture = 0.45f;

        float _ouverture;      // 0 ferme, 1 ouvert
        float _attente;
        float _main;           // 0 bras au repos, 1 bras tendu vers le capot

        public float Ouverture => _ouverture;
        public bool Ouvert => _ouverture >= 0.999f;
        public bool Ferme => _ouverture <= 0.001f;
        /// <summary>Vrai pendant que la main est tendue vers le capot.</summary>
        public bool MainTendue => _main > 0.5f;

        void Awake()
        {
            _attente = DelaiAvantOuverture;
            Poser();
        }

        void Update()
        {
            bool assis = Bureau != null && Bureau.Occupe;
            if (!assis) _attente = DelaiAvantOuverture;
            else if (_attente > 0f) _attente -= Time.deltaTime;

            float voulue = assis && _attente <= 0f ? 1f : 0f;
            _ouverture = Mathf.MoveTowards(_ouverture, voulue, Vitesse * Time.deltaTime);
            Poser();

            // Le bras se tend des qu'il s'installe et retombe une fois le
            // capot leve : c'est lui qui l'a ouvert, pas une force invisible.
            float mainVoulue = assis && !Ouvert ? 1f : 0f;
            _main = Mathf.MoveTowards(_main, mainVoulue, 4f * Time.deltaTime);
            var pas = Bureau != null && Bureau.Joueur != null
                ? Bureau.Joueur.GetComponent<Demarche>() : null;
            if (pas != null) pas.BrasAvance = _main;
        }

        void Poser()
        {
            if (Capot == null) return;
            Capot.localRotation = Quaternion.Euler(Mathf.Lerp(AngleFerme, 0f, _ouverture), 0f, 0f);
        }
    }
}
