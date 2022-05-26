using PluginManager.Others;
using System.Collections.Generic;

using System;
using System.IO;
using System.Threading.Tasks;

namespace PluginManager.LanguageSystem
{
    public class Language
    {
        /// <summary>
        /// The active language
        /// </summary>
        public static Language? ActiveLanguage = null;

        private static readonly string LanguageFileExtension = ".lng";

        /// <summary>
        /// The name of the language
        /// </summary>
        public string LanguageName { get; }

        /// <summary>
        /// The file where the language is imported from
        /// </summary>
        public string fileName { get; }

        /// <summary>
        /// The dictionary of the language
        /// </summary>
        public Dictionary<string, string> LanguageWords { get; }

        /// <summary>
        /// The Language constructor
        /// </summary>
        /// <param name="fileName">The file to import the language from</param>
        /// <param name="words">The dictionary of the language</param>
        /// <param name="LanguageName">The name of the language</param>
        private Language(string fileName, Dictionary<string, string> words, string LanguageName)
        {
            this.fileName = fileName;
            this.LanguageName = LanguageName;
            LanguageWords = words;
        }

        /// <summary>
        /// Load language from file
        /// </summary>
        /// <param name="LanguageFileLocation">The file path</param>
        /// <returns></returns>
        public static Language? CreateLanguageFromFile(string LanguageFileLocation)
        {
            if (!LanguageFileLocation.EndsWith(LanguageFileExtension))
            {
                Console.WriteLine("Failed to load language from file: " + LanguageFileLocation +
                                "\nFile extension is not " + LanguageFileExtension);
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

        /// <summary>
        /// Format text by inserting parameters
        /// </summary>
        /// <param name="text">The raw text</param>
        /// <param name="args">The arguments</param>
        /// <returns></returns>
        public string FormatText(string text, params string[] args)
        {
            if (ActiveLanguage == null) return text;
            int l = args.Length;
            for (var i = 0; i < l; i++) text = text.Replace($"{i}", args[i]);
            return text;
        }


        public static bool LoadLanguage()
        {
            string folder = Functions.langFolder;
            string langSettings = "./Data/Resources/Language.txt";
            if (!File.Exists(langSettings))
                File.WriteAllText(langSettings, "Language=English");
            //Load language from the specified file ...
            Language.ActiveLanguage = null;

            string langname = Functions.readCodeFromFile(langSettings, "Language", '=');
            if (langname == "English")
            {
                Language.ActiveLanguage = null;
                return true;
            }
            foreach (var file in Directory.GetFiles(folder))
            {
                if (Functions.readCodeFromFile(file, "LANGUAGE_NAME", '=') == langname)
                {
                    Language.ActiveLanguage = Language.CreateLanguageFromFile(file);

                    return true;
                }
            }

            if (Language.ActiveLanguage == null)
            {
                File.WriteAllText(langSettings, "Language=English");
                Console_Utilities.WriteColorText($"Failed to find language &r{langname} &c! Check available languages using command: &glistlang");

                return false;
            }

            return false;
        }

        public static void SetLanguage(string LanguageName)
        {
            string langSettings = Functions.dataFolder + "Language.txt";
            File.WriteAllText(langSettings, "Language=" + LanguageName);

            try
            {
                bool success = LoadLanguage();
                if (success)
                {
                    Console_Utilities.WriteColorText($"Language has been setted to: &g{LanguageName}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console_Utilities.WriteColorText($"Could not find language &r{LanguageName}.");
                Functions.WriteErrFile(ex.ToString());
                File.WriteAllText(langSettings, "Language=English");
                LoadLanguage();
            }
        }


    }
}