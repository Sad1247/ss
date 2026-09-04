"""Santinel — Parc informatique. Fenêtre native Windows.

Lancement en développement :  python app/main.py
Empaquetage :                 voir construire.py
"""

import sys
from pathlib import Path

import webview

import comptes
import document
import donnees


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
        self.session = comptes.verifier(utilisateur, motdepasse)
        return self.session

    def deconnexion(self):
        self.session = None
        return True

    def _exiger_session(self):
        if self.session is None:
            raise PermissionError("Session non authentifiée.")

    def _exiger_ecriture(self):
        """Modifier les fiches : administrateur ou compte de modification."""
        self._exiger_session()
        if self.session["role"] not in comptes.ROLES_ECRITURE:
            raise PermissionError(
                "Ce compte est en lecture seule : modification refusée."
            )

    def _exiger_administrateur(self):
        """Gérer les comptes d'accès : administrateur seulement."""
        self._exiger_session()
        if self.session["role"] != "administrateur":
            raise PermissionError(
                "Seul un administrateur gère les comptes d'accès."
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

    def generer_document(self, employe_id):
        """Écrit le formulaire « Équipement prêté » de la fiche, et l'ouvre."""
        self._exiger_session()
        fiche = donnees.fiche(int(employe_id))
        chemin = document.generer(fiche, dossier_ressources() / "logo_bleu.png")
        return {"chemin": str(chemin), "ouvert": document.ouvrir(chemin)}

    def compte(self):
        """Renseignements sur la session en cours."""
        self._exiger_session()
        return {
            "utilisateur": self.session["utilisateur"],
            "role": self.session["role"],
            "role_libelle": comptes.ROLES[self.session["role"]],
            "base": str(donnees.chemin_base()),
        }

    # ----- gestion des comptes, administrateur seulement -----

    def lister_comptes(self):
        self._exiger_administrateur()
        return comptes.lister()

    def creer_compte(self, utilisateur, motdepasse, role):
        self._exiger_administrateur()
        return comptes.creer(utilisateur, motdepasse, role)

    def definir_role_compte(self, utilisateur, role):
        self._exiger_administrateur()
        self._exiger_pas_soi_meme(utilisateur, "changer son propre rôle")
        return comptes.definir_role(utilisateur, role)

    def definir_motdepasse_compte(self, utilisateur, motdepasse):
        self._exiger_administrateur()
        return comptes.definir_motdepasse(utilisateur, motdepasse)

    def supprimer_compte(self, utilisateur):
        self._exiger_administrateur()
        self._exiger_pas_soi_meme(utilisateur, "supprimer le compte ouvert")
        return comptes.supprimer(utilisateur)

    def _exiger_pas_soi_meme(self, utilisateur, geste):
        """Se retirer soi-même les droits laisserait une session incohérente."""
        if comptes.cle(utilisateur) == comptes.cle(self.session["utilisateur"]):
            raise PermissionError(f"Impossible de {geste}.")

    def creer_employe(self, nom):
        """Crée une fiche. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.creer_employe(nom)

    def definir_actif(self, employe_id, actif):
        """Bascule l'état d'emploi d'une fiche. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.definir_actif(int(employe_id), bool(actif))

    def definir_suivi(self, employe_id, suivi):
        """Inclut ou retire une fiche du suivi FileMaker."""
        self._exiger_ecriture()
        return donnees.definir_suivi(int(employe_id), bool(suivi))

    def suivi_automatique(self, employe_id):
        """Rend la fiche au calcul automatique."""
        self._exiger_ecriture()
        donnees.suivi_automatique(int(employe_id))
        return True

    def definir_coordonnees(self, employe_id, coordonnees):
        """Enregistre les coordonnées d'une fiche. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.definir_coordonnees(int(employe_id), coordonnees)

    def supprimer(self, employe_id):
        """Supprime une fiche. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.supprimer(int(employe_id))

    def definir_emploi(self, employe_id, emploi):
        """Enregistre le bloc Emploi. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.definir_emploi(int(employe_id), emploi)

    def definir_poste(self, employe_id, poste):
        """Enregistre le poste de travail. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.definir_poste(int(employe_id), poste)

    def definir_equipements(self, employe_id, equipements):
        """Remplace les équipements d'une fiche. Réservé aux comptes qui peuvent modifier."""
        self._exiger_ecriture()
        return donnees.definir_equipements(int(employe_id), equipements)


def main():
    donnees.initialiser()
    comptes.initialiser()
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
