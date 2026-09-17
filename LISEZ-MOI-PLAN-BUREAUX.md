# Plan des bureaux — application Windows

`PlanBureaux.exe` affiche le plan des 28 postes et permet d'attribuer chaque
bureau. L'exécutable ne dépend de rien : ni Python, ni installation.

## Utilisation

1. Copiez `PlanBureaux.exe` où vous voulez (Bureau, clé USB, dossier partagé).
2. Double-cliquez dessus. Une petite fenêtre noire s'ouvre et le plan
   s'affiche dans votre navigateur.
3. Laissez cette fenêtre ouverte tant que vous utilisez le plan. Pour quitter,
   fermez-la.

Au premier lancement, Windows peut afficher **« Windows a protégé votre
ordinateur »** : c'est l'avertissement SmartScreen pour tout programme sans
signature payante. Cliquez sur *Informations complémentaires* puis
*Exécuter quand même*.

## Où sont enregistrées les données

Dans `plan-bureaux.json`, créé à côté de `PlanBureaux.exe` (ou dans
`%APPDATA%\PlanBureaux\` si le dossier est en lecture seule). La version
précédente est conservée dans `plan-bureaux.json.bak`.

Pour partager les attributions avec vos collègues, posez l'exe **et** le
fichier `.json` dans un dossier réseau commun : tout le monde verra les mêmes
postes. Les modifications simultanées ne sont pas fusionnées — la dernière
enregistrée gagne.

Le serveur écoute uniquement sur `127.0.0.1` : rien n'est exposé sur le réseau
de l'entreprise, et aucune donnée ne sort de votre machine.

## Recompiler

`plan-bureaux.html` est la source unique de l'interface : la même page
fonctionne seule dans un navigateur (les données vont alors dans le navigateur)
ou servie par l'exe (les données vont dans le fichier JSON).

```bash
./build-exe.sh        # nécessite Go ; produit PlanBureaux.exe
```
