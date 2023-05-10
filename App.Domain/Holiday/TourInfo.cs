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

        public string NameEn { get; set; }
        public string SubtitleEn { get; set; }

        public string DepartFromEn { get; set; }
        public string CountryEn { get; set; }
        public List<string> StopPointsEn { get; set; }
        public List<string> SummaryEn { get; set; }

        public List<string> FeeEn { get; set; }
        public List<string> BrefEn { get; set; }
        public List<TourDescription> DescriptionEn { get; set; }
        public List<string> DetailSummaryEn { get; set; }
        public List<string> DetailEn { get; set; }
        public List<string> NotesEn { get; set; }
        public List<string> IncludeEn { get; set; }
        public List<string> RefundRulesEn { get; set; }
        public List<string> RemarksEn { get; set; }
        public List<string> RemindersEn { get; set; }


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
            StopPointsEn = new List<string>();
            SummaryEn =new List<string>();
            FeeEn=new List<string>();
            BrefEn=new List<string>();
            DescriptionEn = new List<TourDescription>();
            DetailSummaryEn = new List<string>();
            DetailEn = new List<string>();
            NotesEn = new List<string>();
            IncludeEn=new List<string>();
            RefundRulesEn=new List<string>();
            RemarksEn=new List<string>();
            RemindersEn=new List<string>();

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
