# Portage Unity — Bella Notte

Le jeu complet est ecrit : la logique (independante d'Unity, testee) **et**
l'interface (construite par code, sans scene a assembler). Tu copies les
scripts, tu appuies sur Play.

## Ce qu'il y a ici

```
Scripts/Core/     logique pure, aucun using UnityEngine
  Ingredient.cs   les 12 ingredients (2 bases + 10 garnitures)
  Recipe.cs       les 9 recettes de la carte + les prenoms clients
  GameConfig.cs   TOUT l'equilibrage (prix, cuisson, patience, difficulte)
  Pizza.cs        composition + cuisson de la pizza en cours
  Order.cs        un ticket client et sa patience
  ScoreRules.cs   evaluation d'un service -> paiement, pourboire, reproches
  PizzeriaGame.cs orchestration : Tick(dt), intentions du joueur, evenements

Scripts/Unity/
  Bootstrap.cs      monte le jeu au lancement : rien a poser dans la scene
  GameRunner.cs     fait avancer le temps, route les entrees, relaie les evenements
  GameView.cs       construit toute l'UI par code et la tient a jour
  PizzaRenderer.cs  dessine la pizza dans une texture generee (aucun sprite a importer) :
                    pate au bruit fractal, croute bombee, sauce a bords irreguliers,
                    mozzarella fondue et gratinee, garnitures aux formes propres

Tests/
  CoreTests.cs    9 tests de non-regression (voir plus bas)
```

## Installation dans le projet Unity

1. Cree un projet **2D (URP)**.
2. Copie `Scripts/` dans `Assets/Scripts/`.
3. Appuie sur **Play**.

C'est tout. `Bootstrap.cs` cree le Canvas, l'EventSystem et le jeu au lancement :
il n'y a aucune scene a assembler, aucun prefab a cabler, aucun sprite a importer
(la pizza est dessinee dans une texture generee a la volee).

Pour reprendre la main sur la mise en scene, supprime `Bootstrap.cs` et pose
`GameRunner` + `GameView` sur un GameObject vide. Pour ecrire ta propre interface,
supprime `GameView.cs`, implemente `BellaNotte.Unity.IGameView` sur ton script et
glisse-le dans le champ `vue` du `GameRunner`.

Boutons disponibles cote moteur : `UiEnfourner`, `UiSortirDuFour`, `UiServir`,
`UiJeter`, `UiSelectionner(int)`, `UiBasculer(int)` (l'index correspond a `IngredientId`).

Ne copie **pas** `Tests/CoreTests.cs` dans `Assets/` tel quel : il contient un
`Main()`. Pour tester dans Unity, recree ces assertions en NUnit via le
Test Runner (Window > General > Test Runner).

## Ce qui est verifie, et ce qui ne l'est pas

- `Scripts/Core/` : compile et passe 9 tests sous mono, hors Unity.
- `Scripts/Unity/` : **execute** dans un faux runtime Unity (`Tests/FakeUnity/`)
  qui implemente la hierarchie, AddComponent/GetComponent, le cycle de vie et
  les clics de boutons. 33 tests couvrent une partie complete : montage sans
  scene, garnissage par les boutons, verrouillage pendant la cuisson, service
  paye, ecran de fin, relance.
- Rendu de la pizza : mesure a **32 ms** par redessin sous mono pour une pizza
  a 5 garnitures, apres avoir fige le bruit fractal et la geometrie (le premier
  jet en coutait 900). Le precalcul du chargement prend 180 ms. IL2CPP est
  nettement plus rapide que mono sur ce genre de boucle, mais si la cuisson
  saccade sur un vieux telephone, deux leviers : elargir le pas de redessin
  dans `GameView.RedessinerSiBesoin`, ou calculer les pixels sur un thread de
  travail et n'appeler `SetPixels32`/`Apply` que sur le thread principal.
- Ce qui reste non verifie : le rendu reel a l'ecran. La mise en page a ete
  calee pour du portrait 1080x1920 sans jamais etre vue dans un editeur.
  Les tailles, marges et la lisibilite sur telephone sont a ajuster a l'oeil
  au premier lancement.
- Sous Unity 6, `FindObjectOfType` peut lever un avertissement de depreciation :
  remplace par `FindFirstObjectByType` si tu veux une console propre.

## La regle a tenir

`Core/` ne doit jamais contenir `using UnityEngine`, et `Scripts/Unity/` ne doit
jamais contenir de regle de jeu. C'est ce qui rend la logique testable en une
seconde au lieu d'un aller-retour dans l'editeur — et ce qui te laisse changer
d'avis sur le moteur sans tout reecrire.

Tout l'equilibrage est dans `GameConfig.cs` : c'est le seul fichier a toucher
pour regler la difficulte.

## Lancer les tests hors Unity

```bash
sudo apt-get install -y mono-mcs        # ou: dotnet

# regles du jeu
mcs -target:library -out:core.dll unity/Scripts/Core/*.cs
mcs -out:test.exe -r:core.dll unity/Tests/CoreTests.cs && mono test.exe

# interface, jouee dans le faux runtime
mcs -out:harness.exe unity/Scripts/Core/*.cs unity/Scripts/Unity/*.cs unity/Tests/FakeUnity/*.cs
mono harness.exe

# exporter le rendu de la pizza a 5 cuissons (fichiers PPM)
mono harness.exe --dump /tmp

# mesurer le cout du rendu, couche par couche
mono harness.exe --bench
```

Certains fichiers de test utilisent des lambdas typees plutot que des fonctions
locales : le compilateur mcs de Mono ne les accepte pas. Cette limite ne
concerne que ce harnais, pas le code du jeu.

`Tests/FakeUnity/` ne doit **jamais** etre copie dans `Assets/` : il redefinit
les types d'UnityEngine et entrerait en conflit avec le vrai moteur.

Couvert : pizza parfaite (prix + pourboire), garniture manquante (paiement
partiel), sortie automatique du four a 1.6 et refus de la pizza cramee,
garniture posee sans base, limite de 5 garnitures, depart d'un client
impatient, fin de partie a 3 clients perdus.

## Regles du jeu (reference)

- **Composition** : une base obligatoire (tomate ou creme), puis 1 a 5 garnitures.
- **Cuisson** : la jauge monte en continu. Zone parfaite entre 0.92 et 1.14.
  En dessous de 0.60 la pate est crue, au dela de 1.45 elle est cramee ;
  a 1.60 la pizza sort seule et est perdue.
- **Paiement** : `prix x ratio`, ou le ratio part de 1 et perd 0.28 par
  garniture manquante, 0.18 par garniture en trop, 0.35 si la base est fausse,
  le tout multiplie par le score de cuisson (1 / 0.6 / 0.5 / 0).
  Ratio de 1 = pourboire de 50 %.
- **Clients** : patience de `52 - 3 x niveau` secondes (plancher 22),
  4 tickets en salle au maximum. 3 clients perdus et le service s'arrete.
- **Difficulte** : un niveau tous les `4 + niveau` services ; les clients
  arrivent plus vite et patientent moins.

## Adaptation mobile a prevoir

Le prototype HTML est pense clavier/souris. Sur telephone, a revoir :
tickets condenses en icones plutot qu'en listes, cibles tactiles de 44 pt
minimum, glisser-deposer des ingredients, et une seule commande visible
en detail a la fois.
