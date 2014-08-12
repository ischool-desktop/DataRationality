using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using JHSchool.Data;
using K12.Data;

namespace JHSchool.DataRationality
{
    public class FlexibleDomainRATRecord
    {
        public string 學生系統編號 { get; set;}

        public string 學號 { get; set;}

        public string 身分證號 { get; set;}

        public string 班級 { get; set;}

        public string 座號 { get; set;}

        public string 姓名 { get; set;}

        public string 狀態 { get; set;}

        public string 學年度 { get; set;}

        public string 學期 { get; set;}

        public string 領域名稱 { get; set;}

        public string 分數 { get; set;}
     }


    public class FlexibleDomainRAT : IDataRationality
    {
        private List<FlexibleDomainRATRecord> RATRecords = new List<FlexibleDomainRATRecord>();

        #region IDataRationality 成員

        public string Name
        {
            get { return "彈性課程學期領域成績檢查"; }
        }

        public string Category
        {
            get { return "成績"; }
        }

        public string Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.AppendLine("檢查範圍：所有學生的學期領域成績。");
                strBuilder.AppendLine("檢查項目：檢查學生的學期領域成績是否包含領域名稱為『彈性課程』。");
                strBuilder.AppendLine("檢查意義：計算『學習領域成績』會根據學生該學年度學期的所有『學期領域成績』進行計算，在計算上應不包含『彈性課程』的領域成績。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            RATRecords.Clear();

            DataRationalityMessage Message = new DataRationalityMessage();

            int TotalCount = 0;

            try
            {
                List<JHSemesterScoreRecord> Scores = JHSchool.Data.JHSemesterScore.SelectAll();

                for (int i = 0; i < Scores.Count; i++)
                {
                    foreach (DomainScore score in Scores[i].Domains.Values)
                    {
                        TotalCount++;

                        if (score.Domain.Equals("彈性課程"))
                        {
                            FlexibleDomainRATRecord record = new FlexibleDomainRATRecord();

                            record.學生系統編號 = Scores[i].Student.ID;
                            record.學號 = Scores[i].Student.StudentNumber;
                            record.身分證號 = Scores[i].Student.IDNumber;
                            record.班級 = Scores[i].Student.Class!=null?Scores[i].Student.Class.Name:string.Empty;
                            record.座號 = K12.Data.Int.GetString(Scores[i].Student.SeatNo);
                            record.姓名 = Scores[i].Student.Name;
                            record.狀態 = Scores[i].Student.StatusStr;
                            record.學年度 = K12.Data.Int.GetString(Scores[i].SchoolYear);
                            record.學期 = K12.Data.Int.GetString(Scores[i].Semester);
                            record.領域名稱 = score.Domain;
                            record.分數 = K12.Data.Decimal.GetString(score.Score);

                            RATRecords.Add(record);
                        }
                    }
                }
            }
            catch (Exception ve)
            {
                Message.Message = ve.Message;

                return Message;
            }

            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查學期領域成績筆數：" + TotalCount);
            strBuilder.AppendLine("學期領域成績包含彈性課程筆數：" + RATRecords.Count);

            var SortedRATRecords = from RATRecord in RATRecords orderby RATRecord.狀態, RATRecord.班級, K12.Data.Int.ParseAllowNull(RATRecord.座號),RATRecord.學年度,RATRecord.學期 select RATRecord;
        
            Message.Message = strBuilder.ToString();
            Message.Data = SortedRATRecords.ToList();

            return Message;
        }

        #endregion

        #region IDataRationality Members


        public void AddToTemp()
        {
            AddToTemp(null);
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            List<string> PrimaryKeys = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? RATRecords.Select(x=>x.學生系統編號).ToList() : EntityIDs.ToList();

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());
        }

        #endregion
    }
}