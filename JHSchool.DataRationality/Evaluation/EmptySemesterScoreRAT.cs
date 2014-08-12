using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Data;
using FISCA.Presentation.Controls;
using JHSchool.Data;

namespace JHSchool.DataRationality
{
    public class EmptySemesterScoreRATRec
    {
        public string 學期成績系統編號 { get; set; }
        
        public string 學生系統編號 { get; set; }

        public string 學號 { get; set; }

        public string 身分證號 { get; set; }

        public string 班級 { get; set; }

        public string 座號 { get; set; }

        public string 姓名 { get; set; }

        public string 狀態 { get; set; }

        public string 學年度 { get; set; }

        public string 學期 { get; set; }
    
    }

    public class EmptySemesterScoreRAT : ICorrectableDataRationality
    {
        List<JHSemesterScoreRecord> CorrectableRecs= new List<JHSemesterScoreRecord> ();
        List<EmptySemesterScoreRATRec> RATRecs = new List<EmptySemesterScoreRATRec>();


        #region ICorrectableDataRationality 成員

        public void ExecuteAutoCorrect()
        {
            ExecuteAutoCorrect(CorrectableRecs.Select(x => x.ID));
        }

        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            try
            {
                if (!K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs))
                    if (MsgBox.Show("自動修正將依照檢查結果建議值進行修正總共" + EntityIDs.Count() + "筆，強烈建議您務必將檢查結果匯出備份，是否進行自動修正？", "您是否要進行自動修正?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        StringBuilder strBuilder = new StringBuilder(1024 * 1024);
                        List<string> LogValues = new List<string>();

                        strBuilder.AppendLine("學號,狀態,學年度,學期" + Environment.NewLine);

                        foreach (JHSemesterScoreRecord sems in CorrectableRecs)
                        {
                            if (EntityIDs.Contains(sems.ID))
                            {
                                LogValues.Add(sems.Student.StudentNumber);
                                LogValues.Add(sems.Student.StatusStr);
                                LogValues.Add(""+sems.SchoolYear);
                                LogValues.Add(""+sems.Semester);
                                strBuilder.AppendLine(string.Join(",", LogValues.ToArray()));
                                LogValues.Clear();
                            }
                        }

                        FISCA.LogAgent.ApplicationLog.Log("資料合理性檢查.學生學期科目與領域成績有空值", "刪除學生學期科目與領域成績有空值", strBuilder.ToString());
                        JHSemesterScore.Delete(EntityIDs);
                        MsgBox.Show("已自動修正完成!");

                    }            
            }
            catch (Exception e)
            {
                SmartSchool.ErrorReporting.ReportingService.ReportException(e);

                MsgBox.Show(e.Message);
            }
        }

        #endregion

        #region IDataRationality 成員

        public string Name
        {
            get { return "空學期成績檢查"; }
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

                strBuilder.AppendLine("檢查範圍：所有學生的學期科目與領域成績。");
                strBuilder.AppendLine("檢查項目：檢查學生的學期科目與領域成績是否為空值。");
                strBuilder.AppendLine("檢查意義：在計算學期領域成績如果該學年度學期沒有成績會產生空值，主要找出並修正有空值的資料。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            QueryHelper Helper = new QueryHelper();

            List<string> StudentIDs = new List<string>();

            DataTable StudentIDTable = Helper.Select("select ref_student_id from sems_subj_score where score_info like'%<SemesterSubjectScoreInfo/>%'  and score_info like'%<Domains/>%' and score_info like'%<LearnDomainScore/>%'");

            for (int i = 0; i < StudentIDTable.Rows.Count; i++)
                StudentIDs.Add("" + StudentIDTable.Rows[i][0]);

            List<JHSemesterScoreRecord> SemsScoreList= JHSemesterScore.SelectByStudentIDs(StudentIDs);

            CorrectableRecs.Clear();
            RATRecs.Clear();
            DataRationalityMessage retMsg = new DataRationalityMessage();
            try
            {
                foreach (JHSemesterScoreRecord SmesRec in SemsScoreList)
                {
                    if (SmesRec.Subjects.Count == 0 && SmesRec.Domains.Count == 0)
                    {
                        EmptySemesterScoreRATRec rec = new EmptySemesterScoreRATRec();
                        rec.學期成績系統編號 = SmesRec.ID;
                        rec.學生系統編號 = SmesRec.RefStudentID;
                        rec.身分證號 = SmesRec.Student.IDNumber;
                        rec.姓名 = SmesRec.Student.Name;
                        rec.狀態 = SmesRec.Student.StatusStr;
                        rec.座號 = K12.Data.Int.GetString(SmesRec.Student.SeatNo);
                        if (SmesRec.Student.Class != null)
                            rec.班級 = SmesRec.Student.Class.Name;
                        rec.學年度 = SmesRec.SchoolYear.ToString();
                        rec.學期 = SmesRec.Semester.ToString();
                        rec.學號 = SmesRec.Student.StudentNumber;

                        RATRecs.Add(rec);
                        CorrectableRecs.Add(SmesRec);
                    }
                }
            }
            catch (Exception ex)
            {
                retMsg.Message = ex.Message;

                return retMsg;
            }

            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查學期成績筆數：" + SemsScoreList.Count);
            strBuilder.AppendLine("學期科目與領域成績空值筆數：" + RATRecs.Count);

            var SortedRATRecords = from RATRecord in RATRecs orderby RATRecord.狀態, RATRecord.班級, K12.Data.Int.ParseAllowNull(RATRecord.座號), RATRecord.學年度, RATRecord.學期 select RATRecord;

            retMsg.Message = strBuilder.ToString();
            retMsg.Data = SortedRATRecords.ToList();

            return retMsg;
        }

        public void AddToTemp()
        {
            AddToTemp(null);
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            List<string> PrimaryKeys = new List<string>();

            if (K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs))
                PrimaryKeys = CorrectableRecs.Select(x => x.RefStudentID).Distinct().ToList();
            else
                PrimaryKeys = CorrectableRecs.Where(x => EntityIDs.Contains(x.ID)).Select(x => x.RefStudentID).Distinct().ToList();            

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());

        }

        #endregion
    }
}