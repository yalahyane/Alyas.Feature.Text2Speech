using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Alyas.Feature.Text2Speech.Models;
using HtmlAgilityPack;
using Microsoft.CognitiveServices.Speech;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Presentation;

namespace Alyas.Feature.Text2Speech.Controllers
{
    public class Text2SpeechController : Controller
    {
        public ActionResult Text2SpeechPlayer()
        {
            var allRichTextContent = GetAllRichTextFromPage();

            var readTimeInMinutes = CalculateReadTime(allRichTextContent, 160);
            var readTimeSpan = TimeSpan.FromMinutes(readTimeInMinutes);

            return View("/Views/Feature/Text2Speech/Text2SpeechPlayer.cshtml", new Text2SpeechModel{ReadTime = readTimeSpan.Minutes < 1? 1:readTimeSpan.Minutes, Text = allRichTextContent});
        }

        [HttpPost]
        public ActionResult ConvertText2Speech(Text2SpeechModel model)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(Settings.GetSetting("Alyas.Text2Speech.ApiKey"), Settings.GetSetting("Alyas.Text2Speech.Region"));
                config.SpeechSynthesisVoiceName = Settings.GetSetting("Alyas.Text2Speech.VoiceName");
                using (var synthesizer = new SpeechSynthesizer(config, null))
                {
                    var result = synthesizer.SpeakTextAsync(model.Text).Result;
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        using (var stream = AudioDataStream.FromResult(result))
                        {
                            var tempFilePath = Path.GetTempFileName();
                            stream.SaveToWaveFileAsync(tempFilePath).GetAwaiter().GetResult();
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                                {
                                    fileStream.CopyToAsync(memoryStream).GetAwaiter().GetResult();
                                }
                                System.IO.File.Delete(tempFilePath);

                                memoryStream.Seek(0, SeekOrigin.Begin);

                                return File(memoryStream.ToArray(), "audio/wav");
                            }
                        }
                    }

                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Could not synthesize speech.");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"ConvertTextToSpeech : {ex}", this);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, $"Could not synthesize speech. {ex}");
            }

        }

        private string GetAllRichTextFromPage()
        {
            var stringBuilder = new StringBuilder();
            var renderings = RenderingContext.Current.PageContext.PageDefinition.Renderings;

            foreach (var rendering in renderings)
            {
                if (rendering.Parameters.Contains("IncludeInSpeech") && rendering.Parameters["IncludeInSpeech"] == bool.TrueString)
                {
                    var dataSourceItem = GetDataSourceItem(rendering);

                    if (dataSourceItem != null)
                    {
                        foreach (var field in dataSourceItem.Fields.Where(f => f.Type == "Rich Text"))
                        {
                            if (!string.IsNullOrEmpty(field.Value))
                            {
                                var plainText = ConvertHtmlToPlainText(field.Value);
                                stringBuilder.AppendLine(plainText);
                            }
                        }
                    }
                }
            }

            return stringBuilder.ToString();
        }

        private static string ConvertHtmlToPlainText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var script in doc.DocumentNode.Descendants("script").ToArray())
                script.Remove();

            foreach (var style in doc.DocumentNode.Descendants("style").ToArray())
                style.Remove();

            foreach (var comment in doc.DocumentNode.SelectNodes("//comment()")?.ToArray() ?? new HtmlNode[] { })
                comment.Remove();

            return doc.DocumentNode.InnerText.Trim();
        }

        private static Item GetDataSourceItem(Rendering rendering)
        {
            if (!string.IsNullOrEmpty(rendering.DataSource))
            {
                var dataSourceItem = Sitecore.Context.Database.GetItem(rendering.DataSource);
                return dataSourceItem;
            }
            return null;
        }

        private static double CalculateReadTime(string text, int wordsPerMinute)
        {
            if (wordsPerMinute <= 0) throw new ArgumentException("Words per minute should be greater than 0.");

            var wordCount = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            return (double)wordCount / wordsPerMinute;
        }
    }
}