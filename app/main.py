"""Santinel — Parc informatique. Fenêtre native Windows.

Lancement en développement :  python app/main.py
Empaquetage :                 voir construire.py
"""

import sys
from pathlib import Path

import webview

import donnees


def dossier_ressources() -> Path:
    """Fonctionne aussi bien en développement qu'une fois empaqueté."""
    if hasattr(sys, "_MEIPASS"):          # PyInstaller --onefile
        return Path(sys._MEIPASS)
    return Path(__file__).parent


class Api:
    """Méthodes appelées depuis le JavaScript via pywebview.api."""

    def chercher(self, terme):
        return donnees.chercher(terme)

    def tous(self):
        return donnees.tous()

    def retardataires(self):
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
