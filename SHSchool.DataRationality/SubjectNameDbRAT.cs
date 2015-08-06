using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Data;
using FISCA.Presentation.Controls;
using SHSchool.Data;
using System.Collections.Generic;
using System.Data;
using System;

namespace SHSchool.DataRationality
{
    /// <summary>
    /// 檢查學生學期科目成績:科目名稱+級別重覆
    /// </summary>
    public class SubjectNameDbRAT : IDataRationality
    {
        List<SubjectNameDb> RATList = new List<SubjectNameDb>();

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            // 取得學生待處理內 ID
            List<string> TempStudentIDList = K12.Presentation.NLDPanels.Student.SelectedSource;

            if (EntityIDs == null)
            {
                foreach (SubjectNameDb csnb in RATList)
                {
                    if (!TempStudentIDList.Contains(csnb.學生系統編號))
                        TempStudentIDList.Add(csnb.學生系統編號);
                }

            }
            else
            {
                foreach (SubjectNameDb csnb in RATList)
                {

                    if (EntityIDs.Contains(csnb.學生系統編號))
                    {
                        if (!TempStudentIDList.Contains(csnb.學生系統編號))
                            TempStudentIDList.Add(csnb.學生系統編號);
                    }
                }            
            }

            // 加入課程處理
            K12.Presentation.NLDPanels.Student.AddToTemp(TempStudentIDList);
        }

        public void AddToTemp()
        {
            AddToTemp(null);
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

                strBuilder.AppendLine("檢查範圍：學生狀態(一般、延俢)，各學年度學期學期科目成績。");
                strBuilder.AppendLine("檢查項目：學生學期科目成績:科目名稱+科目級別有重覆");
                strBuilder.AppendLine("檢查意義：當學生學期科目成績:科目名稱+科目級別有重覆時，在計算學期分項成績只會取其中一個科目成績計算，將造成學期分項成績有誤，此檢查功能可找出有疑問學生科目，並可匯出資料或是將課程加入課程待處理。");

                return strBuilder.ToString();
            }            
        }

        public DataRationalityMessage Execute()
        {
            RATList.Clear();
            DataRationalityMessage Message = new DataRationalityMessage();
            List<string> Err_StudentIDList = new List<string>();
            try
            {
                // 取得學生基本
                List<SHStudentRecord> StudRecListAll = SHStudent.SelectAll();

                Dictionary<string, SHStudentRecord> StudRecDict = new Dictionary<string, SHStudentRecord>();

                foreach (SHStudentRecord rec in StudRecListAll)
                {
                    if (rec.Status == K12.Data.StudentRecord.StudentStatus.一般 || rec.Status == K12.Data.StudentRecord.StudentStatus.延修)
                        StudRecDict.Add(rec.ID,rec);
                }

                Err_StudentIDList = StudRecDict.Keys.ToList();

                // 檢查並取得資料,status 1 一般,2 延俢

                if (Err_StudentIDList.Count > 0)
                {
                   // 取得學生科目成績
                    List<SHSemesterScoreRecord> SHSemScoreList = SHSemesterScore.SelectByStudentIDs(Err_StudentIDList);

                    // 學生科目成績索引,StudentID,
                    Dictionary<string, List<ssSHSubjectScore>> SHSubjScoreDict = new Dictionary<string, List<ssSHSubjectScore>>();

                    foreach (SHSemesterScoreRecord SemsRec in SHSemScoreList)
                    {
                        if (!SHSubjScoreDict.ContainsKey(SemsRec.RefStudentID))
                            SHSubjScoreDict.Add(SemsRec.RefStudentID, new List<ssSHSubjectScore>());

                        foreach (SHSubjectScore ss in SemsRec.Subjects.Values)
                        {
                            ssSHSubjectScore sss = new ssSHSubjectScore ();
                            sss.ss=ss;
                            sss.SchoolYear=SemsRec.SchoolYear.ToString();
                            sss.Semester=SemsRec.Semester.ToString();
                            sss.GradeYear = SemsRec.GradeYear.ToString();
                            SHSubjScoreDict[SemsRec.RefStudentID].Add(sss);
                        }
                    }

                    Dictionary<string, List<ssSHSubjectScore>> ssSubjDict = new Dictionary<string, List<ssSHSubjectScore>>();
                    // 檢查 科目名稱及級別重覆
                    foreach (string studID in SHSubjScoreDict.Keys)
                    {
                        ssSubjDict.Clear();

                        foreach (ssSHSubjectScore ss1 in SHSubjScoreDict[studID])
                        {
                           
                            string key = ss1.ss.Subject;
                            if (ss1.ss.Level.HasValue)
                                key += ss1.ss.Subject + ss1.ss.Level.Value;

                            if (!ssSubjDict.ContainsKey(key))
                                ssSubjDict.Add(key, new List<ssSHSubjectScore>());
                                
                            ssSubjDict[key].Add(ss1);
                        }

                        if(StudRecDict.ContainsKey(studID))
                        {
                            foreach (string ssid in ssSubjDict.Keys)
                            {
                                // 有相同2個以上
                                if (ssSubjDict[ssid].Count > 1)
                                {
                                    foreach (ssSHSubjectScore ssScore in ssSubjDict[ssid])
                                    {
                                        SubjectNameDb snb = new SubjectNameDb();
                                        snb.學生系統編號 = studID;
                                        snb.科目名稱 = ssScore.ss.Subject;
                                        snb.科目級別 = "";
                                        if (ssScore.ss.Level.HasValue)
                                            snb.科目級別 = ssScore.ss.Level.Value.ToString();

                                        snb.座號 = "";
                                        if (StudRecDict[studID].SeatNo.HasValue)
                                            snb.座號 = StudRecDict[studID].SeatNo.Value.ToString();

                                        if (StudRecDict[studID].Class != null)
                                            snb.班級 = StudRecDict[studID].Class.Name;

                                        snb.學生姓名 = StudRecDict[studID].Name;
                                        snb.學年度 = ssScore.SchoolYear;
                                        snb.學期 = ssScore.Semester;
                                        snb.年級 = ssScore.GradeYear;
                                        snb.學號 = StudRecDict[studID].StudentNumber;

                                        RATList.Add(snb);
                                    }

                                }
                            }
                        }                        
                    }
                }
            }
            catch (Exception ex)
            {
                Message.Message = ex.Message;
                return Message;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("(學期科目名稱+科目級別)有重覆紀錄筆數: " + RATList.Count + " 筆");

            Message.Message = sb.ToString();
            // 排序
            var SortedRATRecords = from RATRecord in RATList orderby RATRecord.學號, RATRecord.班級, K12.Data.Int.ParseAllowNull(RATRecord.座號), RATRecord.科目名稱,RATRecord.科目級別, RATRecord.學年度, RATRecord.學期 select RATRecord;
            Message.Data = SortedRATRecords.ToList();

            return Message;
        }

        public string Name
        {
            get { return "學生學期科目成績：科目名稱+科目級別重覆檢查"; }
        }
    }


    public class ssSHSubjectScore
    {
        public SHSubjectScore ss { get; set; }
        
        public string SchoolYear { get; set; }

        public string Semester { get; set; }

        public string GradeYear { get; set; }
    }

    public class SubjectNameDb
    {

        /// <summary>
        /// 學生系統編號
        /// </summary>
        public string 學生系統編號 { get; set; }

        /// <summary>
        /// 學年度
        /// </summary>
        public string 學年度 { get; set; }

        /// <summary>
        /// 學期
        /// </summary>
        public string 學期 { get; set; }


        /// <summary>
        /// 年級
        /// </summary>
        public string 年級 { get; set; }

        /// <summary>
        /// 班級
        /// </summary>
        public string 班級 { get; set; }

        /// <summary>
        /// 座號
        /// </summary>
        public string 座號 { get; set; }

        /// <summary>
        /// 學號
        /// </summary>
        public string 學號 { get; set; }

        /// <summary>
        /// 學生姓名
        /// </summary>
        public string 學生姓名 { get; set; }

        /// <summary>
        /// 科目名稱
        /// </summary>
        public string 科目名稱 { get; set; }

        /// <summary>
        /// 科目級別
        /// </summary>
        public string 科目級別 { get; set; }
    }

}
