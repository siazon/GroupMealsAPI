namespace Takeaway.Service.Contract.Entities.Print
{
    public class WsPrintSetting:WsEntity
    {

        public string PrinterName { get; set; }

        public float PrinterReceiptWidth { get; set; }
        public float PrinterXflow { get; set; }
        public float PrinterYflow { get; set; }
        public string PrinterFontName { get; set; }

        public int PrinterLargeFontSize { get; set; }
        public int PrinterMediumFontSize { get; set; }
        public int PrinterSmallFontSize { get; set; }
        public bool PrintWithGroup { get; set; }

        public int PrintLanguage { get; set; }
    }
}