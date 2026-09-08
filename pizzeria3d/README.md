# Pizzeria 3D — idle tycoon isometrique

Le jeu de la reference : on deplace un pizzaiolo au doigt, les fours produisent
en continu, on porte une pile de pizzas a bout de bras, on la depose au comptoir,
les clients patientent, paient en lachant une liasse au sol, et les dalles
vertes debloquent du materiel quand on reste dessus.

**Aucun asset a importer.** Tout le decor est bati avec des primitives
(cubes, cylindres, capsules) en couleurs plates : c'est ce qui donne le look
low-poly du genre.

## Installation

Cree un projet Unity **3D** (pas 2D : un projet 2D n'affiche pas la 3D), puis
clone ce depot **directement dans le dossier `Assets/`** du projet :

```bash
cd "<ton projet Unity>/Assets"
git clone -b claude/pizza-game-h9uex7 https://github.com/Sad1247/ss.git Pizzeria
```

Sans terminal : GitHub Desktop, `File > Clone repository > URL`, et choisis le
dossier `Assets/Pizzeria` comme destination.

Puis appuie sur **Play**.

### Mettre a jour

```bash
cd "<ton projet Unity>/Assets/Pizzeria"
git pull
```

ou le bouton **Pull origin** dans GitHub Desktop. Unity recompile tout seul en
reprenant le focus. Il n'y a rien a recopier a la main.

Le depot est range pour supporter ce clonage : Unity ignore tout dossier dont le
nom finit par `~`, ce qui met hors de portee de la compilation le harnais de
test (`Tests~`) et l'ancien prototype 2D (`archive-jeu-2d~`). Sans cette regle,
ces fichiers casseraient le projet.

`Batisseur` monte le sol, le decor, le four, le comptoir, la zone d'achat et le
joueur au lancement. Il reutilise la Main Camera et la lumiere deja presentes
dans la scene plutot que d'en creer d'autres. Il n'y a pas de scene
a assembler ni de prefab a cabler.

Pour reprendre la main, supprime `Batisseur.cs` et pose les composants toi-meme.

## Commandes

Doigt ou souris, n'importe ou a l'ecran : un joystick apparait la ou tu
appuies, avec son socle et son bouton, et disparait quand tu relaches. Le reste se joue par proximite — s'approcher du four charge la
pile, s'approcher du comptoir la decharge, marcher sur une liasse l'encaisse,
rester sur une dalle verte l'achete.

## Ce qu'il y a

```
Scripts/Jeu/
  Reglages.cs   TOUT l'equilibrage (vitesses, prix, cadences, capacites)
  Joueur.cs     deplacement isometrique et pile portee
  Manette.cs    le joystick a l'ecran, flottant, dessine sans sprite
  Doigt.cs      lecture du doigt, ancien comme nouveau systeme d'entrees
  Obstacles.cs  les murs vus de dessus, et la resolution des deplacements
  Four.cs       production continue et transfert vers le joueur
  Comptoir.cs   depot, file d'attente, service
  Client.cs     arrivee, file, reception, paiement, repas a table, depart
  TableRepas.cs la table de la salle : une place, l'assiette, les restes a jeter
  Billet.cs     la liasse au sol, puis son vol vers le joueur
  ZoneAchat.cs  la dalle verte : l'argent s'ecoule, l'objet apparait
  PetitePiece.cs la piece du fond : porte automatique, pas libere, terrain etendu
  Bureau.cs      le bureau de la piece : le patron s'y assoit en s'arretant devant
  Personnel.cs   le registre du personnel : poste, salaire, embauche
  EcranBureau.cs l'ecran de l'ordinateur, affiche une fois le capot leve
  OrdinateurPortable.cs le portable : capot ferme, ouvert a la main quand on s'assoit
  MursDiscrets.cs escamote les murs qui cachent le joueur entre dans la piece
  Caissier.cs   l'employe : une pizza, son carton, sa boite, puis le comptoir
  Emballage.cs  le plan : reserve de cartons, rond rouge, rond vert, poste
  Banque.cs     la caisse
  Hud.cs        compteur d'argent et indice contextuel

Scripts/Monde/
  Bloc.cs       fabrique de primitives colorees + la palette
  Pile.cs       une pile de pizzas nues ou en boite (joueur, four, comptoir, sac)
  Pizza3D.cs    la pizza : pate en volume, garniture en texture generee
  Boite3D.cs    le carton a pizza : couvercle, rainure et logo imprime
  Logo.cs       le logo du couvercle, dessine pixel par pixel a l'execution
  Plateau3D.cs  le plateau de la salle, pour le repas sur place
  Bulle.cs      la bulle au-dessus d'un client, et le nombre qu'il attend
  Fumee.cs      les bouffees qui sortent de la cheminee
  Caisse.cs     la caisse enregistreuse et son tiroir coulissant
  Personnage.cs la silhouette commune a tous, habillage et coiffures
  Demarche.cs   le pas : jambes, genoux, bras, rebond et inclinaison du buste
  Batisseur.cs  monte toute la scene au lancement

Tests~/FakeUnity/            (ignore par Unity : le ~ final)
  FakeUnity3D.cs  faux runtime Unity (hierarchie, transforms, entrees, cycle de vie)
  Harness3D.cs    304 verifications qui jouent la boucle complete
```

## Regarder la scene sans Unity

Le banc d'essai peut ecrire la geometrie de la scene, et un petit script la
dessine dans la vue isometrique du jeu. Cela ne remplace pas Unity — ni
ombres, ni lumiere — mais cela repond a « qu'est-ce qu'on voit a cet
endroit ? ».

```bash
DUMP_SCENE=/tmp/scene.txt mono h3d.exe
DUMP_LOGO=/tmp/logo.ppm mono h3d.exe     # la texture du logo, seule
DUMP_UI=/tmp/ui.txt mono h3d.exe         # la mise en page de l'ecran du bureau
python3 'pizzeria3d/Tests~/Rendu/interface.py' /tmp/ui.txt ui.png 0.55
python3 'pizzeria3d/Tests~/Rendu/rendu.py' /tmp/scene.txt vue.png
# cadrage : zoom=52 centre=11.5,4.6  (coordonnees ecran, pas monde)
```

Il compose maintenant les rotations entieres — le capot du portable se rabat
autour de X. Reste une limite : il trie par objet, si bien que deux plaques
minces posees l'une sur l'autre peuvent passer l'une devant l'autre a tort
(le capot ferme laisse voir le clavier qu'il recouvre). Unity, lui, a un
tampon de profondeur ; pour verifier une telle superposition, masquer la piece
du dessous avec `cacher=`.

## Verifier sans Unity

```bash
sudo apt-get install -y mono-mcs
mcs -out:h3d.exe pizzeria3d/Scripts/*/*.cs 'pizzeria3d/Tests~/FakeUnity/'*.cs
mono h3d.exe
```

Couvert : montage de la scene sans editeur, camera orthographique, manette
flottante et accord du deplacement avec l'ecran, apparition et course du joystick, collisions et
glissement le long des murs, bulles de commande au-dessus des clients, production du four, chargement et plafond de la
pile portee, dechargement au comptoir, file d'attente, duree de commande et
ouverture puis fermeture du tiroir-caisse, encaissement au bareme, liasse qui
attend au sol puis se ramasse, dalle verte qui preleve puis livre,
embauche du caissier, navette qu'il fait jusqu'au four et service sans le
joueur.

`Tests~/FakeUnity/` redefinit les types d'UnityEngine : c'est pour cela que son
dossier finit par `~`, qui le rend invisible a Unity.

## A retirer avant publication

`Reglages.ArgentDepart` vaut 5000 pour essayer les achats sans jouer la
partie. Le remettre a 0.

## Ce qui n'y est pas encore

- Un second four : il existait, sa dalle a ete retiree ; a rebrancher sur une
  nouvelle zone d'achat quand la progression le demandera.
- La chaine de production complete (pate, garnissage, four, mise en carton).
- Les ameliorations chiffrees : vitesse, capacite de portage, cadence des fours.
- La sauvegarde de la partie, les sons, les effets a l'encaissement.
- L'agrandissement du restaurant (nouvelles salles, decor qui se remplit).

## Ce qui est verifie, et ce qui ne l'est pas

La logique tourne : les 304 verifications ci-dessus s'executent hors editeur.
Le **rendu 3D n'a jamais ete affiche** — positions, echelles, cadrage de la
camera et couleurs ont ete poses sans jamais etre vus. C'est la premiere chose
a corriger a l'oeil au lancement.
