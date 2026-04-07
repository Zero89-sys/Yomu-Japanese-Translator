using JP_app.Models;
using JP_app.Services;


namespace JP_app.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly KanjiService _kanjiService;
    public bool HasTranslation => MainTranslation != null;

    // User's raw text input
    [ObservableProperty]
    private string _inputText;

    // Details of a single kanji character
    [ObservableProperty]
    private KanjiInfo _result;

    // Collection of words retrieved from JMdict matching the input tokens
    [ObservableProperty]
    private List<WordInfo> _relatedWords;

    // Example sentences retrieved from the Tatoeba database
    [ObservableProperty]
    private List<SentenceInfo> _relatedSentences;

    // List of all individual kanji characters identified within the input text
    [ObservableProperty]
    private List<KanjiInfo> _kanjiResult;

    // For future translation
    [ObservableProperty]
    private string _sentenceTranslation;

    // The primary translation match found in the database
    [ObservableProperty]
    private SentenceInfo _mainTranslation;

    // Constructor with Dependency Injection for accessing the database service
    public MainViewModel (KanjiService kanjiService)
    {
        _kanjiService = kanjiService;
    }

    [RelayCommand]
    // Analyzes the input text using morphological analysis and database lookups
    private async Task Analyze()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        string input = InputText.Trim();

        // Range: \u3040-\u309F (Hiragana), \u30A0-\u30FF (Katakana), \u4E00-\u9FAF (Kanji)
        bool containsJapanese = System.Text.RegularExpressions.Regex.IsMatch(input, @"[\u3040-\u30FF\u4E00-\u9FAF]");

        // If it's not Japanese,  clean everything up and quit
        if (!containsJapanese)
        {
            MainTranslation = null;
            RelatedWords = null;
            KanjiResult = null;
            return;
        }

        // Find the best match for the WHOLE sentence
        var matches = await _kanjiService.FindBestMatchesAsync(input);

        if (matches != null && matches.Any())
        {
            // If there is a match, we take the shortest (most accurate) one as the main translation
            MainTranslation = matches.First();
        }
        else
        {
            // Fallback: If no exact match, search based on the first noun found in the input
            var token = _kanjiService.TokenizeSentence(input);
            var firstNoun = token.FirstOrDefault(t => t.PartOfSpeech.Contains("名詞"));

            if (firstNoun != null)
            {
                var fallBackMatches = await _kanjiService.FindBestMatchesAsync(firstNoun.Surface);
                MainTranslation = fallBackMatches.FirstOrDefault();
            }
            else
            {
                MainTranslation = null;
            }
        }

        // Morphological Analysis of a sentence into words using NMeCab
        var tokens = _kanjiService.TokenizeSentence(InputText);

        // Searching for words in JMdict based on tokens
        var foundWords = new List<WordInfo>();
        foreach(var token in tokens)
        {
            // Try matching the dictionary form
            var match = await _kanjiService.GetExactWordAsync(token.BaseForm);
            if (match == null && token.Surface != token.BaseForm)
            {
                match = await _kanjiService.GetExactWordAsync(token.Surface);
            }
            if(match != null)
            {
                foundWords.Add(match);
            }
        }

        OnPropertyChanged(nameof(HasTranslation));
        // Getting rid of of duplicates
        RelatedWords = foundWords.GroupBy(w => w.KanjiText).Select(g => g.First()).ToList();

        // Extraction and details of Kanji
        KanjiResult = await _kanjiService.GetMultipleKanjiDetails(InputText);
    }
}
