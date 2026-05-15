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
using Avalonia;

namespace C__projet
{
    public partial class MainWindow : Window
    {
        private string? _cheminImageSelectionnee;
        private string? _cheminImageADecoder;

        public MainWindow()
        {
            InitializeComponent();

            var toggleTheme = this.FindControl<ToggleSwitch>("ToggleTheme");
            if (toggleTheme != null)
            {
                toggleTheme.IsCheckedChanged += (sender, args) =>
                {
                    if (toggleTheme.IsChecked == true)
                    {
                        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    }
                    else
                    {
                        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                    }
                };
            }

            this.FindControl<Button>("BtnOuvrirDecodage")!.Click += SelectionnerImageADecoder;
            this.FindControl<Button>("BtnLancerDecodage")!.Click += AuClicLancerDecodage;
            this.FindControl<Button>("BtnOuvrirEncodage")!.Click += SelectionnerImageSource;
            this.FindControl<Button>("BtnLancerEncodage")!.Click += AuClicLancerEncodage;
        }

        private async void SelectionnerImageSource(object? sender, RoutedEventArgs e)
        {
            var storage = this.StorageProvider;
            var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choisir une image source",
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                _cheminImageSelectionnee = result[0].Path.LocalPath;

                using var stream = await result[0].OpenReadAsync();
                var bitmap = new Bitmap(stream);

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
                if (input != null) input.Text = "Erreur : Veuillez choisir une image et taper un message.";
                return;
            }

            ExecuterEncodage(_cheminImageSelectionnee, message);

            if (input != null) input.Text = "Chiffrement réussi ! L'image est sauvegardée sous 'image_cachee.png'.";
        }

        private void ExecuterEncodage(string cheminImage, string messageSecret)
        {
            using SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(cheminImage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageSecret + "\0");

            int bitIndex = 0;
            int totalBits = messageBytes.Length * 8;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (bitIndex < totalBits)
                    {
                        Rgba32 pixel = image[x, y];

                        int byteIdx = bitIndex / 8;
                        int bitShift = 7 - (bitIndex % 8);
                        int bitACacher = (messageBytes[byteIdx] >> bitShift) & 1;

                        // Injection du bit dans le bit de poids faible (LSB) du canal Rouge
                        pixel.R = (byte)((pixel.R & 0xFE) | bitACacher);

                        image[x, y] = pixel;
                        bitIndex++;
                    }
                }
            }

            // Sauvegarde au format PNG pour éviter la compression destructrice
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
            }
        }

        private void AuClicLancerDecodage(object? sender, RoutedEventArgs e)
        {
            var output = this.FindControl<TextBox>("TxtResultat");

            if (string.IsNullOrEmpty(_cheminImageADecoder))
            {
                if (output != null) output.Text = "⚠️ Erreur : Veuillez d'abord sélectionner une image.";
                return;
            }

            string messageExtrait = ExecuterDecodage(_cheminImageADecoder);

            if (output != null)
            {
                if (string.IsNullOrWhiteSpace(messageExtrait))
                {
                    output.Text = "Aucun message caché trouvé dans cette image.";
                }
                else
                {
                    output.Text = "Message découvert : \n" + messageExtrait;
                }
            }
        }

        private string ExecuterDecodage(string cheminImage)
        {
            using SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(cheminImage);
            List<byte> messageBytes = new List<byte>();
            byte currentByte = 0;
            int bitCount = 0;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba32 pixel = image[x, y];

                    // Extraction du LSB du canal Rouge
                    int bit = pixel.R & 1;
                    currentByte = (byte)((currentByte << 1) | bit);
                    bitCount++;

                    if (bitCount == 8)
                    {
                        // Condition d'arrêt sur caractère nul (Null Byte)
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