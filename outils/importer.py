"""Importe des employés depuis un fichier CSV exporté du tableur du parc.

    python outils/importer.py donnees/employes.csv

Une fiche déjà présente (même nom complet) est mise à jour, jamais dupliquée
ni supprimée. Un employé sans ordinateur est importé quand même.

Colonnes attendues, telles qu'elles apparaissent dans le tableur :

    Actif, Prénom, Nom, Titre - Poste, Compagnie, Département,
    Nom d'ordinateur, # Série, CPU, Portable, Mini-PC,
    FM22, Upgrade Windows 11

Les colonnes absentes sont simplement ignorées. Attention : dans le tableur,
« FM22 » et « Upgrade Windows 11 » sont des cases *coloriées*. Une couleur ne
s'exporte pas en CSV — il faut écrire « Oui » dans ces cases avant l'export,
sinon elles arrivent vides et sont lues comme « pas encore fait ».
"""

import csv
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "app"))

import donnees  # noqa: E402

# Version inscrite quand la case FM22 est cochée : le tableur dit « FM22 »,
# pas le numéro de build exact.
VERSION_FM22 = "22"


def valeur(ligne: dict, *noms: str) -> str:
    """Première colonne trouvée parmi plusieurs intitulés possibles."""
    for nom in noms:
        if ligne.get(nom) is not None:
            return (ligne[nom] or "").strip()
    return ""


def coche(ligne: dict, *noms: str) -> bool:
    return valeur(ligne, *noms).lower() not in ("", "0", "non", "no", "false")


def lire(chemin: Path) -> list[dict]:
    with chemin.open(encoding="utf-8-sig", newline="") as fichier:
        lignes = list(csv.DictReader(fichier))

    fiches = []
    for ligne in lignes:
        nom = " ".join(x for x in (valeur(ligne, "Prénom"), valeur(ligne, "Nom")) if x)
        if not nom:
            continue

        appareil = None
        if coche(ligne, "Portable"):
            appareil = "Portable"
        elif coche(ligne, "Mini-PC"):
            appareil = "Mini-PC"

        fiches.append({
            "nom": nom,
            "actif": valeur(ligne, "Actif").lower() != "inactif",
            "emploi": {
                "titre": valeur(ligne, "Titre - Poste", "Titre"),
                "compagnie": valeur(ligne, "Compagnie"),
                "service": valeur(ligne, "Département", "Departement", "Service"),
            },
            "poste": {
                "nom_ordinateur": valeur(ligne, "Nom d'ordinateur"),
                "modele": valeur(ligne, "Modèle", "Modele"),
                "numero_serie": valeur(ligne, "# Série", "# Serie", "Numéro de série"),
                "cpu": valeur(ligne, "CPU", "Processeur"),
                "type_appareil": appareil,
                "mise_en_service": valeur(ligne, "Mise en service"),
                "version_filemaker": VERSION_FM22 if coche(ligne, "FM22") else "",
                "windows11": coche(ligne, "Upgrade Windows 11"),
            },
        })
    return fiches


def importer(fiches: list[dict]) -> tuple[int, int]:
    donnees.initialiser()
    ajoutes = mis_a_jour = 0

    with donnees.connexion() as cx:
        existants = {ligne["nom"]: ligne["id"]
                     for ligne in cx.execute("SELECT id, nom FROM employe")}

    for fiche in fiches:
        employe_id = existants.get(fiche["nom"])
        if employe_id is None:
            with donnees.connexion() as cx:
                employe_id = cx.execute(
                    "INSERT INTO employe (nom, actif) VALUES (?, ?)",
                    (fiche["nom"], 1 if fiche["actif"] else 0),
                ).lastrowid
            ajoutes += 1
        else:
            donnees.definir_actif(employe_id, fiche["actif"])
            mis_a_jour += 1

        donnees.definir_emploi(employe_id, fiche["emploi"])
        if any(fiche["poste"].get(c) for c in ("nom_ordinateur", "numero_serie", "cpu")):
            donnees.definir_poste(employe_id, fiche["poste"])

    return ajoutes, mis_a_jour


def main(arguments: list[str]) -> int:
    if len(arguments) != 1:
        print(__doc__)
        return 2
    chemin = Path(arguments[0])
    if not chemin.exists():
        print(f"Fichier introuvable : {chemin}")
        return 1

    fiches = lire(chemin)
    ajoutes, mis_a_jour = importer(fiches)
    print(f"{ajoutes} fiche(s) ajoutée(s), {mis_a_jour} mise(s) à jour "
          f"dans {donnees.chemin_base()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
