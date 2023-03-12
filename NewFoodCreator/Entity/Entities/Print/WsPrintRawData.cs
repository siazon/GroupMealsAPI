namespace Takeaway.Service.Contract.Entities.Print
{
    public class WsPrintRawData:WsEntity
    {
        public string TextPart { get; set; }
        public string ValuePart { get; set; }

        public int? SortOrder { get; set; }

        /// <summary>
        /// Enum PrintRawDataFormat
        /// </summary>
        public int RawDataFormat { get; set; }
         
    }
}