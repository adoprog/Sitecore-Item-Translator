namespace Sitecore.SharedSource.ItemTranslator
{
    using BingTranslator;

    class BingTranslateService : ITranslationService
    {
        private readonly LanguageServiceClient client = new LanguageServiceClient();
        string BingApplicationId { get; set; }

        string FromLanguage { get; set; }
        string ToLanguage { get; set; }

        public BingTranslateService(string from, string to, string bingApplicationId)
        {
            FromLanguage = from;
            ToLanguage = to;
            BingApplicationId = bingApplicationId;
        }
        
        public string Translate(string text)
        {
            return client.Translate(BingApplicationId, text, FromLanguage, ToLanguage);
        }
    }
}
