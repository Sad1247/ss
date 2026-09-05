# Pizzeria 3D — idle tycoon isometrique

Le jeu de la reference : on deplace un pizzaiolo au doigt, les fours produisent
en continu, on porte une pile de pizzas sur la tete, on la depose au comptoir,
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
  Four.cs       production continue et transfert vers le joueur
  Comptoir.cs   depot, file d'attente, service
  Client.cs     arrivee, file, reception pizza par pizza, paiement, depart
  Billet.cs     la liasse au sol, puis son vol vers le joueur
  ZoneAchat.cs  la dalle verte : l'argent s'ecoule, l'objet apparait
  Caissier.cs   l'employe embauche a la caisse
  Banque.cs     la caisse
  Hud.cs        compteur d'argent et indice contextuel

Scripts/Monde/
  Bloc.cs       fabrique de primitives colorees + la palette
  Pile.cs       une pile de pizzas (joueur, four, comptoir, sac client)
  Fumee.cs      les bouffees qui sortent de la cheminee
  Caisse.cs     la caisse enregistreuse et son tiroir coulissant
  Personnage.cs la silhouette commune au joueur et aux employes
  Batisseur.cs  monte toute la scene au lancement

Tests~/FakeUnity/            (ignore par Unity : le ~ final)
  FakeUnity3D.cs  faux runtime Unity (hierarchie, transforms, entrees, cycle de vie)
  Harness3D.cs    47 verifications qui jouent la boucle complete
```

## Verifier sans Unity

```bash
sudo apt-get install -y mono-mcs
mcs -out:h3d.exe pizzeria3d/Scripts/*/*.cs 'pizzeria3d/Tests~/FakeUnity/'*.cs
mono h3d.exe
```

Couvert : montage de la scene sans editeur, camera orthographique, manette
flottante et axes isometriques, apparition et course du joystick, production du four, chargement et plafond de la
pile portee, dechargement au comptoir, file d'attente, duree de commande et
ouverture puis fermeture du tiroir-caisse, encaissement au bareme, liasse qui
attend au sol puis se ramasse, dalle verte qui preleve puis livre,
embauche du caissier et service sans le joueur.

`Tests~/FakeUnity/` redefinit les types d'UnityEngine : c'est pour cela que son
dossier finit par `~`, qui le rend invisible a Unity.

## Ce qui n'y est pas encore

- Les employes qui font la navette (le caissier existe, pas le porteur).
- Un second four : il existait, sa dalle a ete retiree ; a rebrancher sur une
  nouvelle zone d'achat quand la progression le demandera.
- La chaine de production complete (pate, garnissage, four, mise en carton).
- Les ameliorations chiffrees : vitesse, capacite de portage, cadence des fours.
- La sauvegarde de la partie, les sons, les effets a l'encaissement.
- L'agrandissement du restaurant (nouvelles salles, decor qui se remplit).

## Ce qui est verifie, et ce qui ne l'est pas

La logique tourne : les 47 verifications ci-dessus s'executent hors editeur.
Le **rendu 3D n'a jamais ete affiche** — positions, echelles, cadrage de la
camera et couleurs ont ete poses sans jamais etre vus. C'est la premiere chose
a corriger a l'oeil au lancement.
