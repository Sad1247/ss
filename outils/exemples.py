"""Remplit la base avec quelques fiches de démonstration.

    python outils/exemples.py          # base par défaut
    SANTINEL_DB=/tmp/essai.db python outils/exemples.py

Le script est re-jouable : il n'ajoute que les fiches absentes, et ne
touche pas à celles qui existent déjà.
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "app"))

import donnees  # noqa: E402

EMPLOYES = [
    # nom, service, téléphone, poste, ordinateur, modèle, série, mise en service, FileMaker
    ("Marie Tremblay", "Comptabilité", "418 555-0142", "221",
     "COMPTA-01", "Dell OptiPlex 7010", "5KJ9L2Q", "2023-04-11", "22.0.1"),
    ("Jean Bourgeois", "Direction", "418 555-0118", "201",
     "DIR-PORT-03", "Lenovo ThinkPad T14", "PF3H8821", "2024-09-02", "21.1.3"),
    ("Sophie Lavoie", "Ressources humaines", "418 555-0177", "245",
     "RH-02", "HP EliteDesk 800", "CZC2410XYZ", "2022-01-20", None),
    ("Alain Gagné", "Entrepôt", "418 555-0190", "310",
     "ENTREPOT-01", "Dell OptiPlex 3000", "9WQ4T1B", "2025-02-17", "22.0.1"),
    ("Saad", "Informatique", "418 555-0101", "200",
     "TI-PORT-01", "Dell Latitude 5450", "H7X2M9P", "2025-06-09", "22.0.1"),
    ("Karine Doucet", "Marketing", "418 555-0163", "228",
     "MARKET-04", "Lenovo ThinkCentre M70q", "MJ0AB1CD", "2023-11-06", "20.3.1"),
    ("Luc Marchand", "Expédition", "418 555-0184", "305",
     "EXPED-02", "HP ProDesk 400 G9", "8CG3120FGH", "2024-03-25", None),
]

# Fiches renommées après coup : appliqué aux bases déjà remplies.
RENOMMAGES = {"Saad Anjar": "Saad"}

EQUIPEMENTS = {
    "Marie Tremblay": [("Écran", "Dell P2422H 24 pouces", "CN0X1Y2Z"),
                       ("Imprimante", "Brother HL-L2350DW", "U64312J0N")],
    "Jean Bourgeois": [("Station d'accueil", "Lenovo ThinkPad Dock Gen 2", "1S40AS0090")],
    "Sophie Lavoie": [("Écran", "HP E243 24 pouces", "6CM8241QRS"),
                      ("Téléphone IP", "Yealink T46S", "80AB12CD34")],
    "Alain Gagné": [("Lecteur code-barres", "Zebra DS2208", "18240522500123")],
    "Saad": [("Écran", "Dell U2723QE 27 pouces", "CN0P4R5T"),
             ("Écran", "Dell U2723QE 27 pouces", "CN0P4R5U"),
             ("Station d'accueil", "Dell WD19S", "5J8K2L1M")],
    "Karine Doucet": [("Écran", "Lenovo ThinkVision T24i", "V906C2XY"),
                      ("Casque", "Jabra Evolve2 40", "JB4471002")],
    "Luc Marchand": [("Imprimante étiquettes", "Zebra ZD421", "D4J213900456"),
                     ("Téléphone IP", "Yealink T43U", "80CD34EF56")],
}


def main() -> int:
    donnees.initialiser()
    ajoutes = 0
    with donnees.connexion() as cx:
        for ancien, nouveau in RENOMMAGES.items():
            cx.execute("UPDATE employe SET nom = ? WHERE nom = ?", (nouveau, ancien))

        deja = {ligne[0] for ligne in cx.execute("SELECT nom FROM employe")}

        for nom, service, tel, poste, ordi, modele, serie, date, fm in EMPLOYES:
            if nom in deja:
                continue
            cur = cx.execute(
                "INSERT INTO employe (nom, service, telephone, poste_interne) "
                "VALUES (?, ?, ?, ?)",
                (nom, service, tel, poste),
            )
            employe_id = cur.lastrowid
            cx.execute(
                "INSERT INTO ordinateur (employe_id, nom_ordinateur, modele, "
                "numero_serie, mise_en_service, version_filemaker) "
                "VALUES (?, ?, ?, ?, ?, ?)",
                (employe_id, ordi, modele, serie, date, fm),
            )
            for type_, description, serie_eq in EQUIPEMENTS.get(nom, []):
                cx.execute(
                    "INSERT INTO equipement (employe_id, type, description, numero_serie) "
                    "VALUES (?, ?, ?, ?)",
                    (employe_id, type_, description, serie_eq),
                )
            ajoutes += 1

    if ajoutes:
        print(f"{ajoutes} fiche(s) ajoutée(s) dans {donnees.chemin_base()}")
    else:
        print("Toutes les fiches d'exemple sont déjà présentes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
