namespace Sitecore.SharedSource.ItemTranslator.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.Fields;
    using Data.Items;
    using Diagnostics;
    using Globalization;
    using Jobs;
    using Shell.Applications.Dialogs.ProgressBoxes;
    using Shell.Framework.Commands;

    class TranslateItemCommand : Command
    {
        protected const int MaxServiceRequestLength = 1000;

        protected string BaseLanguage
        {
            get
            {
                return Sitecore.Configuration.Settings.GetSetting("BaseLanguage", "en");  
            }
        }

        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Item item = context.Items[0];
            ProgressBox.Execute("ItemSync", "Translate", this.GetIcon(context, string.Empty), TranslateItem, "item:load(id=" + item.ID + ")", new object[] { item, context });
        }

        private void TranslateItem(params object[] parameters)
        {
            var context = parameters[1] as CommandContext;
            if (context != null)
            {
                Item item = parameters[0] as Item;
                if (item != null)
                {
                    this.TranslateItem(item);
                }
            }
        }

        protected virtual Item TranslateItem(Item item)
        {
            var translationService = GetTranslatorService(item);
             
            TranslateItem(item, translationService);
            return item;
        }

        protected ITranslationService GetTranslatorService(Item item)
        {
            switch (Sitecore.Configuration.Settings.GetSetting("TranslationProvider"))
            {
                case "Google":
                    {
                        return new GoogleTranslateService(BaseLanguage,
                                                          item.Language.CultureInfo.TwoLetterISOLanguageName);
                    }
                case "Bing":
                    {
                        return new BingTranslateService(BaseLanguage,
                                                        item.Language.CultureInfo.TwoLetterISOLanguageName,
                                                        Sitecore.Configuration.Settings.GetSetting("BingApplicationId"));
                    }
                default:
                    {
                        return new GoogleTranslateService(BaseLanguage,
                                                          item.Language.CultureInfo.TwoLetterISOLanguageName);
                    }
            }
        }

        public void TranslateItem(Item item, ITranslationService service)
        {
            var sourceItem = Sitecore.Context.ContentDatabase.GetItem(item.ID, Sitecore.Globalization.Language.Parse(BaseLanguage));

            Job job = Context.Job;
            if (job != null)
            {
                job.Status.LogInfo(Translate.Text("Translating item by path {0}.", new object[] { item.Paths.FullPath }));
            }

            if (item.Versions.Count == 0)
            {
                if (sourceItem == null)
                {
                    return;
                }

                item = item.Versions.AddVersion();
                item.Editing.BeginEdit();

                foreach (Field field in sourceItem.Fields)
                {
                    if ((string.IsNullOrEmpty(sourceItem[field.Name]) || field.Shared))
                    {
                        continue;
                    }

                    if (!FieldIsTranslatable(field) || FieldIsStandard(field))
                    {
                        item[field.Name] = sourceItem[field.Name];
                    }
                    else
                    {
                        var text = sourceItem[field.Name];
                        var translatedText = string.Empty;

                        if (text.Length < MaxServiceRequestLength)
                        {
                            item[field.Name] = service.Translate(text);
                            continue;
                        }

                        translatedText = SplitText(text, MaxServiceRequestLength).Aggregate(translatedText, (current, textBlock) => current + service.Translate(textBlock));

                        item[field.Name] = translatedText;
                    }
                }

                item.Editing.EndEdit();
            }
        }

        private static bool FieldIsTranslatable(Field field)
        {
            return !((field.TypeKey == "image") ||
                        (field.TypeKey == "reference") ||
                        (field.TypeKey == "general link") ||
                        (field.TypeKey == "datetime") ||
                        (field.TypeKey == "droplink") ||
                        (field.TypeKey == "droplist") ||
                        (field.TypeKey == "treelist") ||
                        (field.TypeKey == "droptree") ||
                        (field.TypeKey == "multilist") ||
                        (field.TypeKey == "checklist") ||
                        (field.TypeKey == "treelistex") ||
                        (field.TypeKey == "checkbox"));
        }

        private static bool FieldIsStandard(Field field)
        {
            return field.Definition.Template.Name == "Advanced" ||
                        field.Definition.Template.Name == "Appearance" ||
                        field.Definition.Template.Name == "Help" ||
                        field.Definition.Template.Name == "Layout" ||
                        field.Definition.Template.Name == "Lifetime" ||
                        field.Definition.Template.Name == "Insert Options" ||
                        field.Definition.Template.Name == "Publishing" ||
                        field.Definition.Template.Name == "Security" ||
                        field.Definition.Template.Name == "Statistics" ||
                        field.Definition.Template.Name == "Tasks" ||
                        field.Definition.Template.Name == "Validators" ||
                        field.Definition.Template.Name == "Workflow";
        }

        private static IEnumerable<string> SplitText(string text, int numberOfSymbols)
        {
            int offset = 0;
            List<string> lines = new List<string>();
            while (offset < text.Length)
            {
                int index = text.LastIndexOf(" ", Math.Min(text.Length, offset + numberOfSymbols));
                string line = text.Substring(offset, (index - offset <= 0 ? text.Length : index) - offset);
                offset += line.Length + 1;
                lines.Add(line);
            }

            return lines;
        }
    }
}
