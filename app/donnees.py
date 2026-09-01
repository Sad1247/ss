"""Accès aux données du parc informatique Santinel."""

import os
import sqlite3
from datetime import date
from pathlib import Path

# Version de FileMaker considérée à jour. À ajuster lors des montées de version.
FILEMAKER_CIBLE = "22"


def chemin_base() -> Path:
    """Emplacement du fichier de données.

    SANTINEL_DB permet de pointer vers un partage réseau sans recompiler,
    par exemple  SANTINEL_DB=\\\\serveur\\ti\\parc.db
    """
    if os.environ.get("SANTINEL_DB"):
        return Path(os.environ["SANTINEL_DB"])
    base = Path(os.environ.get("LOCALAPPDATA", Path.home())) / "Santinel"
    base.mkdir(parents=True, exist_ok=True)
    return base / "parc.db"


def connexion() -> sqlite3.Connection:
    cx = sqlite3.connect(chemin_base())
    cx.row_factory = sqlite3.Row
    cx.execute("PRAGMA foreign_keys = ON")
    return cx


def initialiser() -> None:
    """Crée les tables si la base est neuve, et met à niveau les anciennes."""
    schema = (Path(__file__).parent / "schema.sql").read_text(encoding="utf-8")
    with connexion() as cx:
        _migrer(cx)
        cx.executescript(schema)


def _migrer(cx: sqlite3.Connection) -> None:
    """Ajoute les colonnes apparues après coup aux bases existantes.

    Tourne avant le schéma : la vue v_fiche s'appuie sur ces colonnes, et
    CREATE TABLE IF NOT EXISTS ne touche pas une table déjà là.
    """
    tables = {ligne[0] for ligne in
              cx.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    if "employe" not in tables:
        return                      # base neuve : le schéma s'en charge
    colonnes = {ligne["name"] for ligne in cx.execute("PRAGMA table_info(employe)")}
    if "actif" not in colonnes:
        cx.execute("ALTER TABLE employe ADD COLUMN actif INTEGER NOT NULL DEFAULT 1")
    for colonne in ("nom_utilisateur", "courriel", "titre",
                    "compagnie", "statut", "licence_fm"):
        if colonne not in colonnes:
            cx.execute(f"ALTER TABLE employe ADD COLUMN {colonne} TEXT")

    if "ordinateur" in tables:
        colonnes_poste = {l["name"] for l in cx.execute("PRAGMA table_info(ordinateur)")}
        for colonne, type_ in (("cpu", "TEXT"), ("type_appareil", "TEXT"),
                               ("windows11", "INTEGER")):
            if colonne not in colonnes_poste:
                cx.execute(f"ALTER TABLE ordinateur ADD COLUMN {colonne} {type_}")
    if "suivi_manuel" not in colonnes:
        cx.execute("ALTER TABLE employe ADD COLUMN suivi_manuel INTEGER")
        if "suivi_filemaker" in colonnes:      # choix faits avant le passage
            cx.execute("UPDATE employe SET suivi_manuel = 0 "
                       "WHERE suivi_filemaker = 0")
    if "suivi_filemaker" in colonnes:
        # la vue s'appuie dessus : la supprimer d'abord, schema.sql la refait
        cx.execute("DROP VIEW IF EXISTS v_fiche")
        cx.execute("ALTER TABLE employe DROP COLUMN suivi_filemaker")


def a_jour(version: str | None) -> bool:
    return bool(version) and version.strip().startswith(FILEMAKER_CIBLE)


def chercher(terme: str) -> list[dict]:
    """Recherche sur le nom, le poste, la série, le service, l'utilisateur
    ou le courriel."""
    terme = (terme or "").strip()
    if not terme:
        return []
    motif = f"%{terme}%"
    with connexion() as cx:
        lignes = cx.execute(
            """
            SELECT * FROM v_fiche
            WHERE nom LIKE ? OR nom_ordinateur LIKE ?
               OR numero_serie LIKE ? OR service LIKE ?
               OR nom_utilisateur LIKE ? OR courriel LIKE ?
               OR titre LIKE ? OR compagnie LIKE ?
            ORDER BY nom
            """,
            (motif,) * 8,
        ).fetchall()
        return [_fiche(cx, l) for l in lignes]


def tous() -> list[dict]:
    with connexion() as cx:
        lignes = cx.execute("SELECT * FROM v_fiche ORDER BY nom").fetchall()
        return [_fiche(cx, l) for l in lignes]


def retardataires() -> list[dict]:
    """Contenu de l'onglet « À mettre à jour »."""
    return [f for f in tous() if f["suivi"]]


def definir_actif(employe_id: int, actif: bool) -> bool:
    """Marque un employé actif ou inactif. Renvoie l'état enregistré."""
    with connexion() as cx:
        curseur = cx.execute(
            "UPDATE employe SET actif = ? WHERE id = ?",
            (1 if actif else 0, employe_id),
        )
        if curseur.rowcount == 0:
            raise ValueError(f"Aucun employé avec l'identifiant {employe_id}.")
    return bool(actif)


def definir_suivi(employe_id: int, suivi: bool) -> bool:
    """Inclut ou retire une fiche de l'onglet « À mettre à jour ».

    Le choix l'emporte sur la version de FileMaker : un poste parfaitement à
    jour peut être mis dans la liste, et un poste en retard en être sorti.
    """
    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute("UPDATE employe SET suivi_manuel = ? WHERE id = ?",
                   (1 if suivi else 0, employe_id))
    return bool(suivi)


def suivi_automatique(employe_id: int) -> None:
    """Rend la fiche au calcul automatique fondé sur la version FileMaker."""
    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute("UPDATE employe SET suivi_manuel = NULL WHERE id = ?",
                   (employe_id,))


def definir_coordonnees(employe_id: int, coordonnees: dict) -> dict:
    """Met à jour les coordonnées. Renvoie les valeurs gardées.

    Un champ laissé vide est enregistré comme absent (NULL) plutôt que comme
    une chaîne vide, pour que la fiche affiche « — » comme ailleurs.
    """
    valeurs = {c: ((coordonnees or {}).get(c) or "").strip() or None
               for c in CHAMPS_COORDONNEES}

    if valeurs["courriel"] and "@" not in valeurs["courriel"]:
        raise ValueError("Le courriel doit contenir une arobase.")

    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute(
            "UPDATE employe SET telephone = ?, poste_interne = ?, "
            "nom_utilisateur = ?, courriel = ? WHERE id = ?",
            (*(valeurs[c] for c in CHAMPS_COORDONNEES), employe_id),
        )
    return valeurs


CHAMPS_COORDONNEES = ("telephone", "poste_interne", "nom_utilisateur", "courriel")

CHAMPS_EMPLOI = ("titre", "compagnie", "service", "statut", "licence_fm")

CHAMPS_POSTE = ("nom_ordinateur", "modele", "numero_serie", "mise_en_service",
                "version_filemaker", "cpu", "type_appareil")


def _oui_non(brut) -> bool | None:
    """Lit un oui/non venant du formulaire ou d'un import. None = inconnu."""
    if brut is None:
        return None
    if isinstance(brut, str):
        texte = brut.strip().lower()
        if not texte:
            return None
        return texte not in ("0", "non", "no", "false", "n")
    return bool(brut)


def definir_emploi(employe_id: int, emploi: dict) -> dict:
    """Enregistre le titre, la compagnie, le département, le statut, la licence."""
    valeurs = {c: ((emploi or {}).get(c) or "").strip() or None
               for c in CHAMPS_EMPLOI}
    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute(
            f"UPDATE employe SET {', '.join(c + ' = ?' for c in CHAMPS_EMPLOI)} "
            "WHERE id = ?",
            (*(valeurs[c] for c in CHAMPS_EMPLOI), employe_id),
        )
    return valeurs


def definir_poste(employe_id: int, poste: dict) -> dict:
    """Enregistre le poste de travail d'un employé, en le créant au besoin."""
    valeurs = {c: ((poste or {}).get(c) or "").strip() or None for c in CHAMPS_POSTE}
    valeurs["windows11"] = _oui_non((poste or {}).get("windows11"))

    if valeurs["mise_en_service"]:
        try:
            date.fromisoformat(valeurs["mise_en_service"])
        except ValueError:
            raise ValueError(
                "La mise en service doit être une date au format AAAA-MM-JJ."
            ) from None

    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute(
            """
            INSERT INTO ordinateur
                (employe_id, nom_ordinateur, modele, numero_serie,
                 mise_en_service, version_filemaker, cpu, type_appareil,
                 windows11)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(employe_id) DO UPDATE SET
                nom_ordinateur    = excluded.nom_ordinateur,
                modele            = excluded.modele,
                numero_serie      = excluded.numero_serie,
                mise_en_service   = excluded.mise_en_service,
                version_filemaker = excluded.version_filemaker,
                cpu               = excluded.cpu,
                type_appareil     = excluded.type_appareil,
                windows11         = excluded.windows11
            """,
            (employe_id, *(valeurs[c] for c in CHAMPS_POSTE),
             None if valeurs["windows11"] is None else int(valeurs["windows11"])),
        )
    return valeurs


def definir_equipements(employe_id: int, equipements: list) -> list:
    """Remplace la liste des équipements d'un employé.

    Les lignes entièrement vides sont ignorées ; le type est obligatoire dès
    qu'une ligne porte quelque chose.
    """
    propres = []
    for equipement in equipements or []:
        type_ = (equipement.get("type") or "").strip()
        description = (equipement.get("description") or "").strip() or None
        serie = (equipement.get("numero_serie") or "").strip() or None
        if not type_ and description is None and serie is None:
            continue
        if not type_:
            raise ValueError("Chaque équipement doit avoir un type.")
        propres.append({"type": type_, "description": description,
                        "numero_serie": serie})
    propres.sort(key=lambda e: e["type"])

    with connexion() as cx:
        _exiger_employe(cx, employe_id)
        cx.execute("DELETE FROM equipement WHERE employe_id = ?", (employe_id,))
        cx.executemany(
            "INSERT INTO equipement (employe_id, type, description, numero_serie) "
            "VALUES (?, ?, ?, ?)",
            [(employe_id, e["type"], e["description"], e["numero_serie"])
             for e in propres],
        )
    return propres


def _suivi(ligne: sqlite3.Row) -> bool:
    """Figure dans « À mettre à jour » ? Le choix manuel prime sur la version."""
    if ligne["suivi_manuel"] is not None:
        return bool(ligne["suivi_manuel"])
    return not a_jour(ligne["version_filemaker"])


def _exiger_employe(cx: sqlite3.Connection, employe_id: int) -> None:
    if cx.execute("SELECT 1 FROM employe WHERE id = ?", (employe_id,)).fetchone() is None:
        raise ValueError(f"Aucun employé avec l'identifiant {employe_id}.")


def _fiche(cx: sqlite3.Connection, ligne: sqlite3.Row) -> dict:
    equipements = cx.execute(
        "SELECT type, description, numero_serie FROM equipement "
        "WHERE employe_id = ? ORDER BY type",
        (ligne["id"],),
    ).fetchall()
    return {
        "id": ligne["id"],
        "nom": ligne["nom"],
        "service": ligne["service"],
        "telephone": ligne["telephone"],
        "poste_interne": ligne["poste_interne"],
        "nom_utilisateur": ligne["nom_utilisateur"],
        "courriel": ligne["courriel"],
        "titre": ligne["titre"],
        "compagnie": ligne["compagnie"],
        "statut": ligne["statut"],
        "licence_fm": ligne["licence_fm"],
        "actif": bool(ligne["actif"]),
        "suivi": _suivi(ligne),
        "suivi_choisi": ligne["suivi_manuel"] is not None,
        "nom_ordinateur": ligne["nom_ordinateur"],
        "modele": ligne["modele"],
        "numero_serie": ligne["numero_serie"],
        "mise_en_service": ligne["mise_en_service"],
        "cpu": ligne["cpu"],
        "type_appareil": ligne["type_appareil"],
        "windows11": None if ligne["windows11"] is None else bool(ligne["windows11"]),
        "version_filemaker": ligne["version_filemaker"] or "Non installé",
        "filemaker_ok": a_jour(ligne["version_filemaker"]),
        "equipements": [dict(e) for e in equipements],
    }
