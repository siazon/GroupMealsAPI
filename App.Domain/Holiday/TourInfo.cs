using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Holiday
{
    public class TourInfo
    {

        public string Name { get; set; }
        public string Subtitle { get; set; }

        public string DepartFrom { get; set; }
        public string Country { get; set; }
        public List<string> StopPoints { get; set; }
        public List<string> Summary { get; set; }

        public List<string> Fee { get; set; }
        public List<string> Bref { get; set; }
        public List<TourDescription> Description { get; set; }
        public List<string> DetailSummary { get; set; }
        public List<string> Detail { get; set; }
        public List<string> Notes { get; set; }
        public List<string> Include { get; set; }
        public List<string> RefundRules { get; set; }
        public List<string> Remarks { get; set; }
        public List<string> Reminders { get; set; }


        public string NameCn { get; set; }
        public string SubtitleCn { get; set; }
        public string DepartFromCn { get; set; }
        public List<string> StopPointsCn { get; set; }
        public string CountryCn { get; set; }
        public List<string> SummaryCn { get; set; }
        public List<string> FeeCn { get; set; }
        public List<string> BrefCn { get; set; }
        public List<TourDescription> DescriptionCn { get; set; }
        public List<string> DetailSummaryCn { get; set; }
        public List<string> DetailCn { get; set; }
        public List<string> NotesCn { get; set; }
        public List<string> IncludeCn { get; set; }
        public List<string> RefundRulesCn { get; set; }
        public List<string> RemarksCn { get; set; }
        public List<string> RemindersCn { get; set; }


        public string NameTc { get; set; }
        public string SubtitleTc { get; set; }
        public string DepartFromTc { get; set; }
        public List<string> StopPointsTc { get; set; }
        public string CountryTc { get; set; }
        public List<string> SummaryTc{ get; set; }

        public List<string> FeeTc{ get; set; }
        public List<string> BrefTc{ get; set; }
        public List<TourDescription> DescriptionTc{ get; set; }
        public List<string> DetailSummaryTc{ get; set; }
        public List<string> DetailTc{ get; set; }
        public List<string> NotesTc{ get; set; }
        public List<string> IncludeTc{ get; set; }
        public List<string> RefundRulesTc{ get; set; }
        public List<string> RemarksTc{ get; set; }
        public List<string> RemindersTc{ get; set; }

        public TourInfo() {
            StopPoints = new List<string>();
            Summary =new List<string>();
            Fee=new List<string>();
            Bref=new List<string>();
            Description = new List<TourDescription>();
            DetailSummary = new List<string>();
            Detail = new List<string>();
            Notes = new List<string>();
            Include=new List<string>();
            RefundRules=new List<string>();
            Remarks=new List<string>();
            Reminders=new List<string>();

            StopPointsCn = new List<string>();
            SummaryCn = new List<string>();
            FeeCn = new List<string>();
            BrefCn = new List<string>();
            DescriptionCn = new List<TourDescription>();
            DetailSummaryCn = new List<string>();
            DetailCn = new List<string>();
            NotesCn = new List<string>();
            IncludeCn = new List<string>();
            RefundRulesCn = new List<string>();
            RemarksCn = new List<string>();
            RemindersCn = new List<string>();

            StopPointsTc=new List<string>();
            SummaryTc = new List<string>();
            FeeTc= new List<string>();
            BrefTc= new List<string>();
            DescriptionTc= new List<TourDescription>() ;
            DetailSummaryTc= new List<string>();
            DetailTc= new List<string>() ;
            NotesTc= new List<string>();
            IncludeTc = new List<string>();
            RefundRulesTc= new List<string>();
            RemarksTc= new List<string>();
            RemindersTc= new List<string>();
        }

    }
    public class TourDescription 
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
    }
 
}
