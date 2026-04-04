using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class WordInfo
    {
        // Dictionary entry (JMdict)
        [PrimaryKey, AutoIncrement]
        public int Id {  get; set; }
        public string KanjiText { get; set; }
        public string Reading { get; set; }
        public string Meaning { get; set; }

    }

    [Table("sentences")]
    public class SentenceInfo
    {

        // A pair of Japanese sentences and their English translation (Tatoeba)
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Japanese { get; set; } = string.Empty;
        public string English { get; set; } = string.Empty;
    }
}
