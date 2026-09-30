using System.Globalization; // Nécessaire pour formater/lire les nombres avec le point décimal, quelle que soit la langue du téléphone

namespace calculatrice; // Espace de noms du projet

public partial class MainPage : ContentPage // Classe de la page (partie code de MainPage.xaml)
{
    private string _currentExpression = ""; // Expression saisie par l'utilisateur (ex : "2+3×4")
    private List<string> _history = new List<string>(); // Liste des calculs déjà effectués

    private bool _is2ndActive = false; // Vrai si le bouton "2nd" est activé (sin -> asin, etc.)
    private int _angleMode = 0; // Unité d'angle : 0 = degrés, 1 = radians, 2 = grades

    // Vrai juste après un "=" ou au démarrage : la prochaine saisie repart de zéro (sauf opérateur)
    private bool _isNewCalculation = true;

    private bool _isScientificMode = false; // Choix de l'utilisateur en portrait (false = clavier simple)
    private bool? _lastLandscape = null; // Dernière orientation connue (null = jamais calculée)
    private bool _isLandscape = false; // Vrai si l'écran est actuellement en paysage

    // Liste des fonctions reconnues par le parseur (les plus longues d'abord)
    private static readonly string[] FunctionNames = { "asin", "acos", "atan", "sin", "cos", "tan", "ln", "lg", "√" };

    public MainPage()
    {
        InitializeComponent(); // Charge le XAML et relie les x:Name aux champs C#
    }

    // ==========================================
    // GESTION DE L'ORIENTATION (PORTRAIT / PAYSAGE)
    // ==========================================

    // Appelée automatiquement par MAUI à chaque changement de taille de la page
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height); // Comportement standard de MAUI

        if (width <= 0 || height <= 0) return; // Dimensions invalides : on ne fait rien

        bool landscape = width > height; // Paysage si la largeur dépasse la hauteur

        if (_lastLandscape == landscape) return; // L'orientation n'a pas changé : on évite de tout réorganiser

        _lastLandscape = landscape; // On mémorise la nouvelle orientation
        _isLandscape = landscape; // Mise à jour du champ utilisé ailleurs

        ApplyLayout(landscape); // Réorganise l'écran (lignes/colonnes)
        ApplyKeypadVisibility(); // Choisit le bon clavier
        AdjustFontSize(); // Adapte la taille des textes
    }

    // Réorganise la Grid racine selon l'orientation
    private void ApplyLayout(bool landscape)
    {
        RootGrid.RowDefinitions.Clear(); // On vide les lignes existantes
        RootGrid.ColumnDefinitions.Clear(); // On vide les colonnes existantes

        if (landscape)
        {
            // PAYSAGE : 2 colonnes (affichage 1/3 à gauche, clavier 2/3 à droite)
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Ligne de l'en-tête
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Ligne de l'affichage

            Grid.SetRow(HeaderLayout, 0); Grid.SetColumn(HeaderLayout, 0); // En-tête : haut gauche
            Grid.SetRow(DisplayLayout, 1); Grid.SetColumn(DisplayLayout, 0); // Affichage : bas gauche
            Grid.SetRow(KeypadContainer, 0); Grid.SetColumn(KeypadContainer, 1); // Clavier : colonne de droite
            Grid.SetRowSpan(KeypadContainer, 2); // Le clavier occupe toute la hauteur
        }
        else
        {
            // PORTRAIT : 1 colonne, 3 lignes (en-tête / affichage / clavier)
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // En-tête
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star))); // Affichage : 1 part de l'espace restant
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(2, GridUnitType.Star))); // Clavier : 2 parts (les touches s'étirent sur toute la hauteur)

            Grid.SetRow(HeaderLayout, 0); Grid.SetColumn(HeaderLayout, 0); // En-tête : ligne 0
            Grid.SetRow(DisplayLayout, 1); Grid.SetColumn(DisplayLayout, 0); // Affichage : ligne 1
            Grid.SetRow(KeypadContainer, 2); Grid.SetColumn(KeypadContainer, 0); // Clavier : ligne 2
            Grid.SetRowSpan(KeypadContainer, 1); // Le clavier n'occupe qu'une ligne
        }
    }

    // Affiche le bon clavier : scientifique en paysage, sinon selon le choix de l'utilisateur
    private void ApplyKeypadVisibility()
    {
        bool showScientific = _isLandscape || _isScientificMode; // Faut-il afficher le clavier scientifique ?
        SimpleKeypad.IsVisible = !showScientific; // Clavier simple visible dans le cas contraire
        ScientificKeypad.IsVisible = showScientific; // Clavier scientifique visible si nécessaire
        BtnToggle.IsVisible = !_isLandscape; // Le bouton de bascule est inutile en paysage
    }

    // ==========================================
    // ÉVÉNEMENTS DES BOUTONS DE SAISIE
    // ==========================================

    // Appelée par les chiffres, opérateurs et fonctions (tout ce qui s'ajoute simplement à l'expression)
    private void OnInputClicked(object sender, EventArgs e)
    {
        Button btn = (Button)sender; // Le bouton qui a déclenché l'événement
        string input = btn.Text; // Son texte (ex : "7", "+", "sin")

        // Opérateurs qui doivent continuer avec le résultat précédent si on les tape juste après "="
        bool isOperator = input == "+" || input == "-" || input == "×" || input == "÷" || input == "^" || input == "!" || input == "%";

        if (_isNewCalculation) // On vient de finir un calcul (ou de démarrer)
        {
            if (ResultLabel.Text.StartsWith("Erreur")) // Après une erreur, on repart de zéro
            {
                _currentExpression = "";
            }
            else if (isOperator && ResultLabel.Text != "0") // Opérateur : on réutilise le résultat
            {
                _currentExpression = ResultLabel.Text;
            }
            else // Chiffre ou fonction : nouvelle saisie
            {
                _currentExpression = "";
            }
            _isNewCalculation = false; // On n'est plus en début de calcul
        }

        if (_currentExpression == "" && input == ".") // Point tapé en premier
        {
            _currentExpression = "0."; // On affiche "0." pour rester lisible
        }
        else if (_currentExpression == "0" && !isOperator && input != ".") // "0" seul + chiffre
        {
            _currentExpression = input; // On remplace le zéro (évite "05")
        }
        else
        {
            _currentExpression += input; // Cas général : on ajoute le caractère
        }

        ResultLabel.Text = _currentExpression; // Mise à jour de l'affichage
        AdjustFontSize(); // Ajuste la taille du texte
    }

    // Touche ± : change le signe de l'expression entière
    private void OnSignClicked(object sender, EventArgs e)
    {
        if (_isNewCalculation) // Juste après "=" ou au démarrage
        {
            if (ResultLabel.Text.StartsWith("Erreur") || ResultLabel.Text == "0") return; // Rien à inverser
            _currentExpression = ResultLabel.Text; // On travaille sur le résultat affiché
            _isNewCalculation = false; // On reprend l'édition
        }

        if (string.IsNullOrEmpty(_currentExpression)) return; // Expression vide : rien à faire

        if (IsWrappedByNegation(_currentExpression)) // Déjà de la forme "-( ... )"
        {
            // On retire le "-(" au début et le ")" à la fin pour repasser en positif
            _currentExpression = _currentExpression.Substring(2, _currentExpression.Length - 3);
        }
        else
        {
            _currentExpression = "-(" + _currentExpression + ")"; // Sinon on enveloppe avec un signe moins
        }

        ResultLabel.Text = _currentExpression; // Mise à jour de l'affichage
        AdjustFontSize(); // Ajuste la taille du texte
    }

    // Vérifie que l'expression est exactement "-( ... )" (la parenthèse ouvrante se ferme bien à la toute fin)
    private bool IsWrappedByNegation(string expr)
    {
        if (!expr.StartsWith("-(") || !expr.EndsWith(")")) return false; // Mauvaise forme de départ

        int depth = 0; // Profondeur de parenthèses
        for (int i = 1; i < expr.Length; i++) // On part de la parenthèse ouvrante (index 1)
        {
            if (expr[i] == '(') depth++; // Ouverture : on descend d'un niveau
            else if (expr[i] == ')') depth--; // Fermeture : on remonte d'un niveau

            if (depth == 0 && i < expr.Length - 1) return false; // Fermée trop tôt (ex : "-(2)+(3)")
        }
        return depth == 0; // Vrai si tout est équilibré
    }

    // Touche 1/x : remplace l'expression par 1÷(expression)
    private void OnInverseClicked(object sender, EventArgs e)
    {
        if (_isNewCalculation) // Juste après "=" ou au démarrage
        {
            if (ResultLabel.Text.StartsWith("Erreur") || ResultLabel.Text == "0") return; // Rien à inverser (0 donnerait une erreur)
            _currentExpression = ResultLabel.Text; // On inverse le résultat affiché
            _isNewCalculation = false; // On reprend l'édition
        }

        if (string.IsNullOrEmpty(_currentExpression)) return; // Expression vide : rien à faire

        _currentExpression = "1÷(" + _currentExpression + ")"; // Enveloppe : évite les erreurs comme "51/"
        ResultLabel.Text = _currentExpression; // Mise à jour de l'affichage
        AdjustFontSize(); // Ajuste la taille du texte
    }

    // Bascule entre clavier simple et scientifique (portrait uniquement)
    private void OnToggleModeClicked(object sender, EventArgs e)
    {
        _isScientificMode = !_isScientificMode; // On inverse le choix de l'utilisateur
        ApplyKeypadVisibility(); // On applique le choix
    }

    // Touche AC : remise à zéro totale
    private void OnClearClicked(object sender, EventArgs e)
    {
        _currentExpression = ""; // On vide l'expression
        ResultLabel.Text = "0"; // Affichage remis à 0
        OperationLabel.Text = ""; // On efface l'opération affichée au-dessus
        _isNewCalculation = true; // Prochain calcul = nouveau
        AdjustFontSize(); // Ajuste la taille du texte
    }

    // Touche ⌫ : efface le dernier caractère
    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (_isNewCalculation) // Juste après un résultat : on réinitialise l'écran
        {
            _currentExpression = "";
            ResultLabel.Text = "0";
            return;
        }

        if (_currentExpression.Length > 0) // S'il reste des caractères
        {
            _currentExpression = _currentExpression.Substring(0, _currentExpression.Length - 1); // On retire le dernier
            ResultLabel.Text = string.IsNullOrEmpty(_currentExpression) ? "0" : _currentExpression; // "0" si vide
        }
        AdjustFontSize(); // Ajuste la taille du texte
    }

    // Touche deg/rad/grad : change l'unité d'angle
    private void OnDegClicked(object sender, EventArgs e)
    {
        _angleMode = (_angleMode + 1) % 3; // 0 -> 1 -> 2 -> 0 ...
        if (_angleMode == 0) BtnDeg.Text = "deg"; // Degrés
        else if (_angleMode == 1) BtnDeg.Text = "rad"; // Radians
        else BtnDeg.Text = "grad"; // Grades
    }

    // Touche 2nd : bascule sin/cos/tan <-> asin/acos/atan
    private void On2ndClicked(object sender, EventArgs e)
    {
        _is2ndActive = !_is2ndActive; // On inverse l'état
        if (_is2ndActive)
        {
            Btn2nd.TextColor = GetColor("ColorFunctionActive"); // Couleur "actif" définie dans le XAML
            BtnSin.Text = "asin"; // Fonctions inverses
            BtnCos.Text = "acos";
            BtnTan.Text = "atan";
        }
        else
        {
            Btn2nd.TextColor = GetColor("ColorFunctionText"); // Couleur normale définie dans le XAML
            BtnSin.Text = "sin"; // Fonctions directes
            BtnCos.Text = "cos";
            BtnTan.Text = "tan";
        }
    }

    // Récupère une couleur définie dans les ressources XAML de la page
    private Color GetColor(string key)
    {
        return (Color)Resources[key]; // Cherche la clé dans le ResourceDictionary de la page
    }

    // Affiche l'historique dans une boîte de dialogue
    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        string historyText = _history.Count > 0 ? string.Join("\n\n", _history) : "Aucun historique."; // Texte à afficher
        await DisplayAlert("Historique", historyText, "Fermer"); // Boîte de dialogue avec un bouton "Fermer"
    }

    // ==========================================
    // LOGIQUE DE CALCUL
    // ==========================================

    // Touche = : évalue l'expression
    private void OnCalculateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentExpression) || ResultLabel.Text.StartsWith("Erreur"))
            return; // Rien à calculer

        try
        {
            OperationLabel.Text = _currentExpression + " ="; // Affiche l'opération au-dessus du résultat

            double result = Evaluate(_currentExpression); // Calcul via le parseur

            if (double.IsInfinity(result) || double.IsNaN(result)) // Division par zéro, ln(-1), etc.
            {
                ResultLabel.Text = "Erreur math"; // Message d'erreur, sans plantage
            }
            else
            {
                result = Math.Round(result, 10); // Supprime les erreurs d'arrondi (0.1+0.2 -> 0.3)
                // Format "0.##########" : jamais de notation scientifique (E+20), que le parseur ne saurait relire
                ResultLabel.Text = result.ToString("0.##########", CultureInfo.InvariantCulture);
                _history.Insert(0, $"{OperationLabel.Text} {ResultLabel.Text}"); // Ajout en tête d'historique
            }

            _currentExpression = ResultLabel.Text; // Le résultat devient l'expression courante
            _isNewCalculation = true; // Le prochain appui repart d'un nouveau calcul
        }
        catch (Exception) // Expression invalide (parenthèse mal placée, opérateur seul, ...)
        {
            ResultLabel.Text = "Erreur syntaxe"; // Message d'erreur, sans plantage
            _isNewCalculation = true;
        }

        AdjustFontSize(); // Ajuste la taille du texte
    }

    // Adapte la taille des textes à l'orientation et à la longueur affichée
    private void AdjustFontSize()
    {
        bool longText = ResultLabel.Text.Length > 10; // Texte long ?

        if (_isLandscape)
        {
            ResultLabel.FontSize = longText ? 24 : 38; // Plus petit en paysage (colonne étroite)
            OperationLabel.FontSize = 18;
        }
        else
        {
            ResultLabel.FontSize = longText ? 30 : 56; // Taille portrait
            OperationLabel.FontSize = 24;
        }
    }

    // =========================================================================
    // PARSEUR À DESCENTE RÉCURSIVE
    // Priorités : + - (plus faibles) < × ÷ < fonctions/puissances/! % (plus fortes)
    // =========================================================================
    private double Evaluate(string expression)
    {
        int pos = -1, ch; // pos = position courante, ch = caractère courant (-1 = fin)

        // Avance d'un caractère
        void NextChar() { ch = (++pos < expression.Length) ? expression[pos] : -1; }

        // Si le caractère courant (en ignorant les espaces) est charToEat, on le consomme
        bool Eat(int charToEat)
        {
            while (ch == ' ') NextChar(); // Ignore les espaces
            if (ch == charToEat) { NextChar(); return true; } // Consommé
            return false; // Non trouvé
        }

        // Point d'entrée : lit toute l'expression
        double Parse()
        {
            NextChar(); // Se place sur le premier caractère
            double x = ParseExpression(); // Analyse l'expression
            if (pos < expression.Length) throw new Exception("Erreur"); // Caractères restants = syntaxe invalide
            return x;
        }

        // Additions et soustractions
        double ParseExpression()
        {
            double x = ParseTerm(); // Premier terme
            for (;;)
            {
                if (Eat('+')) x += ParseTerm(); // Addition
                else if (Eat('-')) x -= ParseTerm(); // Soustraction
                else return x; // Plus d'opérateur : fin
            }
        }

        // Multiplications et divisions (y compris la multiplication implicite)
        double ParseTerm()
        {
            double x = ParseFactor(); // Premier facteur
            for (;;)
            {
                if (Eat('*') || Eat('×')) x *= ParseFactor(); // Multiplication
                else if (Eat('/') || Eat('÷')) x /= ParseFactor(); // Division (x/0 donne Infinity -> "Erreur math")
                // Multiplication implicite : "2sin(30)", "2π", "(2)(3)"
                // Multiplication implicite : "2sin(30)", "2π", "(2)(3)", et maintenant "e1", "π2", "(2)3"
                else if (ch == '(' || ch == 'π' || ch == 'e' || ch == '√' || (ch >= 'a' && ch <= 'z')
                        || (ch >= '0' && ch <= '9') || ch == '.') // Ajout : un chiffre ou un point après un facteur
                    x *= ParseFactor(); // Multiplie par le facteur suivant
                
                else return x;
            }
        }

        // Nombres, parenthèses, constantes, fonctions, puis opérateurs postfixes (^ ! %)
        double ParseFactor()
        {
            if (Eat('+')) return ParseFactor(); // Signe + unaire : "+5"
            if (Eat('-')) return -ParseFactor(); // Signe - unaire : "-5"

            double x = 0; // Valeur du facteur
            int startPos = pos; // Position de début (pour extraire les nombres)

            if (Eat('(')) // Parenthèse ouvrante
            {
                x = ParseExpression(); // Évalue l'intérieur
                Eat(')'); // Fermeture (facultative : "(2+3" est toléré)
            }
            else if (Eat('π')) x = Math.PI; // Constante π
            else if (Eat('e')) x = Math.E; // Constante e
            else if ((ch >= '0' && ch <= '9') || ch == '.') // Nombre
            {
                while ((ch >= '0' && ch <= '9') || ch == '.') NextChar(); // Lit tous les chiffres
                x = double.Parse(expression.Substring(startPos, pos - startPos), CultureInfo.InvariantCulture); // Convertit en double
            }
            else // Sinon : on attend une fonction connue
            {
                string func = null; // Nom de la fonction trouvée
                foreach (string name in FunctionNames) // On teste chaque nom connu à la position courante
                {
                    if (string.CompareOrdinal(expression, pos, name, 0, name.Length) == 0) { func = name; break; }
                }
                if (func == null) throw new Exception("Inconnu"); // Rien de reconnu : erreur de syntaxe

                for (int i = 0; i < func.Length; i++) NextChar(); // On consomme exactement le nom de la fonction

                x = ParseFactor(); // Argument de la fonction

                // Facteur de conversion selon l'unité d'angle (deg, rad ou grad)
                double angleFactor = _angleMode == 0 ? (Math.PI / 180.0) : (_angleMode == 2 ? (Math.PI / 200.0) : 1.0);

                if (func == "sin") x = Math.Sin(x * angleFactor); // Sinus
                else if (func == "cos") x = Math.Cos(x * angleFactor); // Cosinus
                else if (func == "tan") x = Math.Tan(x * angleFactor); // Tangente
                else if (func == "asin") x = Math.Asin(x) / angleFactor; // Arc sinus
                else if (func == "acos") x = Math.Acos(x) / angleFactor; // Arc cosinus
                else if (func == "atan") x = Math.Atan(x) / angleFactor; // Arc tangente
                else if (func == "√") x = Math.Sqrt(x); // Racine carrée
                else if (func == "ln") x = Math.Log(x); // Logarithme népérien
                else if (func == "lg") x = Math.Log10(x); // Logarithme décimal
            }

            // Opérateurs postfixes : puissance, factorielle, pourcentage
            bool checkPostfix = true;
            while (checkPostfix)
            {
                if (Eat('^')) x = Math.Pow(x, ParseFactor()); // Puissance
                else if (Eat('!')) x = Factorial(x); // Factorielle
                else if (Eat('%')) x = x / 100.0; // Pourcentage : 50% = 0.5
                else checkPostfix = false; // Plus d'opérateur postfixe
            }

            return x; // Valeur du facteur
        }

        return Parse(); // Lance l'analyse
    }

    // Calcule la factorielle (entiers positifs uniquement)
    private double Factorial(double num)
    {
        if (num < 0 || num % 1 != 0) throw new Exception("Math"); // Négatif ou décimal : invalide
        if (num > 170) return double.PositiveInfinity; // Au-delà de 170!, le double déborde -> "Erreur math"
        double result = 1; // Résultat initial
        for (int i = 1; i <= (int)num; i++) result *= i; // 1 × 2 × 3 × ... × num
        return result;
    }
}