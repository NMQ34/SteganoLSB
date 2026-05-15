# SteganoLSB - Outil de Stéganographie

Ce projet est une application de bureau développée en **C# avec Avalonia UI**. Il permet de dissimuler un message secret dans une image (encodage) et de le retrouver (décodage) en utilisant la technique du **LSB (Least Significant Bit)**.

## Prérequis et Installation
- **Framework** : .NET 9.0
- **IDE recommandé** : Visual Studio 2022 (ou Rider)
- **Dépendances** : Avalonia UI, SixLabors.ImageSharp, EntityFrameworkCore.

**Pour lancer le projet :**
1. Clonez ce dépôt sur votre machine.
2. Ouvrez le fichier `.slnx` (ou `.csproj`) avec Visual Studio.
3. Restaurez les packages NuGet si nécessaire.
4. Lancez la compilation via le bouton "Démarrer" ou tapez `dotnet run` dans le terminal.

## Parcours Utilisateur

L'application est divisée en 4 onglets simples pour une prise en main immédiate :
1. ** Accueil** : Présentation de l'outil et du principe de la méthode LSB.
2. ** Encodage** : 
   - Cliquez sur "Choisir une image source".
   - Tapez votre message secret dans la zone de texte.
   - Cliquez sur "Lancer l'encodage". L'image modifiée sera sauvegardée sous le nom `image_cachee.png` dans le dossier de l'application.
3. ** Décodage** : 
   - Sélectionnez l'image précédemment encodée (`image_cachee.png`).
   - Cliquez sur "Analyser les bits".
   - Le message secret s'affichera dans la zone de texte en dessous.
4. ** Paramètres** : Informations sur la version et options d'interface.

---

## Génération de l'exécutable (.exe)
Par souci de bonnes pratiques et pour garantir l'intégrité du dépôt, les fichiers binaires compilés ne sont pas inclus dans ce repo. Pour générer l'exécutable autonome pour Windows, utilisez la commande suivante :

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true