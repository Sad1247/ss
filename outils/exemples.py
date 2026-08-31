"""Remplit la base avec quelques fiches de démonstration.

    python outils/exemples.py          # base par défaut
    SANTINEL_DB=/tmp/essai.db python outils/exemples.py

N'insère rien si la base contient déjà des employés.
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
]

EQUIPEMENTS = {
    "Marie Tremblay": [("Écran", "Dell P2422H 24 pouces", "CN0X1Y2Z"),
                       ("Imprimante", "Brother HL-L2350DW", "U64312J0N")],
    "Jean Bourgeois": [("Station d'accueil", "Lenovo ThinkPad Dock Gen 2", "1S40AS0090")],
    "Sophie Lavoie": [("Écran", "HP E243 24 pouces", "6CM8241QRS"),
                      ("Téléphone IP", "Yealink T46S", "80AB12CD34")],
    "Alain Gagné": [("Lecteur code-barres", "Zebra DS2208", "18240522500123")],
}


def main() -> int:
    donnees.initialiser()
    with donnees.connexion() as cx:
        if cx.execute("SELECT COUNT(*) FROM employe").fetchone()[0]:
            print("La base contient déjà des fiches : rien à faire.")
            return 0

        for nom, service, tel, poste, ordi, modele, serie, date, fm in EMPLOYES:
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

    print(f"{len(EMPLOYES)} fiches insérées dans {donnees.chemin_base()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
