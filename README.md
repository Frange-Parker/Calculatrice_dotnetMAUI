# Calculatrice mobile .NET MAUI

Calculatrice mobile (mode simple et scientifique, portrait et paysage).

## Fonctionnalités
- Opérations de base, nombres décimaux, AC, ⌫, ±(changement de signe), %, division par zéro gérée
- Mode scientifique : trigonométrie(cos, sin, tan et leurs inserves), ln, lg, √, ^, !, 1/x, π, e(constante d'Euler), deg/rad/grad

## Layouts utilisés
-	Grid : répartit l'écran en zones (en-tête, affichage, clavier) et organise les touches en lignes et colonnes régulières.
-	HorizontalStackLayout : aligne côte à côte les boutons de l'en-tête.
-	VerticalStackLayout : empile l'opération au-dessus du résultat.
-	ScrollView : empêche les longues expressions de déborder grâce au défilement horizontal.


## Lancer le projet

### Prérequis
- Visual Studio 2026 (ou 2022 à jour) avec la charge de travail **« Développement d'applications mobiles avec .NET (MAUI) »**, ou le SDK .NET 10 en ligne de commande
- Cloner le dépôt, puis ouvrir  le dossier `calculatrice`

```bash
dotnet --version                # Vérifie que le SDK .NET 10 est installé
dotnet workload install maui    # Installe MAUI (seulement si vous n'utilisez pas Visual Studio)
```

### Cas 1 : Émulateur Android
1. Visual Studio : menu **Outils > Android > Gestionnaire d'appareils Android**, créer un appareil virtuel
2. Le sélectionner dans la barre d'outils (liste déroulante à côté du bouton ▶), puis **F5**

Nécessite la virtualisation activée dans le BIOS et Hyper-V (Windows) ou HAXM/KVM. Si l'émulateur ne démarre pas, passer au cas 2.

### Cas 2 : Téléphone Android réel
1. **Activer le mode développeur** : Paramètres > À propos du téléphone > appuyer 7 fois sur « Numéro de build »
2. **Activer le débogage USB** : Paramètres > Options pour les développeurs > Débogage USB
3. Brancher le téléphone en USB (câble qui transmet les données, pas seulement la charge)
4. Accepter la fenêtre « Autoriser le débogage USB ? » sur le téléphone
5. Vérifier que le téléphone est détecté :
6. Sélectionner le téléphone dans la liste déroulante de Visual Studio, puis **F5**


### Cas 3 : Installer un APK sur un téléphone (sans PC relié)

```bash
# Génère un APK autonome 
dotnet publish calculatrice/calculatrice.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

Envoyer le fichier `.apk` (dans `bin/Release/net10.0-android/publish/`) sur le téléphone, puis l'ouvrir en autorisant « l'installation d'applications de sources inconnues ».

### Cas 4 : Windows (aucun téléphone ni émulateur)
Sous Windows, choisir la cible **« Machine Windows »** dans Visual Studio, puis **F5**.

## Livrables
- Rapport : docs/rapport_avancement.pdf
- Vidéo : video/presentation



##
Réalisé par NOLACK KAWUNJIBI Frange Parker GL5 ENSPD 2026-2027
##