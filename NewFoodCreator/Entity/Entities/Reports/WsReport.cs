using System;
using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Reports
{
    public class WsReport : WsEntity
    {

        /// <summary>
        /// Name of the Report
        /// </summary>
        public string ReportName { get; set; }

        /// <summary>
        /// Description of the report
        /// </summary>
        public string ReportDescription { get; set; }

        /// <summary>
        /// SQL of the report
        /// </summary>
        public string ReportSql { get; set; }


        /// <summary>
        /// Role required to access the report
        /// </summary>
        public string ReportRole { get; set; }


        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int ReportTypeId { get; set; }
        


        /// <summary>
        /// Parameters required for the report
        /// </summary>
        public List<WsReportParameter> ReportParameters { get; set; }

        /// <summary>
        /// Result Sets for the reports
        /// </summary>
        public dynamic ReportResults { get; set; }


        public WsReport()
        {
            ReportParameters = new List<WsReportParameter>();
        }
    }
}