using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using JHSchool.Data;
using K12.Data;
using K12.Logic;
using FISCA.Presentation.Controls;

namespace JHSchool.DataRationality
{
    public class DetailedInformationOnNonInspectionRATRecord
    {
        #region 屬性
        public string 學生系統編號 { get; set; }

        public string 自動統計鍵值 { get; set; } //學生系統編號+學年度+學期

        //public string 學號 { get; set; }

        //public string 身分證號 { get; set; }

        public int 學年度 { get; set; }

        public int 學期 { get; set; }

        public string 班級 { get; set; }

        public string 座號 { get; set; }

        public string 姓名 { get; set; }

        public string 狀態 { get; set; }

        public int 明細_大過數 { get; set; }

        public int 明細_小過數 { get; set; }

        public int 明細_警告數 { get; set; }

        public int 銷過明細_大過數 { get; set; }

        public int 銷過明細_小過數 { get; set; }

        public int 銷過明細_警告數 { get; set; }

        public int 非明細_大過數 { get; set; }

        public int 非明細_小過數 { get; set; }

        public int 非明細_警告數 { get; set; }

        public int 自動統計_大過數 { get; set; }

        public int 自動統計_小過數 { get; set; }

        public int 自動統計_警告數 { get; set; }
        #endregion
    }

    class DetailedInformationOnNonInspectionRAT : IDataRationality
    {
        /// <summary>
        /// 問題資料集合
        /// </summary>
        private List<DetailedInformationOnNonInspectionRATRecord> RATRecords = new List<DetailedInformationOnNonInspectionRATRecord>();

        /// <summary>
        /// 超級物件字典(學生ID)
        /// </summary>
        private Dictionary<string, SuperObj_new> Dic = new Dictionary<string, SuperObj_new>();

        #region IDataRationality 成員

        public DataRationalityMessage Execute()
        {
            RATRecords.Clear();

            DataRationalityMessage Message = new DataRationalityMessage();

            int UpdateRecordCount = 0;

            //取得學生資料及建立資料物件
            foreach (JHStudentRecord student in JHStudent.SelectAll())
            {
                if (!Dic.ContainsKey(student.ID))
                {
                    Dic.Add(student.ID, new SuperObj_new(student));
                }
            }

            foreach (JHUpdateRecordRecord each in JHUpdateRecord.SelectByStudentIDs(Dic.Keys))
            {
                if (each.UpdateCode == "3") //轉入異動代碼為"3"
                {
                    if (Dic.ContainsKey(each.StudentID)) //包含於清單內
                    {
                        Dic.Remove(each.StudentID); //將其移除
                        UpdateRecordCount++;
                    }
                }
            }

            //取得所有懲戒明細,並加入學生資料物件內
            foreach (JHDemeritRecord demerit in JHDemerit.SelectAll())
            {
                if (Dic.ContainsKey(demerit.RefStudentID))
                {
                    Dic[demerit.RefStudentID].DemeritList.Add(demerit);
                }
            }

            //取得(971,972,981,982)自動統計,排除目前學年度/學期
            List<SchoolYearSemester> SysList = new List<SchoolYearSemester>();
            SysList.Add(new SchoolYearSemester(97, 1));
            SysList.Add(new SchoolYearSemester(97, 2));
            SysList.Add(new SchoolYearSemester(98, 1));
            SysList.Add(new SchoolYearSemester(98, 2));
            SysList.Add(new SchoolYearSemester(99, 1));
            SysList.Add(new SchoolYearSemester(99, 2));
            List<AutoSummaryRecord> AutoSummaryList = AutoSummary.Select(Dic.Keys, SysList, SummaryType.Discipline, true);
            foreach (AutoSummaryRecord autoSummary in AutoSummaryList)
            {
                if (!Dic[autoSummary.RefStudentID].AutoSummaryDic.Contains(autoSummary))
                {
                    Dic[autoSummary.RefStudentID].AutoSummaryDic.Add(autoSummary);
                }
            }


            foreach (string each in Dic.Keys)
            {
                //if (Dic[each].IsCleadDmmerit())
                //{
                if (Dic[each].DetailAndSummary(97, 1))
                {
                    SetValue(Dic[each], 97, 1);
                }
                if (Dic[each].DetailAndSummary(97, 2))
                {
                    SetValue(Dic[each], 97, 2);
                }
                if (Dic[each].DetailAndSummary(98, 1))
                {
                    SetValue(Dic[each], 98, 1);
                }
                if (Dic[each].DetailAndSummary(98, 2))
                {
                    SetValue(Dic[each], 98, 2);
                }
                if (Dic[each].DetailAndSummary(99, 1))
                {
                    SetValue(Dic[each], 99, 1);
                }
                if (Dic[each].DetailAndSummary(99, 2))
                {
                    SetValue(Dic[each], 99, 2);
                }
                //}
            }

            //錯誤說明
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查(非明細統計)筆數：" + AutoSummaryList.Count());
            strBuilder.AppendLine("檢查(非明細統計)不為0筆數：" + RATRecords.Count);
            strBuilder.AppendLine("具有(轉入異動)記錄之學生共" + UpdateRecordCount + "名,未列入檢查範圍!");
            strBuilder.AppendLine("(非明細定義於:僅轉入生可以有值)");

            var SortedRATRecords = from RATRecord in RATRecords orderby RATRecord.班級, K12.Data.Int.ParseAllowNull(RATRecord.座號), RATRecord.學年度, RATRecord.學期 select RATRecord;
            Message.Data = SortedRATRecords.ToList();
            Message.Message = strBuilder.ToString();

            if (RATRecords.Count == 0)
            {
                MsgBox.Show("未檢查到錯誤資料!!");
            }

            return Message;
        }

        /// <summary>
        /// 將值填入Row的方法
        /// </summary>
        public void SetValue(SuperObj_new obj, int SchoolYear, int Semester)
        {
            DetailedInformationOnNonInspectionRATRecord ddcbsnc = new DetailedInformationOnNonInspectionRATRecord();
            ddcbsnc.自動統計鍵值 = obj.StudentRecord.ID + "," + SchoolYear + "," + Semester;
            ddcbsnc.學生系統編號 = obj.StudentRecord.ID;
            ddcbsnc.學年度 = SchoolYear;
            ddcbsnc.學期 = Semester;
            ddcbsnc.姓名 = obj.StudentRecord.Name;
            //ddcbsnc.身分證號 = obj.StudentRecord.IDNumber;
            ddcbsnc.班級 = obj.StudentRecord.Class != null ? obj.StudentRecord.Class.Name : "";
            ddcbsnc.座號 = obj.StudentRecord.SeatNo.HasValue ? obj.StudentRecord.SeatNo.Value.ToString() : "";
            ddcbsnc.狀態 = obj.StudentRecord.StatusStr;
            //ddcbsnc.學號 = obj.StudentRecord.StudentNumber;
            ddcbsnc.明細_大過數 = obj.DetailA;
            ddcbsnc.明細_小過數 = obj.DetailB;
            ddcbsnc.明細_警告數 = obj.DetailC;
            ddcbsnc.銷過明細_大過數 = obj.DefAuto.ClearedDemeritA;
            ddcbsnc.銷過明細_小過數 = obj.DefAuto.ClearedDemeritB;
            ddcbsnc.銷過明細_警告數 = obj.DefAuto.ClearedDemeritC;
            ddcbsnc.非明細_大過數 = obj.DefAuto.InitialDemeritA;
            ddcbsnc.非明細_小過數 = obj.DefAuto.InitialDemeritB;
            ddcbsnc.非明細_警告數 = obj.DefAuto.InitialDemeritC;
            ddcbsnc.自動統計_大過數 = obj.DefAuto.DemeritA;
            ddcbsnc.自動統計_小過數 = obj.DefAuto.DemeritB;
            ddcbsnc.自動統計_警告數 = obj.DefAuto.DemeritC;
            RATRecords.Add(ddcbsnc);
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            //EntityIDs是否為null / 是:將RATRecords選擇所有學生系統編號 否:EntityIDs
            List<string> PrimaryKeys = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? RATRecords.Select(x => x.學生系統編號).ToList() : EntityIDs.ToList();

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());

            MsgBox.Show("已將" + PrimaryKeys.Distinct().Count() + "名學生,加入帶處理!!");
        }

        public void AddToTemp()
        {
            AddToTemp(null);
        }

        string IDataRationality.Category
        {
            get { return "學務"; }
        }

        string IDataRationality.Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.AppendLine("檢查條件：");
                strBuilder.AppendLine("1.[懲戒非明細]內容不為零。");
                strBuilder.AppendLine("2.排除有[轉入異動]之學生");
                return strBuilder.ToString();
            }
        }

        string IDataRationality.Name
        {
            get { return "非明細資料檢查(高雄市適用)"; }
        }

        #endregion
    }

    public class SuperObj_new
    {
        /// <summary>
        /// 學生Record
        /// </summary>
        public JHStudentRecord StudentRecord { get; set; }

        /// <summary>
        /// 學生懲戒明細
        /// </summary>
        public List<DemeritRecord> DemeritList { get; set; }

        /// <summary>
        /// 學生自動統計
        /// </summary>
        public List<AutoSummaryRecord> AutoSummaryDic { get; set; }

        public SuperObj_new(JHStudentRecord student)
        {
            StudentRecord = student;
            DemeritList = new List<DemeritRecord>();
            AutoSummaryDic = new List<AutoSummaryRecord>();
        }

        /// <summary>
        /// 懲戒資料,是否有已銷過記錄
        /// </summary>
        /// <returns></returns>
        public bool IsCleadDmmerit()
        {
            return DemeritList.Exists(x => x.Cleared == "是");
        }

        /// <summary>
        /// 明細統計
        /// </summary>
        public int DetailA, DetailB, DetailC;

        public AutoSummaryRecord DefAuto;
        /// <summary>
        /// 進行明細統計,與非明細是否不一致
        /// </summary>
        /// <returns></returns>
        public bool DetailAndSummary(int SchoolYear, int Semester)
        {
            #region 進行明細統計
            DetailA = 0;
            DetailB = 0;
            DetailC = 0;

            foreach (DemeritRecord demerit in DemeritList)
            {
                //相同學年度學期
                if (SchoolYear == demerit.SchoolYear && Semester == demerit.Semester)
                {
                    //未銷過之資料
                    if (demerit.Cleared != "是")
                    {
                        DetailA += demerit.DemeritA.HasValue ? demerit.DemeritA.Value : 0;
                        DetailB += demerit.DemeritB.HasValue ? demerit.DemeritB.Value : 0;
                        DetailC += demerit.DemeritC.HasValue ? demerit.DemeritC.Value : 0;
                    }
                }
            }
            #endregion

            //與自動統計進行比較
            foreach (AutoSummaryRecord autoSummary in AutoSummaryDic)
            {
                if (SchoolYear == autoSummary.SchoolYear && Semester == autoSummary.Semester)
                {
                    //非明細不為0
                    try
                    {
                        if (autoSummary.InitialDemeritA != 0 || autoSummary.InitialDemeritB != 0 || autoSummary.InitialDemeritC != 0)
                        {
                            DefAuto = autoSummary;
                            return true;
                        }
                    }
                    catch
                    {
                        MsgBox.Show("資料檢查,發生錯誤。(學生：" + autoSummary.Student.Name + ")");
                    }
                }
            }
            return false;


        }
    }
}