# ss

Deux projets indépendants dans le même dépôt : **Santinel** (application de
gestion du parc informatique) et **velvet-cat-jazz** (générateur de jazz lofi).

---

# Santinel — parc informatique

Application de bureau Windows qui recense les employés, leur poste de travail,
leurs équipements et la version de FileMaker installée. Fenêtre native
(pywebview) avec une interface HTML, données dans un simple fichier SQLite.

## Lancer en développement

```bash
pip install -r requirements.txt
python outils/exemples.py     # facultatif : quelques fiches de démonstration
python app/main.py
```

## Construire l'exécutable

Depuis Windows :

```bash
python construire.py          # produit dist/Santinel.exe
```

Un seul fichier, aucune installation de Python sur les postes. L'interface,
le logo et `schema.sql` sont embarqués dans l'exécutable, qui prend l'icône
`ressources/santinel.ico`.

`python construire.py --console` produit une variante qui garde la console
ouverte, à utiliser si l'exe se ferme sans message.

## Connexion

L'application s'ouvre sur un écran de connexion. Deux comptes :

| Compte | Mot de passe | Droits |
| --- | --- | --- |
| `Admin` | `Admin` | consultation **et** modification |
| `Invité` | `1234` | consultation seule |

Le nom d'utilisateur ignore la casse et les accents (`Invité`, `invite` et
`INVITE` sont le même compte) ; le mot de passe, non. Pour changer ces
identifiants sans recompiler :

```
SANTINEL_UTILISATEUR / SANTINEL_MOTDEPASSE          (compte administrateur)
SANTINEL_INVITE      / SANTINEL_INVITE_MOTDEPASSE   (compte en lecture)
```

Le menu **Compte**, à droite du bandeau, indique le compte connecté et ses
droits, et permet de se déconnecter.

Les droits sont appliqués côté Python, pas en masquant des boutons :
`_exiger_session` protège la lecture, `_exiger_administrateur` protège
l'écriture. Un compte en lecture seule voit la mention « Lecture seule » à
la place du bouton d'état, et `definir_actif` le rejetterait de toute façon.

Ce n'est pas pour autant un rempart sérieux — le fichier `parc.db` reste
lisible par quiconque a accès au poste ou au partage réseau. C'est une
barrière contre la consultation de passage, pas contre quelqu'un de
déterminé.

## Emplacement des données

Par défaut `%LOCALAPPDATA%\Santinel\parc.db`. Pour partager la même base entre
plusieurs postes, pointer la variable d'environnement `SANTINEL_DB` vers un
partage réseau — sans recompiler :

```
SANTINEL_DB=\\serveur\ti\parc.db
```

## Structure

| Chemin | Rôle |
| --- | --- |
| `app/main.py` | fenêtre pywebview, expose l'API Python au JavaScript |
| `app/donnees.py` | accès SQLite : `chercher`, `tous`, `retardataires` |
| `app/schema.sql` | tables `employe`, `ordinateur`, `equipement` + vue `v_fiche` |
| `app/interface/` | écran de connexion et interface HTML / CSS / JS |
| `app/interface/santinel.png` | logo (blanc, sur le bandeau marine) |
| `ressources/santinel.ico` | icône de l'exécutable, dérivée du logo |
| `outils/exemples.py` | jeu de données de démonstration |
| `outils/icone.py` | régénère l'icône à partir du logo |
| `construire.py` | empaquetage PyInstaller |

## Employés actifs et inactifs

Chaque fiche porte un état affiché à droite dans la liste : vert « Actif »,
rouge « Inactif ». Le bouton en haut à droite de la fiche (« Marquer
inactif » / « Réactiver ») bascule l'état et l'enregistre aussitôt dans la
colonne `employe.actif`, qui vaut 1 par défaut.

L'écriture passe par `Api.definir_actif`, protégée par la session au même
titre que la lecture : sans connexion, elle est refusée.

Les bases créées avant l'ajout de cette colonne sont mises à niveau
automatiquement au démarrage (voir `_migrer` dans `app/donnees.py`).

## Suivi des versions de FileMaker

`FILEMAKER_CIBLE` dans `app/donnees.py` définit la version considérée à jour
(actuellement `22`). Une fiche dont la version ne commence pas par cette valeur
— ou dont FileMaker n'est pas installé — apparaît dans l'onglet
« À mettre à jour ». Ajuster cette constante lors des montées de version.

---

# velvet-cat-jazz

Générateur de jazz lofi « vintage noir » — le genre de nappe qu'on trouve sur les
streams YouTube 24/7. Tout est **synthétisé** en Python/numpy, aucun échantillon,
aucune boucle préenregistrée : chaque rendu est une prise différente.

```bash
pip install numpy
python3 lofi_jazz.py --minutes 5 --out velvet_cat.wav
```

## Options

| Option | Défaut | Rôle |
| --- | --- | --- |
| `--minutes` | `3` | durée du morceau |
| `--bpm` | `72` | tempo (60–90 conseillé) |
| `--seed` | aléatoire | rejouer exactement le même morceau |
| `--no-lead` | — | supprime la trompette solo (pur fond de travail) |
| `--out` | `velvet_cat_jazz.wav` | fichier WAV 44,1 kHz stéréo 16 bits |

## Ce qu'il y a dedans

**Instruments** (synthèse pure) :

- *Rhodes* — FM à deux opérateurs (ratio 1 pour le corps, partielle 14 pour la cloche),
  index de modulation qui décroît vite pour l'attaque en « cloche ».
- *Contrebasse* — sinus + harmoniques 2 et 3, léger glissando d'attaque, bruit de doigt filtré.
- *Batterie aux balais* — bruit filtré en vague (frottement), frappe sur 2 et 4, ride swingué,
  grosse caisse à balayage de fréquence.
- *Trompette bouchée* — harmoniques impaires, vibrato retardé, souffle en bande passante.

**Harmonie** : trois grilles de 8 mesures en ré mineur (Dm9 – Gm11 – B♭maj7♯11 – A7♭9, etc.),
voicings sans fondamentale tirés au sort, walking bass avec approche chromatique vers l'accord
suivant, solo construit sur le mode de chaque accord (dorien / lydien / altéré) avec des silences.

**Chaîne lofi** : réverbération par convolution (RI synthétique), *wow & flutter* de bande
(rééchantillonnage modulé), passe-bas 7,2 kHz, saturation `tanh`, souffle et craquements de
vinyle, fondus d'entrée/sortie.

## Notes

Le rendu est hors-ligne (≈ 10 s de calcul par minute de musique). Pour un stream continu,
enchaînez plusieurs rendus avec des seeds différentes.
