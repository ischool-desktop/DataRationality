using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using JHSchool.Data;
using K12.Logic;
using K12.Data;
using FISCA.Presentation.Controls;
using FISCA.LogAgent;
using System.Windows.Forms;

namespace JHSchool.DataRationality
{
    public class DemeritDetailClearedButSummaryNotClearedRATRecord
    {
        #region 屬性
        public string 自動統計鍵值 { get; set; } //學生系統編號+學年度+學期

        public string 學生系統編號 { get; set; }

        public string 學號 { get; set; }

        public string 身分證號 { get; set; }

        public string 班級 { get; set; }

        public string 座號 { get; set; }

        public string 姓名 { get; set; }

        public string 狀態 { get; set; }

        public int 學年度 { get; set; }

        public int 學期 { get; set; }

        public int 明細大過數 { get; set; }

        public int 明細小過數 { get; set; }

        public int 明細警告數 { get; set; }

        public int 銷過明細大過數 { get; set; }

        public int 銷過明細小過數 { get; set; }

        public int 銷過明細警告數 { get; set; }

        public int 非明細大過數 { get; set; }

        public int 非明細小過數 { get; set; }

        public int 非明細警告數 { get; set; }

        public int 自動統計大過數 { get; set; }

        public int 自動統計小過數 { get; set; }

        public int 自動統計警告數 { get; set; } 
        #endregion
    }

    public class DemeritDetailClearedButSummaryNotClearedRAT : ICorrectableDataRationality
    {
        #region IDataRationality 成員

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name
        {
            get { return "懲戒自動統計檢查(高雄市適用)"; }
        }

        /// <summary>
        /// 群組
        /// </summary>
        public string Category
        {
            get { return "學務"; }
        }

        /// <summary>
        /// 說明
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.AppendLine("檢查條件：");
                strBuilder.AppendLine("1.[懲戒明細]與[懲戒自動統計]-->(不相等)。");
                strBuilder.AppendLine("2.[懲戒明細已銷過資料]與[非明細統計]-->(相等)");
                strBuilder.AppendLine("3.學年度/學期範圍:97/1,97/2,98/1,98/2");
                strBuilder.AppendLine("4.轉入生已排除");
                strBuilder.AppendLine("(依資料庫大小,可能需費時2至6分鐘)");
                return strBuilder.ToString();
            }
        }

        /// <summary>
        /// 問題資料集合
        /// </summary>
        private List<DemeritDetailClearedButSummaryNotClearedRATRecord> RATRecords = new List<DemeritDetailClearedButSummaryNotClearedRATRecord>();

        /// <summary>
        /// 超級物件字典(學生ID)
        /// </summary>
        private Dictionary<string, SuperObj> Dic = new Dictionary<string, SuperObj>();
        /// <summary>
        /// 功能區域
        /// </summary>
        /// <returns></returns>
        public DataRationalityMessage Execute()
        {
            RATRecords.Clear();

            DataRationalityMessage Message = new DataRationalityMessage();

            int UpdateRecordCount = 0;
            //資料物件清單
            Dic.Clear();

            //取得學生資料及建立資料物件
            foreach (JHStudentRecord student in JHStudent.SelectAll())
            {
                if (!Dic.ContainsKey(student.ID))
                {
                    Dic.Add(student.ID, new SuperObj(student));
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
                if (Dic[each].IsCleadDmmerit())
                {
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
                }
            }

            //錯誤說明
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查(懲戒統計)筆數：" + AutoSummaryList.Count());
            strBuilder.AppendLine("問題(懲戒統計)筆數：" + RATRecords.Count);
            strBuilder.AppendLine("具有(轉入異動)記錄之學生共" + UpdateRecordCount + "名,未列入檢查範圍!");
            strBuilder.AppendLine("(您可以匯出Excel以保存資料,再進行自動修正!!)");

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
        public void SetValue(SuperObj obj, int SchoolYear, int Semester)
        {
            DemeritDetailClearedButSummaryNotClearedRATRecord ddcbsnc = new DemeritDetailClearedButSummaryNotClearedRATRecord();
            ddcbsnc.自動統計鍵值 = obj.StudentRecord.ID + "," + SchoolYear + "," + Semester;
            ddcbsnc.學生系統編號 = obj.StudentRecord.ID;
            ddcbsnc.學年度 = SchoolYear;
            ddcbsnc.學期 = Semester;
            ddcbsnc.姓名 = obj.StudentRecord.Name;
            ddcbsnc.身分證號 = obj.StudentRecord.IDNumber;
            ddcbsnc.班級 = obj.StudentRecord.Class != null ? obj.StudentRecord.Class.Name : "";
            ddcbsnc.座號 = obj.StudentRecord.SeatNo.HasValue ? obj.StudentRecord.SeatNo.Value.ToString() : "";
            ddcbsnc.狀態 = obj.StudentRecord.StatusStr;
            ddcbsnc.學號 = obj.StudentRecord.StudentNumber;
            ddcbsnc.明細大過數 = obj.DetailA;
            ddcbsnc.明細小過數 = obj.DetailB;
            ddcbsnc.明細警告數 = obj.DetailC;
            ddcbsnc.銷過明細大過數 = obj.DefAuto.ClearedDemeritA;
            ddcbsnc.銷過明細小過數 = obj.DefAuto.ClearedDemeritB;
            ddcbsnc.銷過明細警告數 = obj.DefAuto.ClearedDemeritC;
            ddcbsnc.非明細大過數 = obj.DefAuto.InitialDemeritA;
            ddcbsnc.非明細小過數 = obj.DefAuto.InitialDemeritB;
            ddcbsnc.非明細警告數 = obj.DefAuto.InitialDemeritC;
            ddcbsnc.自動統計大過數 = obj.DefAuto.DemeritA;
            ddcbsnc.自動統計小過數 = obj.DefAuto.DemeritB;
            ddcbsnc.自動統計警告數 = obj.DefAuto.DemeritC;
            RATRecords.Add(ddcbsnc);
        }

        /// <summary>
        /// 加入至待處理
        /// </summary>
        public void AddToTemp()
        {
            AddToTemp(null);
        }

        /// <summary>
        /// 加入選擇至待處理
        /// </summary>
        /// <param name="EntityIDs"></param>
        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            //EntityIDs是否為null / 是:將RATRecords選擇所有學生系統編號 否:EntityIDs
            List<string> PrimaryKeys = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? RATRecords.Select(x => x.學生系統編號).ToList() : EntityIDs.ToList();

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());

            MsgBox.Show("已將" + PrimaryKeys.Distinct().Count() + "名學生,加入帶處理!!");
        }

        #endregion

        #region ICorrectableDataRationality 成員

        /// <summary>
        /// 自動修正所有資料
        /// </summary>
        public void ExecuteAutoCorrect()
        {
            ExecuteAutoCorrect(null);
        }

        /// <summary>
        /// 自動修正選擇資料
        /// </summary>
        /// <param name="EntityIDs">修正資料的ID</param>
        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            StringBuilder sb_Help = new StringBuilder();
            sb_Help.AppendLine("錯誤資料情境說明：");
            sb_Help.AppendLine("懲戒明細　=警告(0/2)<-未銷過/已銷過");
            sb_Help.AppendLine("懲戒非明細=警告(2)<---一般生之非明細資料應該為0");
            sb_Help.AppendLine("懲戒統計　=警告(2)<---自動統計數值不正確");
            sb_Help.AppendLine("");
            sb_Help.AppendLine("自動修正功能將會修正為：");
            sb_Help.AppendLine("懲戒明細　=警告(0/2)<-未銷過/已銷過");
            sb_Help.AppendLine("懲戒非明細=警告(0)<---非明細已被修正為0(減去銷過支數)");
            sb_Help.AppendLine("懲戒統計　=警告(0)<---統計值等於(未銷過明細)即為正確狀態");
            sb_Help.AppendLine("");
            sb_Help.AppendLine("您確定要進行資料修正？");
            DialogResult dr = MsgBox.Show(sb_Help.ToString(), "進行自動修正前您必須瞭解...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr == DialogResult.No)
                return;

            //Log
            StringBuilder sb = new StringBuilder();

            List<MoralScoreRecord> list = new List<MoralScoreRecord>();

            //如果是null / 表示修正所有資料
            if (K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs))
            {
                sb.AppendLine("已進行：懲戒明細不等於自動統計檢查");
                sb.AppendLine("自動修正「所有」之錯誤資料");
                sb.AppendLine("詳細資料如下：");
                sb.AppendLine("");
                foreach (DemeritDetailClearedButSummaryNotClearedRATRecord APeach in RATRecords)
                {
                    List<string> strList = APeach.自動統計鍵值.Split(',').ToList();//系統編號+學年度+學期

                    if (Dic.ContainsKey(strList[0]))
                    {
                        sb.AppendLine("學生：「" + Dic[strList[0]].StudentRecord.Name + "」學年度：「" + strList[1] + "」學期：「" + strList[2] + "」");

                        MoralScoreRecord msr = setInitialSummary(sb, strList[1], strList[2], Dic[strList[0]]);
                        if (msr != null)
                        {
                            list.Add(msr);
                        }
                    }
                }
            }
            else //將傳入的ID以逗號分割,並且取得缺曠編號
            {
                sb.AppendLine("已進行懲戒明細不等於自動統計檢查");
                sb.AppendLine("自動修正「已選擇」之錯誤項目");
                sb.AppendLine("詳細資料如下：");
                sb.AppendLine("");
                foreach (string Entityeach in EntityIDs)
                {
                    List<string> strList = Entityeach.Split(',').ToList();//系統編號+學年度+學期

                    if (Dic.ContainsKey(strList[0]))
                    {
                        sb.AppendLine("學生：「" + Dic[strList[0]].StudentRecord.Name + "」學年度：「" + strList[1] + "」學期：「" + strList[2] + "」");

                        MoralScoreRecord msr = setInitialSummary(sb, strList[1], strList[2], Dic[strList[0]]);
                        if (msr != null)
                        {
                            list.Add(msr);
                        }
                    }
                }
            }

            try
            {
                MoralScore.Update(list); //更新InitialSummary資料
            }
            catch (Exception ex)
            {
                MsgBox.Show("自動修正失敗\n" + ex.Message);
                return;
            }

            ApplicationLog.Log("資料合理性檢查", "懲戒明細不等於自動統計檢查", sb.ToString());


            MsgBox.Show("自動修正已完成。\n\n請重新執行資料合理性檢查\n以確保資料清單正確性\n避免對資料進行重覆修正。");
        }
        #endregion
        private MoralScoreRecord setInitialSummary(StringBuilder sb, string SchoolYear, string Semester, SuperObj obj)
        {
            foreach (AutoSummaryRecord auto in obj.AutoSummaryDic)
            {
                if (auto.SchoolYear == int.Parse(SchoolYear) && auto.Semester == int.Parse(Semester))
                {
                    XmlHelper helper = new XmlHelper(auto.InitialSummary);
                    //取得資料並且轉換
                    int A = K12.Data.Int.Parse(helper.GetElement("DisciplineStatistics/Demerit").GetAttribute("A"));
                    int B = K12.Data.Int.Parse(helper.GetElement("DisciplineStatistics/Demerit").GetAttribute("B"));
                    int C = K12.Data.Int.Parse(helper.GetElement("DisciplineStatistics/Demerit").GetAttribute("C"));
                    sb.AppendLine("「非明細懲戒統計」目前內容為：");
                    sb.AppendLine("大過：「" + A + "」小過「" + B + "」警告「" + C + "」");

                    //減去銷過數量
                    A -= auto.ClearedDemeritA;
                    B -= auto.ClearedDemeritB;
                    C -= auto.ClearedDemeritC;

                    sb.AppendLine("修正後「非明細懲戒統計」為：");
                    sb.AppendLine("大過：「" + A + "」小過「" + B + "」警告「" + C + "」");
                    sb.AppendLine("");
                    //設定回xml資料內
                    helper.GetElement("DisciplineStatistics/Demerit").SetAttribute("A", A.ToString());
                    helper.GetElement("DisciplineStatistics/Demerit").SetAttribute("B", B.ToString());
                    helper.GetElement("DisciplineStatistics/Demerit").SetAttribute("C", C.ToString());
                    //更新資料需由MoralScore進行更新
                    return auto.MoralScore;
                }
            }
            return null;
        }
    }



    public class SuperObj
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

        public SuperObj(JHStudentRecord student)
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
            //return DemeritList.TrueForAll(x => x.Cleared == "是");

            return DemeritList.Exists(x => x.Cleared == "是");

            //foreach (DemeritRecord each in DemeritList)
            //{
            //    if (each.Cleared == "是")
            //    {
            //        return true;
            //    }
            //}
            //return false;
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
        public bool DetailAndSummary(int SchoolYear,int Semester)
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
                    //非明細相加大於0
                    try
                    {
                        //明細(不等於)自動統計+非明細(等於)銷過資料
                        if (autoSummary.DemeritA != DetailA && autoSummary.InitialDemeritA == autoSummary.ClearedDemeritA
                            || autoSummary.DemeritB != DetailB && autoSummary.InitialDemeritB == autoSummary.ClearedDemeritB
                            || autoSummary.DemeritC != DetailC && autoSummary.InitialDemeritC == autoSummary.ClearedDemeritC)
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
