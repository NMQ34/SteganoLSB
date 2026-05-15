using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace C__projet
{
    public partial class MainWindow : Window
    {

        private string? _cheminImageSelectionnee;
        private string? _cheminImageADecoder;
        public MainWindow()
        {

            InitializeComponent();

            this.FindControl<Button>("BtnOuvrirDecodage").Click += SelectionnerImageADecoder;
            this.FindControl<Button>("BtnLancerDecodage").Click += AuClicLancerDecodage;
            // On lie nos boutons à des fonctions C#
            this.FindControl<Button>("BtnOuvrirEncodage").Click += SelectionnerImageSource;

            this.FindControl<Button>("BtnLancerEncodage").Click += AuClicLancerEncodage;
        }

        // Fonction pour ouvrir l'explorateur de fichiers
        private async void SelectionnerImageSource(object? sender, RoutedEventArgs e)
        {
            // 1. On accède au gestionnaire de stockage du système
            var storage = this.StorageProvider;

            // 2. On ouvre la fenêtre de sélection de fichier
            var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choisir une image source",
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
                AllowMultiple = false
            });

            // 3. Si l'utilisateur a bien choisi un fichier
            if (result.Count > 0)
            {

                _cheminImageSelectionnee = result[0].Path.LocalPath;
                // 1. On récupère le flux (le contenu) du fichier
                using var stream = await result[0].OpenReadAsync();

                // 2. On crée un objet Bitmap (une image compréhensible par C#)
                var bitmap = new Bitmap(stream);

                // 3. On l'affiche dans notre composant Image du XAML
                var preview = this.FindControl<Avalonia.Controls.Image>("ImgPreview");
                if (preview != null)
                {
                    preview.Source = bitmap;
                }
            }
        }

        private void AuClicLancerEncodage(object? sender, RoutedEventArgs e)
        {
            var input = this.FindControl<TextBox>("InputMessage");
            string message = input?.Text ?? "";

            if (string.IsNullOrEmpty(_cheminImageSelectionnee) || string.IsNullOrEmpty(message))
            {
                // On pourrait ajouter une alerte ici : "Veuillez choisir une image et un message"
                return;
            }

            // On lance ton algorithme !
            ExecuterEncodage(_cheminImageSelectionnee, message);
        }

        private void ExecuterEncodage(string cheminImage, string messageSecret)
        {
            // 1. Charger l'image
            using SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(cheminImage);

            // 2. Convertir le message en bits (0 et 1)
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageSecret);

            int bitIndex = 0;
            int totalBits = messageBytes.Length * 8;

            // 3. Boucler sur les pixels
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (bitIndex < totalBits)
                    {
                        // On récupère le pixel actuel
                        Rgba32 pixel = image[x, y];

                        // On extrait le bit qu'on veut cacher (0 ou 1)
                        int byteIdx = bitIndex / 8;
                        int bitShift = 7 - (bitIndex % 8);
                        int bitACacher = (messageBytes[byteIdx] >> bitShift) & 1;

                        // --- MAGIE DU LSB ---
                        // On met le dernier bit du ROUGE à 0, puis on injecte notre bit
                        pixel.R = (byte)((pixel.R & 0xFE) | bitACacher);
                        // --------------------

                        // On réenregistre le pixel modifié
                        image[x, y] = pixel;
                        bitIndex++;
                    }
                }
            }
            // 4. Sauvegarder l'image finale en PNG (obligatoire pour ne pas perdre les bits !)
            image.Save("image_cachee.png");
        }

        private async void SelectionnerImageADecoder(object? sender, RoutedEventArgs e)
        {
            var storage = this.StorageProvider;
            var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Ouvrir l'image à décoder",
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            if (result.Count > 0)
            {
                _cheminImageADecoder = result[0].Path.LocalPath;
                // Optionnel : tu peux aussi afficher cette image dans une preview côté décodage
            }
        }

        // 2. Déclenchement du décodage
        private void AuClicLancerDecodage(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_cheminImageADecoder)) return;

            string messageExtrait = ExecuterDecodage(_cheminImageADecoder);

            var output = this.FindControl<TextBox>("TxtResultat");
            if (output != null) output.Text = messageExtrait;
        }

        // 3. L'ALGORITHME DE DÉCODAGE LSB
        private string ExecuterDecodage(string cheminImage)
        {
            using SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(cheminImage); List<byte> messageBytes = new List<byte>();
            byte currentByte = 0;
            int bitCount = 0;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba32 pixel = image[x, y];

                    // On extrait le dernier bit du rouge (0 ou 1)
                    int bit = pixel.R & 1;

                    // On décale l'octet en cours vers la gauche et on ajoute le bit
                    currentByte = (byte)((currentByte << 1) | bit);
                    bitCount++;

                    // Dès qu'on a 8 bits, on a un caractère complet
                    if (bitCount == 8)
                    {
                        // CONDITION D'ARRÊT : En LSB, on s'arrête souvent sur un caractère nul (0)
                        // ou on définit une longueur. Ici, on va tout lire, mais attention aux caractères bizarres à la fin.
                        if (currentByte == 0) return Encoding.UTF8.GetString(messageBytes.ToArray());

                        messageBytes.Add(currentByte);
                        currentByte = 0;
                        bitCount = 0;
                    }
                }
            }
            return Encoding.UTF8.GetString(messageBytes.ToArray());
        }
    }
}