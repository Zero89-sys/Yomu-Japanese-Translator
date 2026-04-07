using JP_app.Models;
using NMeCab.Specialized;
using SQLite;


namespace JP_app.Services
{
    // Represents the result of morphological analysis of a single word
    public class TokenResult
    {
        public string Surface { get; set; } = string.Empty; // Word form directly in the sentence
        public string BaseForm { get; set; } = string.Empty; // Basic (dictionary) form
        public string Reading { get; set; } = string.Empty; // Reading
        public string PartOfSpeech { get; set; } = string.Empty; // Part of speech
    }

    public class KanjiService
    {
        private SQLiteAsyncConnection _db;
        private readonly string _kanjiPath;
        private readonly string _dictionaryPath;
        private readonly string _sentencesPath;


        // Initializes connection to main database and connects external DB modules
        public KanjiService(string kanjiPath, string dictionaryPath, string sentencesPath)
        {
            this._kanjiPath = kanjiPath;
            this._dictionaryPath = dictionaryPath;
            this._sentencesPath = sentencesPath;

            _db = new SQLiteAsyncConnection(kanjiPath);


            // ATTACH allows you to query across multiple database files
            _db.ExecuteAsync($"ATTACH DATABASE '{dictionaryPath}' AS dict").GetAwaiter().GetResult();
            _db.ExecuteAsync($"ATTACH DATABASE '{sentencesPath}' AS sentences").GetAwaiter().GetResult();
        }

        // Finds an exact match for a word in the JMdict dictionary.
        public async Task<List<KanjiInfo>> GetMultipleKanjiDetails(string text)
        {
            // REGEX for Kanji range in Unicode
            var kanjiChars = System.Text.RegularExpressions.Regex.Matches(text, @"[\u4e00-\u9faf]")
                                                                .Select(m => m.Value)
                                                                .Distinct()
                                                                .ToList();

            if (!kanjiChars.Any())
            {
                return new List<KanjiInfo>();
            }
           // Find details for all found kanji at once
           return await _db.Table<KanjiInfo>()
                .Where(x => kanjiChars.Contains(x.Character))
                .ToListAsync();
        }

        // Finds an exact match for a word in JMdict
        public async Task<WordInfo> GetExactWordAsync(string word)
        {
            // Match either in KanjiText or in Reading
            var query = "SELECT * FROM dict.words WHERE KanjiText = ? OR Reading = ? LIMIT 1";

            var results = await _db.QueryAsync<WordInfo>(query, word, word);

            return results.FirstOrDefault();
        }

        //Performs morphological analysis of the sentence using the NMeCab library and returns a list of tokens.
        public List<TokenResult> TokenizeSentence(string sentence)
        {
            var results = new List<TokenResult>();

            // Path to the folder with the MeCab dictionary
            string dicPath = Path.Combine(FileSystem.AppDataDirectory, "dic", "ipadic");

            using var tagger = MeCabIpaDicTagger.Create();
            var nodes = tagger.Parse(sentence);

            foreach(var node in nodes)
            {
                if (string.IsNullOrEmpty(node.Surface)) continue;

                if (node.PartsOfSpeech.Contains("記号")) continue; // We ignore punctuation

                var features = node.Feature?.Split(',');
                string baseForm = (features != null && features.Length > 6 && features[6] != "*")
                ? features[6]
                : node.Surface;


                // Extract the basic shape (index 6 in IPADIC)
                string reading = (features != null && features.Length > 7 && features[7] != "*")
                    ? features[7]
                    : node.Surface;

                if (string.IsNullOrEmpty(baseForm) || baseForm == "*")
                    baseForm = node.Surface;

                results.Add(new TokenResult
                {
                    Surface = node.Surface,
                    BaseForm = baseForm,
                    Reading = node.Reading ?? node.Surface,
                    PartOfSpeech = node.PartsOfSpeech
                });
            }
            return results;
        }

        // Searches the Tatoeba database for sentences most similar to the given input.
        public async Task<List<SentenceInfo>> FindBestMatchesAsync(string input)
        {
            // Searching for sentences that contain the specified string.
            var query = "SELECT * FROM sentences.sentences WHERE Japanese LIKE ? ORDER BY LENGTH(Japanese) ASC LIMIT 3";

            return await _db.QueryAsync<SentenceInfo>(query, $"%{input}%");
        }
    }
}
