namespace Takeaway.Service.Contract.Entities.Reports
{
    public class WsReportParameter : WsEntity
    {

        /// <summary>
        /// Report Parameter Name
        /// </summary>
        public string ReportParamName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ReportParamValue { get; set; }


        /// <summary>
        /// Report Parameter Type
        /// </summary>
        public int? ReportParamType { get; set; }
    }
}