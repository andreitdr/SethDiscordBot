using PluginManager.Others;
using System.Collections.Generic;

using System;
using System.IO;

namespace PluginManager.LanguageSystem
{
    public class Language
    {
        public static Language? ActiveLanguage = null;

        private static readonly string LanguageFileExtension = ".lng";

        private Language(string fileName, Dictionary<string, string> words, string LanguageName)
        {
            this.fileName = fileName;
            this.LanguageName = LanguageName;
            LanguageWords = words;
        }

        public string LanguageName { get; }

        public string fileName { get; }

        public Dictionary<string, string> LanguageWords { get; }

        public static Language? CreateLanguageFromFile(string LanguageFileLocation)
        {
            if (!LanguageFileLocation.EndsWith(LanguageFileExtension))
            {
                Console.WriteLine("Failed to load Language from file: " + LanguageFileLocation +
                                "\nFile extension is not .lng");
                return null;
            }

            string[] lines = File.ReadAllLines(LanguageFileLocation);
            var languageName = "Unknown";
            var words = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                if (line.StartsWith("#") || line.Length < 4)
                    continue;
                string[] sLine = line.Split('=');

                if (sLine[0] == "LANGUAGE_NAME")
                {
                    languageName = sLine[1];
                    continue;
                }

                words.Add(sLine[0], sLine[1]);
            }

            Functions.WriteLogFile("Successfully loaded language: " + languageName + " from file : " +
                                   LanguageFileLocation.Replace('\\', '/'));
            return new Language(LanguageFileLocation, words, languageName);
        }

        public string FormatText(string text, params string[] args)
        {
            if (ActiveLanguage == null) return text;
            int l = args.Length;
            for (var i = 0; i < l; i++) text = text.Replace($"{i}", args[i]);
            return text;
        }
    }
}