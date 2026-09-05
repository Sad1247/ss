using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// L'employe embauche a la caisse. Tant qu'il n'est pas la, c'est le joueur
    /// qui doit se tenir au comptoir pour qu'un client soit servi ; une fois
    /// embauche, il s'en charge et libere le joueur.
    /// </summary>
    public sealed class Caissier : MonoBehaviour
    {
        public Comptoir Comptoir;

        Transform _corps;
        float _balancement;

        void OnEnable()
        {
            if (Comptoir != null) Comptoir.CaissierPresent = true;
        }

        void Awake()
        {
            _corps = transform.Find("Corps");
        }

        void Update()
        {
            if (_corps == null) return;
            // un leger balancement : plante raide, il aurait l'air d'un decor
            _balancement += Time.deltaTime * 1.6f;
            var p = _corps.localPosition;
            p.y = Mathf.Sin(_balancement) * 0.03f;
            _corps.localPosition = p;
        }
    }
}
