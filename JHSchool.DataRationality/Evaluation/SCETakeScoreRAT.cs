using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataRationality;
using FISCA.Data;
using FISCA.Presentation.Controls;
using JHSchool.Data;
using K12.Data;


namespace JHSchool.DataRationality
{
    public class SCETakeScoreRAT : ICorrectableDataRationality
    {
        private List<SCETakeScoreRATRecord> RATRecords = new List<SCETakeScoreRATRecord>();

        [FISCA.Data.Query("select sce_take.ID as 評量系統編號,sce_take.ref_sc_attend_id as 學生修課編號,sc_attend.ref_student_id as 學生系統編號,sc_attend.ref_course_id as 課程系統編號,xpath_string(sce_take.extension,'/Extension/Score') as 定期評量設定中含平時評量成績,xpath_string(sce_take.extension,'/Extension/Effort') as 平時努力程度於定期評量,xpath_string(sc_attend.extension,'/Extension/OrdinarilyScore') as 平時評量成績,xpath_string(sc_attend.extension,'/Extension/OrdinarilyEffort') as 平時努力程度於學生修課 from sce_take inner join exam on sce_take.ref_exam_id=exam.id inner join sc_attend on sce_take.ref_sc_attend_id = sc_attend.id  where exam_name='平時評量' and xpath_string(sce_take.extension,'/Extension/Score')<>'' and sc_attend.ref_course_id in (select course.id from course where subject<>'')")]
        public class SCETakeScoreRATRecord
        {
            public string 評量系統編號 { get; set; }

            public string 學生修課編號 { get; set; }

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

            public string 平時評量成績 { get; set;}

            public string 平時評量努力程度 { get; set; }

            public string 自動修正建議 { get; set; }
        }

        public class DisplaySCETakeScoreRATRecord
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
                int AutoCorrectCount1 = 0;
                int AutoCorrectCount2 = 0;
                int AutoCorrectCount3 = 0;

                StringBuilder strLogDetail = new StringBuilder();

                List<SCETakeScoreRATRecord> SelectedRATRecords = EntityIDs == null ? 
                RATRecords : RATRecords.Where(x => EntityIDs.Contains(x.評量系統編號)).ToList();

                Dictionary<string, JHSCETakeRecord> UpdatedSCETakeRecords = JHSCETake
                    .Select(null,null,null,SelectedRATRecords.Select(x => x.評量系統編號),null)
                    .ToDictionary(x => x.ID);

                List<SCETakeScoreRATRecord> SCRecords = SelectedRATRecords
                    .FindAll(x => x.自動修正建議.StartsWith("1"));

                Dictionary<string, JHSCAttendRecord> UpdatedSCAttendRecords = new Dictionary<string, JHSCAttendRecord>();

                if (SCRecords.Count > 0)
                    UpdatedSCAttendRecords = JHSCAttend
                        .SelectByIDs(SCRecords.Select(x => x.學生修課編號))
                        .ToDictionary(x => x.ID);

                try
                {
                    foreach (SCETakeScoreRATRecord record in SelectedRATRecords)
                    {
                        if (string.IsNullOrEmpty(record.平時評量成績))
                        {
                            strLogDetail.AppendLine("== SCETakeRecord：" + record.評量系統編號 + "、狀況一 ==");
                            strLogDetail.AppendLine(UpdatedSCETakeRecords[record.評量系統編號].ToString());

                            UpdatedSCAttendRecords[record.學生修課編號].OrdinarilyScore = UpdatedSCETakeRecords[record.評量系統編號].Score;
                            UpdatedSCAttendRecords[record.學生修課編號].OrdinarilyEffort = UpdatedSCETakeRecords[record.評量系統編號].Effort;

                            UpdatedSCETakeRecords[record.評量系統編號].Score = null;
                            UpdatedSCETakeRecords[record.評量系統編號].Effort = null;

                            AutoCorrectCount1++;

                        }
                        else if (record.定期評量設定中含平時評量成績.Equals(record.平時評量成績))
                        {
                            strLogDetail.AppendLine("== SCETakeRecord：" + record.評量系統編號 + "、狀況二 ==");
                            strLogDetail.AppendLine(UpdatedSCETakeRecords[record.評量系統編號].ToString());

                            UpdatedSCETakeRecords[record.評量系統編號].Score = null;
                            UpdatedSCETakeRecords[record.評量系統編號].Effort = null;

                            AutoCorrectCount2++;
                        }
                        else if (!record.定期評量設定中含平時評量成績.Equals(record.平時評量成績))
                        {
                            strLogDetail.AppendLine("== SCETakeRecord：" + record.評量系統編號 + "、狀況三 ==");
                            strLogDetail.AppendLine(UpdatedSCETakeRecords[record.評量系統編號].ToString());

                            UpdatedSCETakeRecords[record.評量系統編號].Score = null;
                            UpdatedSCETakeRecords[record.評量系統編號].Effort = null;

                            AutoCorrectCount3++;
                        }
                    }

                    StringBuilder strLog = new StringBuilder();

                    strLog.AppendLine("自動修正將依照檢查結果建議值進行修正總共" + SelectedRATRecords.Count + "筆，強烈建議您務必將檢查結果匯出備份，是否進行自動修正？");
                    strLog.AppendLine("1.『定期評量設定中含平時評量成績』有資料，而『平時評量成績』無資料，『自動修正』將前者資料複蓋到後者，並將前者資料清空。=>共" + AutoCorrectCount1 + "筆");
                    strLog.AppendLine("2.『定期評量設定中含平時評量成績』等於『平時評量成績』，『自動修正』將前者資料清空。=>共" + AutoCorrectCount2 + "筆");
                    strLog.AppendLine("3.『定期評量設定中含平時評量成績』不等於『平時評量成績』，建議先手動核對後者正確性，再『自動修正』將前者資料清空。=>共" + AutoCorrectCount3 + "筆");

                    if (MsgBox.Show(strLog.ToString(), "您是否要進行自動修正?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        strLog.AppendLine(strLogDetail.ToString());

                        if (UpdatedSCETakeRecords.Values.Count > 0)
                            JHSCETake.Update(UpdatedSCETakeRecords.Values);
                        if (UpdatedSCAttendRecords.Values.Count > 0)
                            JHSCAttend.Update(UpdatedSCAttendRecords.Values);

                        MsgBox.Show("已自動修正完成!若要再繼續修正，請重新執行本合理性檢查以確保取得最新資料!");
                        FISCA.LogAgent.ApplicationLog.Log("資料合理性檢查.平時評量成績輸入檢查", "平時評量成績輸入自動修正", strLog.ToString());

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
            get { return "平時評量成績輸入檢查（高雄市專用）"; }
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
                strBuilder.AppendLine("1.『定期評量設定中含平時評量成績』有資料，而『平時評量成績』無資料，『自動修正』將前者資料複蓋到後者，並將前者資料清空。");
                strBuilder.AppendLine("2.『定期評量設定中含平時評量成績』等於『平時評量成績』，『自動修正』將前者資料清空。");
                strBuilder.AppendLine("3.『定期評量設定中含平時評量成績』不等於『平時評量成績』，建議先手動核對後者正確性，再『自動修正』將前者資料清空。");
                //strBuilder.AppendLine("1.『定期評量設定中含平時評量成績』有資料，但是在『平時評量成績』中沒有資料。");
                //strBuilder.AppendLine("1.1將『定期評量設定中含平時評量成績』搬移到『平時評量成績』。 ");
                //strBuilder.AppendLine("2.『定期評量設定中含平時評量成績』有資料，但是在『平時評量成績』中也有資料。");
                //strBuilder.AppendLine("2.1 兩者一致將『定期評量設定中含平時評量成績』資料清空。");
                //strBuilder.AppendLine("2.2 兩者不一致情況，方便使用者匯出、加入待處理，以及清空『定期評量設定中含平時評量成績』資料。");
                strBuilder.AppendLine("檢查意義：系統已內建『平時評量成績』，不需再在『定期評量設定』設定『平時評量』，若設定可能會造成學期科目成績結算不正確。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            DataRationalityMessage Message = new DataRationalityMessage();

            QueryHelper Helper = new QueryHelper();

            RATRecords.Clear();
            RATRecords = Helper.Select<SCETakeScoreRATRecord>();

            List<DisplaySCETakeScoreRATRecord> DisplayRecords = new List<DisplaySCETakeScoreRATRecord>();

            try
            {
                Dictionary<string, CourseRecord> Courses = Course
                    .SelectByIDs(RATRecords.Select(x => x.課程系統編號)
                    .Distinct())
                    .ToDictionary(x => x.ID);

                Dictionary<string, StudentRecord> Students = Student
                    .SelectByIDs(RATRecords.Select(x => x.學生系統編號)
                    .Distinct())
                    .ToDictionary(x=>x.ID);
                Dictionary<string, ClassRecord> Classes = Class.SelectAll().ToDictionary(x => x.ID);

                    foreach (SCETakeScoreRATRecord record in RATRecords)
                    {
                        if (Students.ContainsKey(record.學生系統編號))
                        {
                            record.學號 = Students[record.學生系統編號].StudentNumber;
                            record.姓名 = Students[record.學生系統編號].Name;
                            record.狀態 = Students[record.學生系統編號].StatusStr;
                            record.座號 = K12.Data.Int.GetString(Students[record.學生系統編號].SeatNo);
                            record.班級 = Classes.ContainsKey(Students[record.學生系統編號].RefClassID)?Classes[Students[record.學生系統編號].RefClassID].Name:string.Empty;
                        }

                        if (Courses.ContainsKey(record.課程系統編號))
                        {
                            record.學年度 = K12.Data.Int.GetString(Courses[record.課程系統編號].SchoolYear);
                            record.學期 = K12.Data.Int.GetString(Courses[record.課程系統編號].Semester);
                            record.課程名稱 = Courses[record.課程系統編號].Name;
                        }

                        if (string.IsNullOrEmpty(record.平時評量成績))
                            record.自動修正建議 = "1.『定期評量設定中含平時評量成績』有資料，而『平時評量成績』無資料，『自動修正』將前者資料複蓋到後者，並將前者資料清空。";
                        else if (record.定期評量設定中含平時評量成績.Equals(record.平時評量成績))
                            record.自動修正建議 = "2.『定期評量設定中含平時評量成績』等於『平時評量成績』，『自動修正』將前者資料清空。";
                        else if (!record.定期評量設定中含平時評量成績.Equals(record.平時評量成績))
                            record.自動修正建議 = "3.『定期評量設定中含平時評量成績』不等於『平時評量成績』，建議先手動核對後者正確性，再『自動修正』將前者資料清空。";

                        DisplaySCETakeScoreRATRecord DisplayRecord = new DisplaySCETakeScoreRATRecord();

                        DisplayRecord.平時評量成績 = record.平時評量成績;
                        DisplayRecord.平時評量努力程度 = record.平時評量努力程度;
                        DisplayRecord.自動修正建議 = record.自動修正建議;
                        DisplayRecord.姓名 = record.姓名;
                        DisplayRecord.定期評量設定中含平時評量成績 = record.定期評量設定中含平時評量成績;
                        DisplayRecord.定期評量設定中含平時評量努力程度 = record.定期評量設定中含平時評量努力程度;
                        DisplayRecord.狀態 = record.狀態;
                        DisplayRecord.座號 = record.座號;
                        DisplayRecord.班級 = record.班級;
                        DisplayRecord.評量系統編號 = record.評量系統編號;
                        DisplayRecord.課程名稱 = record.課程名稱;
                        DisplayRecord.學年度 = record.學年度;
                        DisplayRecord.學期 = record.學期;
                        DisplayRecord.學號 = record.學號;

                        DisplayRecords.Add(DisplayRecord);
                    }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            var SortedDisplayRecords = from DisplayRecord in DisplayRecords orderby DisplayRecord.課程名稱,DisplayRecord.狀態, DisplayRecord.班級, K12.Data.Int.ParseAllowNull(DisplayRecord.座號) select DisplayRecord;

            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查問題筆數：" + RATRecords.Count);
            strBuilder.AppendLine("自動修正建議（狀況3請您務必核對資料後再進行修正）：");
            strBuilder.AppendLine("1.『定期評量設定中含平時評量成績』有資料，而『平時評量成績』無資料，『自動修正』將前者資料複蓋到後者，並將前者資料清空。");
            strBuilder.AppendLine("2.『定期評量設定中含平時評量成績』等於『平時評量成績』，『自動修正』將前者資料清空。");
            strBuilder.AppendLine("3.『定期評量設定中含平時評量成績』不等於『平時評量成績』，建議先手動核對後者正確性，再『自動修正』將前者資料清空。");

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