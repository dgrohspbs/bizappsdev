using Newtonsoft.Json.Linq;
using System;


namespace ClientFacingBot.Common
{
    [Serializable]
    public class CaseDetail
    {
        public string categoryName { get; set; }
        public string categoryCode { get; set; }
        public string categoryId { get; set; }
        public string categoryQuestion { get; set; }
        public JArray categoryItemResults { get; set; }
        public string problemString { get; set; }
        public JArray kbItemResults { get; set; }
        public int kbItemResultsIndex { get; set; }
        public string productNumber { get; set; }
        public string productName { get; set; }
        public Guid productId { get; set; }
    }
}