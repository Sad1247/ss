"""Santinel — Parc informatique. Fenêtre native Windows.

Lancement en développement :  python app/main.py
Empaquetage :                 voir construire.py
"""

import os
import secrets
import sys
import unicodedata
from pathlib import Path

import webview

import donnees

# Comptes d'accès, chacun avec son rôle :
#   administrateur — consultation et modification
#   lecture        — consultation seule
# Modifiables sans recompiler par variables d'environnement.
COMPTES = {
    "admin": {
        "nom": os.environ.get("SANTINEL_UTILISATEUR", "Admin"),
        "motdepasse": os.environ.get("SANTINEL_MOTDEPASSE", "Admin"),
        "role": "administrateur",
    },
    "invite": {
        "nom": os.environ.get("SANTINEL_INVITE", "Invité"),
        "motdepasse": os.environ.get("SANTINEL_INVITE_MOTDEPASSE", "1234"),
        "role": "lecture",
    },
}

ROLES = {"administrateur": "Administrateur", "lecture": "Lecture seule"}


def cle_compte(nom: str) -> str:
    """Clé de recherche d'un compte : sans casse ni accents.

    « Invité », « invite » ou « INVITE » désignent donc le même compte.
    """
    decompose = unicodedata.normalize("NFD", (nom or "").strip().lower())
    return "".join(c for c in decompose if not unicodedata.combining(c))


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
        self.session = None

    # ----- session -----

    def connexion(self, utilisateur, motdepasse):
        """Ouvre la session. Renvoie le compte, ou None si le couple est faux."""
        self.session = None
        compte = COMPTES.get(cle_compte(utilisateur))
        if compte and secrets.compare_digest(motdepasse or "", compte["motdepasse"]):
            self.session = {"utilisateur": compte["nom"], "role": compte["role"]}
        return self.session

    def deconnexion(self):
        self.session = None
        return True

    def _exiger_session(self):
        if self.session is None:
            raise PermissionError("Session non authentifiée.")

    def _exiger_administrateur(self):
        self._exiger_session()
        if self.session["role"] != "administrateur":
            raise PermissionError(
                "Ce compte est en lecture seule : modification refusée."
            )

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

    def compte(self):
        """Renseignements sur la session en cours."""
        self._exiger_session()
        return {
            "utilisateur": self.session["utilisateur"],
            "role": self.session["role"],
            "role_libelle": ROLES[self.session["role"]],
            "base": str(donnees.chemin_base()),
        }

    def definir_actif(self, employe_id, actif):
        """Bascule l'état d'emploi d'une fiche. Réservé à l'administrateur."""
        self._exiger_administrateur()
        return donnees.definir_actif(int(employe_id), bool(actif))

    def definir_suivi(self, employe_id, suivi):
        """Inclut ou retire une fiche du suivi FileMaker. Administrateur."""
        self._exiger_administrateur()
        return donnees.definir_suivi(int(employe_id), bool(suivi))

    def suivi_automatique(self, employe_id):
        """Rend la fiche au calcul automatique. Administrateur."""
        self._exiger_administrateur()
        donnees.suivi_automatique(int(employe_id))
        return True

    def definir_coordonnees(self, employe_id, telephone, poste_interne):
        """Enregistre les coordonnées d'une fiche. Réservé à l'administrateur."""
        self._exiger_administrateur()
        return donnees.definir_coordonnees(int(employe_id), telephone, poste_interne)

    def definir_poste(self, employe_id, poste):
        """Enregistre le poste de travail. Réservé à l'administrateur."""
        self._exiger_administrateur()
        return donnees.definir_poste(int(employe_id), poste)

    def definir_equipements(self, employe_id, equipements):
        """Remplace les équipements d'une fiche. Réservé à l'administrateur."""
        self._exiger_administrateur()
        return donnees.definir_equipements(int(employe_id), equipements)


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
