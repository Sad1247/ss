# Portage Unity — Bella Notte

Le coeur du jeu est deja ecrit, teste, et **independant d'Unity**. Il ne reste
que la partie visuelle a construire dans l'editeur.

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
  GameRunner.cs   MonoBehaviour qui fait avancer le temps et route les entrees
                  + interface IGameView a implementer par ton script d'UI

Tests/
  CoreTests.cs    9 tests de non-regression (voir plus bas)
```

## Installation dans le projet Unity

1. Copie `Scripts/` dans `Assets/Scripts/` de ton projet.
2. Cree un GameObject vide nomme `Game`, ajoute-lui le composant `GameRunner`.
3. Ecris ton script d'UI, fais-lui implementer `BellaNotte.Unity.IGameView`,
   et glisse-le dans le champ `vue` du `GameRunner`.
4. Branche tes boutons sur `UiEnfourner`, `UiSortirDuFour`, `UiServir`,
   `UiJeter`, `UiBasculer(int)` (l'index correspond a `IngredientId`).

Ne copie **pas** `Tests/CoreTests.cs` dans `Assets/` tel quel : il contient un
`Main()`. Pour tester dans Unity, recree ces assertions en NUnit via le
Test Runner (Window > General > Test Runner).

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
