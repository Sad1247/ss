"""Santinel — Parc informatique. Fenêtre native Windows.

Lancement en développement :  python app/main.py
Empaquetage :                 voir construire.py
"""

import os
import secrets
import sys
from pathlib import Path

import webview

import donnees

# Identifiants d'accès. Modifiables sans recompiler via les variables
# d'environnement SANTINEL_UTILISATEUR et SANTINEL_MOTDEPASSE.
UTILISATEUR = os.environ.get("SANTINEL_UTILISATEUR", "Admin")
MOT_DE_PASSE = os.environ.get("SANTINEL_MOTDEPASSE", "Admin")


def dossier_ressources() -> Path:
    """Fonctionne aussi bien en développement qu'une fois empaqueté."""
    if hasattr(sys, "_MEIPASS"):          # PyInstaller --onefile
        return Path(sys._MEIPASS)
    return Path(__file__).parent


class Api:
    """Méthodes appelées depuis le JavaScript via pywebview.api.

    Le parc n'est lisible qu'après une connexion réussie : le contrôle est
    fait ici, en Python, et pas seulement en masquant l'écran côté page.
    """

    def __init__(self):
        self.connecte = False

    # ----- session -----

    def connexion(self, utilisateur, motdepasse):
        """Renvoie True si les identifiants sont bons."""
        nom_ok = (utilisateur or "").strip().lower() == UTILISATEUR.lower()
        passe_ok = secrets.compare_digest(motdepasse or "", MOT_DE_PASSE)
        self.connecte = nom_ok and passe_ok
        return self.connecte

    def deconnexion(self):
        self.connecte = False
        return True

    def _exiger_session(self):
        if not self.connecte:
            raise PermissionError("Session non authentifiée.")

    # ----- parc -----

    def chercher(self, terme):
        self._exiger_session()
        return donnees.chercher(terme)

    def tous(self):
        self._exiger_session()
        return donnees.tous()

    def retardataires(self):
        self._exiger_session()
        return donnees.retardataires()


def main():
    donnees.initialiser()
    index = dossier_ressources() / "interface" / "index.html"
    webview.create_window(
        "Santinel — Parc informatique",
        str(index),
        js_api=Api(),
        width=1180,
        height=800,
        min_size=(860, 600),
    )
    # http_server=True évite les restrictions du protocole file://
    webview.start(http_server=True)


if __name__ == "__main__":
    main()
