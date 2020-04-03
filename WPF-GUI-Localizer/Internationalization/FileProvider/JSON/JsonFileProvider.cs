using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Internationalization.Exception;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Newtonsoft.Json;

namespace Internationalization.FileProvider.JSON
{
    /// <summary>
    /// Saves its files using Json.NET.
    /// The managed dictionary gets directly saved in its deserialized form
    /// </summary>
    public class JsonFileProvider : IFileProvider
    {
        private readonly string _path;
        private Dictionary<CultureInfo, Dictionary<string, string>> _dictOfdicts;

        private bool successfullyCreatedFile;
        public ProviderStatus Status { get; private set; }

        /// <param name="translationFilePath">the path under which the dictionary will be saved</param>
        public JsonFileProvider(string translationFilePath)
        {
            Status = ProviderStatus.InitializationInProgress;

            _path = InspectPath(translationFilePath);

            ReadFile();
        }

        public void SaveDictionary()
        {
            try
            {
                File.WriteAllText(_path, JsonConvert.SerializeObject(_dictOfdicts));
            }
            catch
            {
                Console.WriteLine($@"Unable to write langage file ({Path.GetFullPath(_path)}).");
            }
        }

        public void Update(string key, IEnumerable<TextLocalization> texts)
        {
            foreach (TextLocalization textLocalization in texts)
            {
                _dictOfdicts.TryGetValue(textLocalization.Language, out Dictionary<string, string> langDict);
                if (langDict == null)
                {
                    langDict = new Dictionary<string, string>();
                    _dictOfdicts.Add(textLocalization.Language, langDict);
                }

                if (langDict.ContainsKey(key))
                {
                    langDict.Remove(key);
                }
                langDict.Add(key, textLocalization.Text);
            }

            //if file was created by JsonFileProvider itself
            if (Status == ProviderStatus.InitializationInProgress && successfullyCreatedFile)
            {
                SaveDictionary();

                Status = ProviderStatus.Initialized;
            }
        }

        public void CancelInitialization()
        {
            Status = ProviderStatus.CancellationInProgress;
            //Cancelation identical to Initialization
        }

        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status != ProviderStatus.Initialized)
            {
                throw new FileProviderNotInitializedException();
            }

            return _dictOfdicts;
        }

        private string InspectPath(string path)
        {
            if (path == null)
            {
                Console.WriteLine(@"Cannot access language file, bacause path is null");
                return null;
            }

            path = path.EndsWith(".json") ? path : path + ".json";

            if (File.Exists(Path.GetFullPath(path)))
            {
                return path;
            }
            Console.WriteLine($@"New Json file will be created ({path})");

            string directory = Path.GetDirectoryName(path);

            if (directory != null)
            {
                //could throw IOException if file exists with same path as directory
                Directory.CreateDirectory(directory);
            }

            return path;
        }

        private void ReadFile()
        {
            string fileContent = string.Empty;

            try
            {
                fileContent = File.ReadAllText(_path);
            }
            catch
            {
                Console.WriteLine($@"Unable to open langauge file ({Path.GetFullPath(_path)}).");

                try
                {
                    fileContent = "{}"; //can also be rewritten as JsonConvert.SerializeObject(new Dictionary<CultureInfo, Dictionary<string, string>>())
                    File.WriteAllText(_path, fileContent);

                    successfullyCreatedFile = true;
                    if (Status == ProviderStatus.CancellationInProgress)
                    {
                        Status = ProviderStatus.CancellationComplete;
                    }

                    return;
                }
                catch
                {
                    Console.WriteLine($@"Unable to create new language file ({_path})");
                }
            }
            _dictOfdicts = JsonConvert.DeserializeObject<Dictionary<CultureInfo, Dictionary<string, string>>>(fileContent);

            //Cancelation identical to Initialization
            switch (Status)
            {
                case ProviderStatus.CancellationInProgress:
                    Status = ProviderStatus.CancellationComplete;
                    break;
                case ProviderStatus.InitializationInProgress:
                    Status = ProviderStatus.Initialized;
                    break;
            }
        }
    }
}
