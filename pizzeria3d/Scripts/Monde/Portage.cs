using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Accroche une pile portee aux mains d'un personnage. Le point de portage
    /// est celui que Personnage a place devant le buste, a hauteur des paumes
    /// bras tendus : la pile suit donc le corps et son orientation, et les
    /// bras viennent se figer dessous (voir Demarche.BrasPortent).
    /// </summary>
    public static class Portage
    {
        /// <summary>Hauteur de repli si le personnage n'a pas de point de portage.</summary>
        public const float HauteurSecours = 1.0f;

        public static Pile Creer(Transform personnage, string nom)
        {
            var go = new GameObject(nom);
            var mains = personnage.Find("Corps/Mains");

            if (mains != null)
            {
                go.transform.SetParent(mains, false);
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go.transform.SetParent(personnage, false);
                go.transform.localPosition = new Vector3(0f, HauteurSecours, 0f);
            }

            return go.AddComponent<Pile>();
        }
    }
}
