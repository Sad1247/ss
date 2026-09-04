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

## Connexion et comptes

L'application s'ouvre sur un écran de connexion. Deux comptes sont créés au
tout premier lancement :

| Compte | Mot de passe | Rôle |
| --- | --- | --- |
| `Admin` | `Admin` | Administrateur |
| `Invité` | `1234` | Lecture seule |

Trois rôles :

| Rôle | Consulter | Modifier les fiches | Gérer les comptes |
| --- | :-: | :-: | :-: |
| Administrateur | ✓ | ✓ | ✓ |
| Modification | ✓ | ✓ | — |
| Lecture seule | ✓ | — | — |

Pendant la vérification, une trousse de premiers soins tourne sous les
champs. Le contrôle du mot de passe dure quelques dixièmes de seconde ; un
plancher de 700 ms laisse à l'animation le temps d'être vue plutôt que de
clignoter.

Le nom d'utilisateur ignore la casse et les accents (`Invité`, `invite` et
`INVITE` sont le même compte) ; le mot de passe, non.

Ensuite, tout se gère depuis l'application : menu **Compte** → **Compte**.
L'administrateur y voit la liste des comptes et peut en créer, changer un
rôle, changer un mot de passe (saisir le nouveau puis `Entrée`) ou supprimer
un compte avec le `×`. Un compte en lecture seule n'y voit que sa propre
fiche de session.

Deux garde-fous évitent de s'enfermer dehors : on ne peut ni changer son
propre rôle, ni supprimer le compte avec lequel on est connecté, et le
dernier administrateur ne peut pas être rétrogradé ni effacé.

Les mots de passe ne sont **jamais** conservés en clair : la table `compte`
garde un sel par compte et l'empreinte PBKDF2-SHA256 (200 000 itérations) du
mot de passe. Les variables `SANTINEL_UTILISATEUR`, `SANTINEL_MOTDEPASSE`,
`SANTINEL_INVITE` et `SANTINEL_INVITE_MOTDEPASSE` ne servent plus qu'à
choisir les identifiants créés au premier démarrage.

Les droits sont appliqués côté Python, pas en masquant des boutons :
`_exiger_session` protège la lecture, `_exiger_ecriture` protège les fiches
et `_exiger_administrateur` protège les comptes d'accès.

Cela dit, le fichier `parc.db` reste lisible par quiconque a accès au poste
ou au partage réseau. C'est une barrière contre la consultation de passage,
pas contre quelqu'un de déterminé.

## Thème clair ou sombre

Le bouton en croissant de lune, à gauche du menu Compte, bascule entre les
deux thèmes ; il devient un soleil une fois en sombre. **L'application
s'ouvre en mode sombre**, et le choix est retenu d'un lancement à l'autre.

Toutes les couleurs sont des variables CSS définies sur `:root` : le mode
sombre ne redéfinit que ces jetons dans `:root.sombre`, sans dupliquer une
seule règle de mise en page.

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
| `app/donnees.py` | accès SQLite : `chercher`, `tous`, `retardataires` et les écritures |
| `app/comptes.py` | comptes d'accès : empreintes, rôles, garde-fous |
| `app/schema.sql` | tables `employe`, `ordinateur`, `equipement` + vue `v_fiche` |
| `app/interface/` | écran de connexion et interface HTML / CSS / JS |
| `app/interface/santinel.png` | logo (blanc, sur le bandeau marine) |
| `ressources/santinel.ico` | icône de l'exécutable, dérivée du logo |
| `outils/exemples.py` | jeu de données de démonstration |
| `outils/importer.py` | import d'employés depuis un CSV du tableur |
| `donnees/employes.csv` | fiches importées depuis le tableur du parc |
| `outils/icone.py` | régénère l'icône à partir du logo |
| `construire.py` | empaquetage PyInstaller |

## Importer depuis le tableur

Le parc est tenu dans un tableur. `outils/importer.py` en reprend un export
CSV :

```bash
python outils/importer.py donnees/employes.csv
```

Une fiche déjà présente (même nom complet) est mise à jour, jamais dupliquée
ni supprimée — le script peut donc être rejoué après chaque export. Un
employé sans ordinateur est importé quand même.

Colonnes reconnues, telles qu'elles apparaissent dans le tableur : `Actif`,
`Prénom`, `Nom`, `Titre - Poste`, `Compagnie`, `Département`,
`Nom d'ordinateur`, `# Série`, `CPU`, `Portable`, `Mini-PC`, `FM22`,
`Upgrade Windows 11`. Les autres colonnes du tableur, `Statut` et
`License FM` comprises, sont ignorées.

**Piège du tableur** : `FM22` et `Upgrade Windows 11` sont des cases
*coloriées*. Une couleur ne s'exporte pas en CSV. Il faut écrire « Oui »
dans ces cases avant l'export, sinon elles arrivent vides et sont lues comme
« pas encore fait ».

## Modifier une fiche

Avec le compte administrateur, chacun des quatre blocs — **Emploi** (titre,
compagnie, département), **Coordonnées**
(téléphone, poste interne, nom d'utilisateur, courriel), **Poste de travail**
(dont processeur, type d'appareil et migration Windows 11), **Équipements** —
porte un bouton « Modifier ». Les
champs deviennent saisissables, `Entrée` enregistre, `Échap` ou « Annuler »
abandonne. Un seul bloc est modifiable à la fois, pour qu'on sache toujours
ce qui sera enregistré.

Un champ laissé vide est enregistré comme absent et la fiche affiche « — ».
Trois règles refusent une saisie incohérente : le courriel doit contenir une
arobase, la mise en service doit être une date `AAAA-MM-JJ`, et un
équipement doit avoir un type. Les lignes
d'équipement entièrement vides sont simplement ignorées, et le bouton `×`
retire une ligne.

Une fiche sans poste de travail en reçoit un au premier enregistrement, sans
manipulation particulière.

Le compte en lecture seule n'a aucun de ces boutons, et `definir_coordonnees`,
`definir_poste` et `definir_equipements` le refuseraient de toute façon.

## Supprimer une fiche

Le bouton **Supprimer**, en haut à droite de la fiche et réservé à
l'administrateur, demande d'abord confirmation : la suppression emporte le
poste de travail et les équipements (clés étrangères `ON DELETE CASCADE`) et
ne se défait pas.

Pour un simple départ, préférer « Marquer inactif » : la fiche reste
consultable et le matériel reste rattaché à quelqu'un.

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

Ce calcul n'est qu'un défaut : sous la version FileMaker, l'administrateur
décide au cas par cas avec **Mettre au suivi** / **Retirer du suivi**. Le
choix l'emporte sur la version — n'importe quelle fiche peut entrer dans
l'onglet, y compris un poste parfaitement à jour, et n'importe laquelle peut
en sortir. Un bouton **Automatique** rend la fiche au calcul par la version.

C'est la colonne `employe.suivi_manuel` : `NULL` (la version décide), `1`
(toujours dans l'onglet) ou `0` (jamais). Les bases existantes la reçoivent
au démarrage, en conservant les exclusions déjà faites.

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
