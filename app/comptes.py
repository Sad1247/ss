"""Comptes d'accès à Santinel : vérification et gestion.

Les mots de passe ne sont jamais conservés en clair. Chaque compte porte son
propre sel et l'empreinte PBKDF2-SHA256 du mot de passe ; à la connexion, on
recalcule l'empreinte et on compare.
"""

import hashlib
import os
import secrets
import unicodedata

import donnees

# Du plus large au plus étroit. « modification » touche aux fiches mais
# pas aux comptes d'accès.
ROLES = {
    "administrateur": "Administrateur",
    "modification": "Modification",
    "lecture": "Lecture seule",
}

ROLES_ECRITURE = ("administrateur", "modification")

ITERATIONS = 200_000
LONGUEUR_MINIMALE = 4


def cle(nom: str) -> str:
    """Identifiant de recherche : sans casse ni accents.

    « Invité », « invite » et « INVITE » désignent donc le même compte.
    """
    decompose = unicodedata.normalize("NFD", (nom or "").strip().lower())
    return "".join(c for c in decompose if not unicodedata.combining(c))


def _empreinte(motdepasse: str, sel: str) -> str:
    return hashlib.pbkdf2_hmac(
        "sha256", (motdepasse or "").encode(), bytes.fromhex(sel), ITERATIONS
    ).hex()


def initialiser() -> None:
    """Crée les comptes par défaut si la table est vide."""
    with donnees.connexion() as cx:
        if cx.execute("SELECT COUNT(*) FROM compte").fetchone()[0]:
            return
    creer(os.environ.get("SANTINEL_UTILISATEUR", "Admin"),
          os.environ.get("SANTINEL_MOTDEPASSE", "Admin"), "administrateur")
    creer(os.environ.get("SANTINEL_INVITE", "Invité"),
          os.environ.get("SANTINEL_INVITE_MOTDEPASSE", "1234"), "lecture")


def verifier(utilisateur: str, motdepasse: str) -> dict | None:
    """Renvoie {utilisateur, role} si le couple est bon, sinon None."""
    with donnees.connexion() as cx:
        ligne = cx.execute("SELECT * FROM compte WHERE identifiant = ?",
                           (cle(utilisateur),)).fetchone()
    if ligne is None:
        return None
    calcul = _empreinte(motdepasse or "", ligne["sel"])
    if not secrets.compare_digest(calcul, ligne["empreinte"]):
        return None
    return {"utilisateur": ligne["nom"], "role": ligne["role"]}


def lister() -> list[dict]:
    ordre = {role: rang for rang, role in enumerate(ROLES)}
    with donnees.connexion() as cx:
        lignes = cx.execute("SELECT nom, role FROM compte").fetchall()
    lignes = sorted(lignes, key=lambda l: (ordre[l["role"]], l["nom"]))
    return [{"utilisateur": l["nom"], "role": l["role"],
             "role_libelle": ROLES[l["role"]]} for l in lignes]


def creer(utilisateur: str, motdepasse: str, role: str) -> dict:
    nom = (utilisateur or "").strip()
    _valider(nom, motdepasse, role)
    identifiant = cle(nom)
    with donnees.connexion() as cx:
        if cx.execute("SELECT 1 FROM compte WHERE identifiant = ?",
                      (identifiant,)).fetchone():
            raise ValueError(f"Le compte « {nom} » existe déjà.")
        sel = secrets.token_hex(16)
        cx.execute(
            "INSERT INTO compte (identifiant, nom, sel, empreinte, role) "
            "VALUES (?, ?, ?, ?, ?)",
            (identifiant, nom, sel, _empreinte(motdepasse, sel), role),
        )
    return {"utilisateur": nom, "role": role, "role_libelle": ROLES[role]}


def definir_motdepasse(utilisateur: str, motdepasse: str) -> bool:
    if len(motdepasse or "") < LONGUEUR_MINIMALE:
        raise ValueError(
            f"Le mot de passe doit faire au moins {LONGUEUR_MINIMALE} caractères.")
    with donnees.connexion() as cx:
        _exiger(cx, utilisateur)
        sel = secrets.token_hex(16)
        cx.execute("UPDATE compte SET sel = ?, empreinte = ? WHERE identifiant = ?",
                   (sel, _empreinte(motdepasse, sel), cle(utilisateur)))
    return True


def definir_role(utilisateur: str, role: str) -> str:
    if role not in ROLES:
        raise ValueError(f"Rôle inconnu : {role}.")
    with donnees.connexion() as cx:
        ligne = _exiger(cx, utilisateur)
        if ligne["role"] == "administrateur" and role != "administrateur":
            _exiger_autre_administrateur(cx, utilisateur)
        cx.execute("UPDATE compte SET role = ? WHERE identifiant = ?",
                   (role, cle(utilisateur)))
    return role


def supprimer(utilisateur: str) -> str:
    with donnees.connexion() as cx:
        ligne = _exiger(cx, utilisateur)
        if ligne["role"] == "administrateur":
            _exiger_autre_administrateur(cx, utilisateur)
        cx.execute("DELETE FROM compte WHERE identifiant = ?", (cle(utilisateur),))
    return ligne["nom"]


def _valider(nom: str, motdepasse: str, role: str) -> None:
    if not nom:
        raise ValueError("Le nom d'utilisateur est obligatoire.")
    if len(motdepasse or "") < LONGUEUR_MINIMALE:
        raise ValueError(
            f"Le mot de passe doit faire au moins {LONGUEUR_MINIMALE} caractères.")
    if role not in ROLES:
        raise ValueError(f"Rôle inconnu : {role}.")


def _exiger(cx, utilisateur: str):
    ligne = cx.execute("SELECT * FROM compte WHERE identifiant = ?",
                       (cle(utilisateur),)).fetchone()
    if ligne is None:
        raise ValueError(f"Aucun compte nommé « {utilisateur} ».")
    return ligne


def _exiger_autre_administrateur(cx, utilisateur: str) -> None:
    """Empêche de se retrouver sans personne pour administrer le parc."""
    restants = cx.execute(
        "SELECT COUNT(*) FROM compte WHERE role = 'administrateur' "
        "AND identifiant <> ?", (cle(utilisateur),)).fetchone()[0]
    if not restants:
        raise ValueError(
            "C'est le dernier compte administrateur : le retirer fermerait "
            "l'accès à la gestion du parc.")
