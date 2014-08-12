using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DataRationality;
using JHSchool.Logic;
using JHSchool.Data;

namespace JHSchool.DataRationality
{
    public class AttendanceSummaryRATRecord
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

        public string 節次類型 { get; set;}

        public string 缺曠假別 { get; set;}

        public string 缺曠統計值 { get; set;}

        public string 缺曠自動統計值 { get; set;}
    }

    abstract public class AbstractAttendanceSummaryRAT : IDataRationality
    {

        private List<AttendanceSummaryRATRecord> RATRecords = new List<AttendanceSummaryRATRecord>();

        #region IDataRationality 成員

        abstract public string Name { get; }

        abstract public string Category { get; }

        abstract public List<string> StudentIDs { get; }

        abstract public string Description { get; }

        public DataRationalityMessage Execute()
        {
            RATRecords.Clear();

            StringBuilder strBuilder = new StringBuilder();

            DataRationalityMessage Message = new DataRationalityMessage();

            List<JHMoralScoreRecord> moralscorerecords = JHMoralScore.Select(null, StudentIDs, null, null);
            List<AutoSummaryRecord> autosummaryrecords = AutoSummary.Select(StudentIDs, null);

            //取得預設學年度及學期
            int SchoolYear = K12.Data.Int.Parse(K12.Data.School.DefaultSchoolYear);
            int Semester = K12.Data.Int.Parse(K12.Data.School.DefaultSemester);

            try
            {
                foreach (JHMoralScoreRecord moralscorerecord in moralscorerecords.Where(x => !(x.SchoolYear == SchoolYear && x.Semester == Semester)))
                {
                    List<AutoSummaryRecord> filterrecords = autosummaryrecords.Where(x => x.RefStudentID.Equals(moralscorerecord.RefStudentID) && x.SchoolYear == moralscorerecord.SchoolYear && x.Semester == moralscorerecord.Semester).ToList();

                    if (filterrecords.Count != 1)
                        throw new Exception("學號" + moralscorerecord.Student.StudentNumber + "對於" + moralscorerecord.SchoolYear + "學年度" + moralscorerecord.Semester + "學期應有對應的自動缺曠獎懲統計物件!!筆數為" + filterrecords.Count);

                    if (moralscorerecord.Summary != null)
                    {
                        foreach (XmlNode Node in moralscorerecord.Summary.SelectNodes("AttendanceStatistics/Absence"))
                        {
                            XmlElement Elm = Node as XmlElement;

                            if (Elm != null)
                            {
                                int Count = 0;

                                List<AbsenceCountRecord> absencecountrecords = filterrecords[0].AbsenceCounts.Where(x => x.Name.Equals(Elm.GetAttribute("Name")) && x.PeriodType.Equals(Elm.GetAttribute("PeriodType"))).ToList();

                                if (absencecountrecords.Count == 1)
                                    Count = absencecountrecords[0].Count;

                                if (Count != K12.Data.Int.Parse(Elm.GetAttribute("Count")))
                                {
                                    AttendanceSummaryRATRecord record = new AttendanceSummaryRATRecord();

                                    record.學生系統編號 = moralscorerecord.Student.ID;
                                    record.學號 = moralscorerecord.Student.StudentNumber;
                                    record.身分證號 = moralscorerecord.Student.IDNumber;
                                    record.班級 = moralscorerecord.Student.Class != null ? moralscorerecord.Student.Class.Name : string.Empty;
                                    record.座號 = K12.Data.Int.GetString(moralscorerecord.Student.SeatNo);
                                    record.姓名 = moralscorerecord.Student.Name;
                                    record.狀態 = moralscorerecord.Student.StatusStr;
                                    record.學年度 = K12.Data.Int.GetString(moralscorerecord.SchoolYear);
                                    record.學期 = K12.Data.Int.GetString(moralscorerecord.Semester);
                                    record.節次類型 = Elm.GetAttribute("PeriodType");
                                    record.缺曠假別 = Elm.GetAttribute("Name");
                                    record.缺曠統計值 = Elm.GetAttribute("Count");
                                    record.缺曠自動統計值 = K12.Data.Int.GetString(Count);

                                    RATRecords.Add(record);
                                }
                            }
                        }
                    }

                }

            }
            catch (Exception e)
            {
                Message.Message = e.Message;
                Message.Data = RATRecords;
                return Message;
            }

            strBuilder.AppendLine("檢查缺曠統計筆數：" + moralscorerecords.Count);
            strBuilder.AppendLine("問題缺曠統計筆數：" + RATRecords.Count);
            strBuilder.AppendLine(RATRecords.Count > 0 ? "系統中有學生日常生活表現缺曠統計值與自動缺曠統計值不一致，建議您運用本檢查匯出功能將自動結算值刪除後，再運用系統匯入功能將缺曠統計值匯入做調整。" : string.Empty);

            var SortedRATRecords = from RATRecord in RATRecords orderby RATRecord.狀態,RATRecord.班級,K12.Data.Int.ParseAllowNull(RATRecord.座號),RATRecord.學年度,RATRecord.學期 select RATRecord;

            Message.Data = SortedRATRecords.ToList();
            Message.Message = strBuilder.ToString();

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