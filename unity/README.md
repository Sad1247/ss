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
  PizzaRenderer.cs  dessine la pizza dans une texture generee (aucun sprite a importer)

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
- `Scripts/Unity/` : type-checke contre des stubs de l'API Unity, donc sans
  erreur de syntaxe ni de signature — mais **jamais execute dans un vrai
  editeur**. Les reglages visuels (tailles, marges, lisibilite sur telephone)
  sont a ajuster a l'oeil au premier lancement.
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
mcs -target:library -out:core.dll unity/Scripts/Core/*.cs
mcs -out:test.exe -r:core.dll unity/Tests/CoreTests.cs
mono test.exe
```

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
