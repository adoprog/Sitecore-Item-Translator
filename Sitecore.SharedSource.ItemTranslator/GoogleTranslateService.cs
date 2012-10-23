namespace Sitecore.SharedSource.ItemTranslator
{
    using Google.API.Translate;

    class GoogleTranslateService : ITranslationService
    {
        private readonly TranslateClient client = new TranslateClient("http://google.com");

        string FromLanguage { get; set; }
        string ToLanguage { get; set; }

        public GoogleTranslateService(string from, string to)
        {
            FromLanguage = from;
            ToLanguage = to;
        }

        public string Translate(string text)
        {
            return client.Translate(text, FromLanguage, ToLanguage);
        }
    }
}
