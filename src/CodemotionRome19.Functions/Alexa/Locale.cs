using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexa.NET.Request;
using Alexa.NET.Response;

namespace CodemotionRome19.Functions.Alexa
{
    public interface ILSpeech
    {
        IOutputSpeech Get(string key, params object[] arguments);
    }

    public class LSpeech : ILSpeech
    {
        public ILSpeechStore Store { get; }

        public string Locale { get; }

        public LSpeech(ILSpeechStore store, string locale)
        {
            var localeSpeechStore = store;
            Store = localeSpeechStore ?? throw new ArgumentNullException(nameof(store));
            var str = locale;
            Locale = str ?? throw new ArgumentNullException(nameof(locale));
        }

        public IOutputSpeech Get(string key, params object[] arguments)
        {
            var localeSpeech = this;
            var outputSpeech = localeSpeech.Store.Get(localeSpeech.Locale, key, arguments);
            if (outputSpeech == null)
                throw new ArgumentOutOfRangeException(nameof(key), "No key \"" + key + "\" found in store");
            return outputSpeech;
        }
    }

    public interface ILSpeechStore
    {
        bool Supports(string locale);

        IOutputSpeech Get(string locale, string key, object[] arguments = null);
    }

    public class DictionaryLSpeechStore : ILSpeechStore
    {
        public Dictionary<string, IDictionary<string, object>> Languages { get; } = new Dictionary<string, IDictionary<string, object>>();

        public DictionaryLSpeechStore()
        {
        }

        public bool Supports(string locale)
        {
            return Languages.ContainsKey(locale.ToLower());
        }

        public IOutputSpeech Get(string locale, string key, params object[] arguments)
        {
            var value = Languages[locale.ToLower()][key];

            if (!(value is SsmlOutputSpeech speech))
                return new PlainTextOutputSpeech { Text = string.Format(value.ToString(), arguments) };

            speech.Ssml = string.Format(speech.Ssml, arguments);

            return speech;
        }

        public void AddLanguage(string locale, IDictionary<string, object> speech)
        {
            locale = locale.ToLower();
            if (Languages.ContainsKey(locale))
            {
                Languages[locale] = speech;
            }
            else
            {
                Languages.Add(locale, speech);
            }
        }
    }

    public interface ILSpeechFactory
    {
        ILSpeech Create(SkillRequest request);
        ILSpeech Create(string locale);
    }

    public class LSpeechFactory : ILSpeechFactory
    {
        public ILSpeechStore[] Stores { get; set; }

        public LSpeechFactory(params ILSpeechStore[] stores)
        {
            if (stores == null)
                throw new ArgumentNullException(nameof(stores));
            if (!stores.Any())
                throw new ArgumentOutOfRangeException(nameof(stores), "No LocaleSpeech stores found");
            Stores = stores;
        }

        public ILSpeech Create(SkillRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return Create(request.Request.Locale);
        }

        public ILSpeech Create(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
                throw new ArgumentNullException(nameof(locale));
            var store1 = Stores.FirstOrDefault(s => s.Supports(locale));
            if (store1 != null)
                return new LSpeech(store1, locale);
            if (locale.Length != 5 || locale[2] != 45)
                throw new InvalidOperationException("unable to find store that supports locale " + locale);
            {
                var generalLocale = locale.Substring(0, 2);
                var store2 = Stores.FirstOrDefault(s => s.Supports(generalLocale));
                if (store2 != null)
                    return new LSpeech(store2, generalLocale);
            }
            throw new InvalidOperationException("unable to find store that supports locale " + locale);
        }
    }
}
