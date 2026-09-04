# Pizzeria 3D — idle tycoon isometrique

Le jeu de la reference : on deplace un pizzaiolo au doigt, les fours produisent
en continu, on porte une pile de pizzas sur la tete, on la depose au comptoir,
les clients patientent, paient en lachant une liasse au sol, et les dalles
vertes debloquent du materiel quand on reste dessus.

**Aucun asset a importer.** Tout le decor est bati avec des primitives
(cubes, cylindres, capsules) en couleurs plates : c'est ce qui donne le look
low-poly du genre.

## Installation

1. Cree un projet Unity **3D**. Pas 2D : un projet 2D n'affiche pas la 3D.
2. Copie **uniquement** `Scripts/` dans `Assets/` (jamais `Tests/`).
3. Appuie sur **Play**.

`Batisseur` monte le sol, le decor, le four, le comptoir, la zone d'achat et le
joueur au lancement. Il reutilise la Main Camera et la lumiere deja presentes
dans la scene plutot que d'en creer d'autres. Il n'y a pas de scene
a assembler ni de prefab a cabler.

Pour reprendre la main, supprime `Batisseur.cs` et pose les composants toi-meme.

## Commandes

Doigt ou souris, n'importe ou a l'ecran : une manette flottante apparait la ou
tu appuies. Le reste se joue par proximite — s'approcher du four charge la
pile, s'approcher du comptoir la decharge, marcher sur une liasse l'encaisse,
rester sur une dalle verte l'achete.

## Ce qu'il y a

```
Scripts/Jeu/
  Reglages.cs   TOUT l'equilibrage (vitesses, prix, cadences, capacites)
  Joueur.cs     manette flottante, deplacement isometrique, pile portee
  Four.cs       production continue et transfert vers le joueur
  Comptoir.cs   depot, file d'attente, service
  Client.cs     arrivee, file, reception pizza par pizza, paiement, depart
  Billet.cs     la liasse au sol, puis son vol vers le joueur
  ZoneAchat.cs  la dalle verte : l'argent s'ecoule, l'objet apparait
  Banque.cs     la caisse
  Hud.cs        compteur d'argent et indice contextuel

Scripts/Monde/
  Bloc.cs       fabrique de primitives colorees + la palette
  Pile.cs       une pile de pizzas (joueur, four, comptoir, sac client)
  Batisseur.cs  monte toute la scene au lancement

Tests/FakeUnity/
  FakeUnity3D.cs  faux runtime Unity (hierarchie, transforms, entrees, cycle de vie)
  Harness3D.cs    28 verifications qui jouent la boucle complete
```

## Verifier sans Unity

```bash
sudo apt-get install -y mono-mcs
mcs -out:h3d.exe pizzeria3d/Scripts/*/*.cs pizzeria3d/Tests/FakeUnity/*.cs
mono h3d.exe
```

Couvert : montage de la scene sans editeur, camera orthographique, manette
flottante et axes isometriques, production du four, chargement et plafond de la
pile portee, dechargement au comptoir, file d'attente, encaissement au bareme,
liasse qui attend au sol puis se ramasse, dalle verte qui preleve puis livre.

`Tests/FakeUnity/` ne doit **jamais** etre copie dans `Assets/` : il redefinit
les types d'UnityEngine.

## Ce qui n'y est pas encore

- Les employes qu'on embauche et qui font la navette a ta place.
- La chaine de production complete (pate, garnissage, four, mise en carton).
- Les ameliorations chiffrees : vitesse, capacite de portage, cadence des fours.
- La sauvegarde de la partie, les sons, les effets a l'encaissement.
- L'agrandissement du restaurant (nouvelles salles, decor qui se remplit).

## Ce qui est verifie, et ce qui ne l'est pas

La logique tourne : les 28 verifications ci-dessus s'executent hors editeur.
Le **rendu 3D n'a jamais ete affiche** — positions, echelles, cadrage de la
camera et couleurs ont ete poses sans jamais etre vus. C'est la premiere chose
a corriger a l'oeil au lancement.
