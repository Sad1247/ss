using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Fait marcher un personnage : jambes et bras se balancent, le buste
    /// rebondit. L'allure est deduite du deplacement reel plutot que d'un
    /// ordre exterieur — le meme composant sert donc au joueur comme aux
    /// employes, sans qu'aucun n'ait a le prevenir.
    /// </summary>
    public sealed class Demarche : MonoBehaviour
    {
        public Transform Corps, HancheG, HancheD, EpauleG, EpauleD;

        /// <summary>Amplitude du balancement, en degres, a pleine vitesse.</summary>
        public float Amplitude = 42f;
        public float VitesseReference = 6f;

        /// <summary>
        /// Vrai quand le personnage porte quelque chose : les bras se figent
        /// alors devant lui. Sans cela la pile, posee au point de portage,
        /// resterait immobile pendant que les mains, elles, se balanceraient.
        /// </summary>
        public bool BrasPortent;

        Vector3 _precedente;
        float _phase;
        float _allure;          // 0 a l'arret, 1 a pleine vitesse

        public float Phase => _phase;
        public float Allure => _allure;

        void Awake() => _precedente = transform.position;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            var delta = transform.position - _precedente;
            delta.y = 0f;
            _precedente = transform.position;

            float vitesse = delta.magnitude / dt;
            float voulue = Mathf.Clamp01(vitesse / VitesseReference);
            // lissage : sans lui, la moindre saccade ferait tressauter les jambes
            _allure = Mathf.Lerp(_allure, voulue, Mathf.Clamp01(dt * 10f));

            if (_allure > 0.02f) _phase += dt * (4f + _allure * 7f);
            else _phase = Mathf.Lerp(_phase, 0f, Mathf.Clamp01(dt * 8f));

            float angle = Mathf.Sin(_phase) * Amplitude * _allure;
            Tourner(HancheG, angle);
            Tourner(HancheD, -angle);

            if (BrasPortent)
            {
                Tourner(EpauleG, Personnage.AngleBrasPortant);
                Tourner(EpauleD, Personnage.AngleBrasPortant);
            }
            else
            {
                Tourner(EpauleG, -angle * 0.75f);
                Tourner(EpauleD, angle * 0.75f);
            }

            if (Corps == null) return;
            var p = Corps.localPosition;
            // rebond au pas quand il marche, respiration quand il attend
            p.y = _allure > 0.02f
                ? Mathf.Abs(Mathf.Sin(_phase)) * 0.05f * _allure
                : Mathf.Sin(Time.time * 1.6f) * 0.02f;
            Corps.localPosition = p;
        }

        static void Tourner(Transform pivot, float angle)
        {
            if (pivot != null) pivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
