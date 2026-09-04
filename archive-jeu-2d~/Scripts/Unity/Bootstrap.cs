using UnityEngine;
using UnityEngine.EventSystems;

namespace BellaNotte.Unity
{
    /// <summary>
    /// Monte le jeu tout seul au lancement : rien a poser dans la scene.
    /// Ouvre une scene vide, appuie sur Play.
    /// Pour reprendre la main, supprime ce fichier et pose GameRunner + GameView
    /// sur un GameObject a la main.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Demarrer()
        {
            if (Object.FindObjectOfType<GameRunner>() != null) return;   // deja pose a la main

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            var go = new GameObject("Game");
            go.AddComponent<GameRunner>();
            go.AddComponent<GameView>();
        }
    }
}
