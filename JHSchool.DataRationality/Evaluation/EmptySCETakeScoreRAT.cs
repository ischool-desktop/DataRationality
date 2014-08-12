using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Data;
using FISCA.Presentation.Controls;
using JHSchool.Data;
using K12.Data;

namespace JHSchool.DataRationality
{
    public class EmptySCETakeScoreRAT : ICorrectableDataRationality
    {
        private List<EmptySCETakeScoreRATRecord> RATRecords = new List<EmptySCETakeScoreRATRecord>();

        [FISCA.Data.Query("select sce_take.ID as 評量系統編號,sce_take.ref_sc_attend_id as 學生修課編號,sc_attend.ref_student_id as 學生系統編號,sc_attend.ref_course_id as 課程系統編號,xpath_string(sce_take.extension,'/Extension/Score') as 定期評量設定中含平時評量成績,xpath_string(sce_take.extension,'/Extension/Effort') as 平時努力程度於定期評量,xpath_string(sc_attend.extension,'/Extension/OrdinarilyScore') as 平時評量成績,xpath_string(sc_attend.extension,'/Extension/OrdinarilyEffort') as 平時努力程度於學生修課 from sce_take inner join exam on sce_take.ref_exam_id=exam.id inner join sc_attend on sce_take.ref_sc_attend_id = sc_attend.id  where exam_name='平時評量' and xpath_string(sce_take.extension,'/Extension/Score')='' and sc_attend.ref_course_id in (select course.id from course where subject<>'')")]
        public class EmptySCETakeScoreRATRecord
        {
            public string 評量系統編號 { get; set; }

            public string 課程系統編號 { get; set; }

            public string 學生系統編號 { get; set; }

            public string 學年度 { get; set; }

            public string 學期 { get; set; }

            public string 課程名稱 { get; set; }

            public string 學號 { get; set; }

            public string 班級 { get; set; }

            public string 座號 { get; set; }

            public string 姓名 { get; set; }

            public string 狀態 { get; set; }

            public string 定期評量設定中含平時評量成績 { get; set; }

            public string 定期評量設定中含平時評量努力程度 { get; set; }

            public string 平時評量成績 { get; set; }

            public string 平時評量努力程度 { get; set; }

            public string 自動修正建議 { get; set; }
        }

        public class DisplayEmptySCETakeScoreRATRecord
        {
            public string 評量系統編號 { get; set; }

            public string 學年度 { get; set; }

            public string 學期 { get; set; }

            public string 課程名稱 { get; set; }

            public string 學號 { get; set; }

            public string 班級 { get; set; }

            public string 座號 { get; set; }

            public string 姓名 { get; set; }

            public string 狀態 { get; set; }

            public string 定期評量設定中含平時評量成績 { get; set; }

            public string 定期評量設定中含平時評量努力程度 { get; set; }

            public string 平時評量成績 { get; set; }

            public string 平時評量努力程度 { get; set; }

            public string 自動修正建議 { get; set; }
        }

        #region ICorrectableDataRationality Members

        public void ExecuteAutoCorrect()
        {
            ExecuteAutoCorrect(null);
        }

        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            StringBuilder strLogDetail = new StringBuilder();

            List<EmptySCETakeScoreRATRecord> SelectedRATRecords = EntityIDs == null ?
            RATRecords : RATRecords.Where(x => EntityIDs.Contains(x.評量系統編號)).ToList();

            Dictionary<string, JHSCETakeRecord> UpdatedSCETakeRecords = JHSCETake
                .Select(null, null, null, SelectedRATRecords.Select(x => x.評量系統編號), null)
                .ToDictionary(x => x.ID);

            try
            {
                foreach (EmptySCETakeScoreRATRecord record in SelectedRATRecords)
                {
                    strLogDetail.AppendLine("== SCETakeRecord：" + record.評量系統編號 + " ==");
                    strLogDetail.AppendLine(UpdatedSCETakeRecords[record.評量系統編號].ToString());
                }

                StringBuilder strLog = new StringBuilder();

                strLog.AppendLine("自動修正將依照檢查結果建議值進行修正總共" + SelectedRATRecords.Count + "筆，強烈建議您務必將檢查結果匯出備份，是否進行自動修正？");

                if (MsgBox.Show(strLog.ToString(), "您是否要進行自動修正?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    strLog.AppendLine(strLogDetail.ToString());

                    if (UpdatedSCETakeRecords.Count > 0)
                        JHSCETake.Delete(UpdatedSCETakeRecords.Values);

                    MsgBox.Show("已自動修正完成!若要再繼續修正，請重新執行本合理性檢查以確保取得最新資料!");
                    FISCA.LogAgent.ApplicationLog.Log("資料合理性檢查.平時評量成績輸入檢查", "平時評量成績（於定期評量）空值檢查", strLog.ToString());
                }
            }
            catch (Exception e)
            {
                SmartSchool.ErrorReporting.ReportingService.ReportException(e);
                MsgBox.Show(e.Message);
            }
        }

        #endregion

        #region IDataRationality Members

        public string Name
        {
            get { return "平時成績（於定期評量）空值檢查（高雄市專用）"; }
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

                strBuilder.AppendLine("檢查範圍：所有學生的平時評量（放於定期評量）成績。");
                strBuilder.AppendLine("檢查項目：");
                strBuilder.AppendLine("檢查平時評量（放於定期評量）成績是否為空值，若為空值建議自動修正刪除此筆記錄。");
                strBuilder.AppendLine("平時成績已定建於系統中，若放置於『定期評量設定』中會導致學期科目成績計算錯誤。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            DataRationalityMessage Message = new DataRationalityMessage();

            QueryHelper Helper = new QueryHelper();

            RATRecords.Clear();
            RATRecords = Helper.Select<EmptySCETakeScoreRATRecord>();

            List<DisplayEmptySCETakeScoreRATRecord> DisplayRecords = new List<DisplayEmptySCETakeScoreRATRecord>();

            try
            {
                Dictionary<string, CourseRecord> Courses = Course
                    .SelectByIDs(RATRecords.Select(x => x.課程系統編號)
                    .Distinct())
                    .ToDictionary(x => x.ID);

                Dictionary<string, StudentRecord> Students = Student
                    .SelectByIDs(RATRecords.Select(x => x.學生系統編號)
                    .Distinct())
                    .ToDictionary(x => x.ID);
                Dictionary<string, ClassRecord> Classes = Class
                    .SelectAll()
                    .ToDictionary(x => x.ID);

                foreach (EmptySCETakeScoreRATRecord record in RATRecords)
                {
                    if (Students.ContainsKey(record.學生系統編號))
                    {
                        record.學號 = Students[record.學生系統編號].StudentNumber;
                        record.姓名 = Students[record.學生系統編號].Name;
                        record.狀態 = Students[record.學生系統編號].StatusStr;
                        record.座號 = K12.Data.Int.GetString(Students[record.學生系統編號].SeatNo);
                        record.班級 = Classes[Students[record.學生系統編號].RefClassID].Name;
                    }

                    if (Courses.ContainsKey(record.課程系統編號))
                    {
                        record.學年度 = K12.Data.Int.GetString(Courses[record.課程系統編號].SchoolYear);
                        record.學期 = K12.Data.Int.GetString(Courses[record.課程系統編號].Semester);
                        record.課程名稱 = Courses[record.課程系統編號].Name;
                    }

                    record.自動修正建議 = "刪除此筆平時評量（於定期評量）記錄。";

                    DisplayEmptySCETakeScoreRATRecord DisplayRecord = new DisplayEmptySCETakeScoreRATRecord();

                    DisplayRecord.平時評量成績 = record.平時評量成績;
                    DisplayRecord.平時評量努力程度 = record.平時評量努力程度;
                    DisplayRecord.定期評量設定中含平時評量成績 = record.定期評量設定中含平時評量成績;
                    DisplayRecord.定期評量設定中含平時評量努力程度 = record.定期評量設定中含平時評量努力程度;
                    DisplayRecord.自動修正建議 = record.自動修正建議;
                    DisplayRecord.姓名 = record.姓名;
                    DisplayRecord.狀態 = record.狀態;
                    DisplayRecord.座號 = record.座號;
                    DisplayRecord.班級 = record.班級;
                    DisplayRecord.課程名稱 = record.課程名稱;
                    DisplayRecord.學年度 = record.學年度;
                    DisplayRecord.學期 = record.學期;
                    DisplayRecord.學號 = record.學號;
                    DisplayRecord.評量系統編號 = record.評量系統編號;

                    DisplayRecords.Add(DisplayRecord);
                }
            }
            catch (Exception e)
            {
                MsgBox.Show(e.Message);
            }

            var SortedDisplayRecords = from DisplayRecord in DisplayRecords orderby DisplayRecord.課程名稱, DisplayRecord.狀態, DisplayRecord.班級, K12.Data.Int.ParseAllowNull(DisplayRecord.座號) select DisplayRecord;

            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查問題筆數：" + DisplayRecords.Count);
            strBuilder.AppendLine("自動修正建議：刪除平時評量（於定期評量）記錄。");

            Message.Data = SortedDisplayRecords.ToList();
            Message.Message = strBuilder.ToString();

            return Message;
        }

        public void AddToTemp()
        {
            AddToTemp(null);
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            List<string> PrimaryKeys = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ?
                RATRecords.Select(x => x.學生系統編號).ToList() :
                RATRecords.Where(x => EntityIDs.Contains(x.評量系統編號)).Select(x => x.學生系統編號).ToList();

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());
        }

        #endregion
    }
}