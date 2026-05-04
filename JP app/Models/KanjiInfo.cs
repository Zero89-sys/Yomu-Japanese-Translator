using SQLite;
using System.Windows.Input;

namespace JP_app.Models
{
    [Table("kanji")]
    public class KanjiInfo
    {
        [PrimaryKey]
        // Detailed information about an individual kanji character
        public string Character {  get; set; } = string.Empty;
        public string Reading { get; set; } = string.Empty;
        public string Meaning {  get; set; } = string.Empty;
        public string Onyomi { get; set; } = string.Empty;
        public string Kunyomi {  get; set; } = string.Empty;
    }

    [Table("words")]
    public partial class WordInfo : ObservableObject
    {
        // Dictionary entry (JMdict)
        [PrimaryKey, AutoIncrement]
        public int Id {  get; set; }
        public string KanjiText { get; set; }
        public string Reading { get; set; }
        public string Meaning { get; set; }


        // Kanji to speech
        [RelayCommand]
        private async Task SpeakWordAsync()
        {
            await SpeakAsync(KanjiText);
        }

        // Text to speech
        private async Task SpeakAsync(string text)
        {
            try
            {
                // Retrieve languages ​​supported by the device
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var japaneseLocale = locales.FirstOrDefault(l => l.Language.Contains("ja", StringComparison.OrdinalIgnoreCase));

                SpeechOptions options = new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = japaneseLocale // If null, it will use the default language
                };

                await TextToSpeech.Default.SpeakAsync(text, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
            }
        }
        [ObservableProperty]
        private bool isSpeaking;
        // Command to play sound and start Lottie animation
        public ICommand SpeakCommand => new Command(async () =>
        {
            if (IsSpeaking) return;
            IsSpeaking = true;

            try
            {
                await SpeakAsync(KanjiText);
            }
            finally
            {
                IsSpeaking = false;
            }
        });

        // Copies the kanji word to the system clipboard.
        [RelayCommand]
        async Task CopyKanji()
        {
            if (string.IsNullOrEmpty(KanjiText)) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(KanjiText);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Page error: {ex.Message}");
                }
            });
        }
        // Copies the reading of the word to the system clipboard.
        [RelayCommand]
        async Task CopyReading()
        {
            if (string.IsNullOrEmpty(Reading)) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(Reading);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Page error: {ex.Message}");
                }
            });
        }
        // Copies the meaning of the word to the system clipboard.
        [RelayCommand]
        async Task CopyMeaning()
        {
            if (string.IsNullOrEmpty(Meaning)) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(Meaning);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Page error: {ex.Message}");
                }
            });
        }
    }

    [Table("sentences")]
    public partial class SentenceInfo
    {
        // A pair of Japanese sentences and their English translation (Tatoeba)
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Japanese { get; set; } = string.Empty;
        public string English { get; set; } = string.Empty;
    }
}
