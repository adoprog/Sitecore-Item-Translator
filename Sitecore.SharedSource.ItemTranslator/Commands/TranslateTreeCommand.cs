namespace Sitecore.SharedSource.ItemTranslator.Commands
{
    using Sitecore.Data.Items;

    class TranslateTreeCommand : TranslateItemCommand
    {
        protected override Item TranslateItem(Item item)
        {
            var translationService = GetTranslatorService(item);

            TranslateItem(item, translationService);
            var items = item.Axes.GetDescendants();
            foreach (var childItem in items)
            {
                TranslateItem(childItem, translationService);
            }

            return item;
        }
    }
}
