using System.Linq;
using System.Text.RegularExpressions;

namespace CardScannerFunction
{
    public class BusinessCardData
    {
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string FixedLine { get; set; }
        public string Website { get; set; }
        public string FullName { get; set; }
        public string Organisation { get; set; }
        public string Address { get; set; }

        private static Regex _matchMail = new Regex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
        private static Regex _matchMobilePhone = new Regex(@"^\s*\+?\s*([\s-(]*[0-9][\s-)]*){3,}([\s-(]*[0-9][\s-)]*){5,}$", RegexOptions.IgnoreCase);
        private static Regex _matchLandlinePhone = new Regex(@"^\s*\+?\s*([\s-(]*[0-9][\s-)]*){4,}([\s-(]*[0-9][\s-)]*){4,}$", RegexOptions.IgnoreCase);
        private static Regex _matchWebSite = new Regex(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$");

        public BusinessCardData(string fullText)
        {
            InitializeFromText(fullText);
        }

        private void InitializeFromText(string fullText)
        {

            fullText = fullText.Replace("\\n", "\n"); //If double backslashes are present convert to single

            var lines = fullText.Split('\n').ToList(); //Break text into list of lines
            lines.ForEach(s => s.Trim()); //Trim whitespace from each line
            //Identify phones,mail and website with simple regex and remove from full text after identification 
            Email = lines.Where(l => _matchMail.IsMatch(l)).FirstOrDefault();
            lines.Remove(Email);
            Mobile = lines.Where(l => _matchMobilePhone.IsMatch(l)).FirstOrDefault();
            lines.Remove(Mobile);
            FixedLine = lines.Where(l => _matchLandlinePhone.IsMatch(l)).FirstOrDefault();
            lines.Remove(FixedLine);
            Website = lines.Where(l => _matchWebSite.IsMatch(l)).FirstOrDefault();
            lines.Remove(Website);
            //The rest of the text should contain names, address and organisation
            //To identify names, organisation and address use AzureML and entity extraction on remaining full text
            //TODO: We should find a way to join all address lines (City,Street,..) before sending to ML
            var extractor = new EntityExtractor(lines);
            FullName = extractor.OutputData.Where(data => data.Type == "PER").FirstOrDefault() != null ?
                    extractor.OutputData.Where(data => data.Type == "PER").FirstOrDefault().Mention : "";
            Organisation = extractor.OutputData.Where(data => data.Type == "ORG").FirstOrDefault() != null ?
                    extractor.OutputData.Where(data => data.Type == "ORG").FirstOrDefault().Mention : "";
            Address =
                extractor.OutputData.Count(data => data.Type == "LOC") > 0 ?
                    string.Join("\n", extractor.OutputData.Where(data => data.Type == "LOC").Select(s => s.Mention)) : "";
            System.Console.WriteLine("");
        }

    }
}