using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> _dictOfdicts;
        private readonly string _path;

        public ProviderStatus Status { get; private set; }

        /// <param name="translationFilePath">the file under which the dictionary will be saved</param>
        public JsonFileProvider(string translationFilePath)
        {
            Status = ProviderStatus.InitializationInProgress;

            _path = translationFilePath;
            string fileContent = string.Empty;

            try
            {
                fileContent = System.IO.File.ReadAllText(_path);
            }
            catch
            {
                Console.WriteLine(@"Unable to read JSON file at '{0}'.", _path);

                try
                {
                    fileContent = JsonConvert.SerializeObject(new Dictionary<CultureInfo, Dictionary<string, string>>()); //can also be rewritten as = "{}"
                    try
                    {
                        //throws IOException if file exists with same path as System.IO.Path.GetDirectoryName(_path)
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path));
                    }
                    catch (System.IO.IOException) { }

                    System.IO.File.WriteAllText(_path, fileContent);
                }
                catch
                {
                    Console.WriteLine(@"Unable to write to file at '{0}'.", _path);
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

        public void SaveDictionary()
        {
            try
            {
                System.IO.File.WriteAllText(_path, JsonConvert.SerializeObject(_dictOfdicts));
            }
            catch
            {
                Console.WriteLine(@"Failed to write to file at '{0}'.", _path);
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
    }
}
