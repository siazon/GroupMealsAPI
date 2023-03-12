using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Reports
{
    public class WsReportCategory : WsEntity
    {

        /// <summary>
        /// Report Category Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Report Category Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Reports within the Category
        /// </summary>
        public List<WsReport> CategoryReports { get; set; }

        public WsReportCategory()
        {
            CategoryReports= new List<WsReport>();
        }
         
    }
}