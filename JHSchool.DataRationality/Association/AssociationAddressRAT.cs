using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.UDT;
using FISCA.Presentation.Controls;
using JHSchool.Data;
using System.Windows.Forms;
using FISCA.LogAgent;

namespace JHSchool.DataRationality
{
    public class AssociationAddressRATRecord
    {
        #region 屬性
        public string 上課地點UID { get; set; } //上課地點UID

        public string 上課學年度 { get; set; }

        public string 上課學期 { get; set; }

        public string 上課地點 { get; set; }

        public string 社團系統編號 { get; set; }

        public string 社團名稱 { get; set; }

        public string 社團學年度 { get; set; }

        public string 社團學期 { get; set; } 



        #endregion
    }

    class AssociationAddressRAT : ICorrectableDataRationality
    {
        #region ICorrectableDataRationality 成員

        /// <summary>
        /// 問題資料集合
        /// </summary>
        private List<AssociationAddressRATRecord> RATRecords = new List<AssociationAddressRATRecord>();

        //UDT物件
        private AccessHelper _accessHelper = new AccessHelper();

        /// <summary>
        /// 物件對照清單
        /// </summary>
        private Dictionary<string, AssnAddress> AssnciationDic = new Dictionary<string, AssnAddress>();

        /// <summary>
        /// 課程對照物件
        /// </summary>
        Dictionary<string, JHCourseRecord> CourseDic = new Dictionary<string, JHCourseRecord>();

        /// <summary>
        /// 檢查
        /// </summary>
        public DataRationalityMessage Execute()
        {
            RATRecords.Clear();
            AssnciationDic.Clear();
            CourseDic.Clear();

            DataRationalityMessage Message = new DataRationalityMessage();

            List<AssnAddress> AssnciationAddressList = _accessHelper.Select<AssnAddress>();

            List<string> CourseIDList = new List<string>();

            foreach (AssnAddress each in AssnciationAddressList)
            {
                if (each.Address == "")
                {
                    //目的是後續要取得該課程
                    if (!CourseIDList.Contains(each.AssociationID))
                    {
                        CourseIDList.Add(each.AssociationID);
                    }

                    //建立對照清單
                    if (!AssnciationDic.ContainsKey(each.UID))
                    {
                        AssnciationDic.Add(each.UID, each);
                    }
                }
            }

            foreach (JHCourseRecord each in JHCourse.SelectByIDs(CourseIDList))
            {
                if (!CourseDic.ContainsKey(each.ID))
                {
                    CourseDic.Add(each.ID, each);
                }
            }

            foreach (AssnAddress each in AssnciationAddressList)
            {
                if (each.Address == "")
                {
                    AssociationAddressRATRecord AArat = new AssociationAddressRATRecord();
                    AArat.社團系統編號 = each.AssociationID;
                    AArat.上課學年度 = each.SchoolYear;
                    AArat.上課學期 = each.Semester;
                    AArat.上課地點 = each.Address;
                    AArat.上課地點UID = each.UID;
                    if (CourseDic.ContainsKey(each.AssociationID))
                    {
                        AArat.社團名稱 = CourseDic[each.AssociationID].Name;
                        AArat.社團學年度 = CourseDic[each.AssociationID].SchoolYear.HasValue ? CourseDic[each.AssociationID].SchoolYear.Value.ToString() : "";
                        AArat.社團學期 = CourseDic[each.AssociationID].Semester.HasValue ? CourseDic[each.AssociationID].Semester.Value.ToString() : "";
                    }
                    RATRecords.Add(AArat);
                }
            }

            //錯誤說明
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查(上課地點)筆數：" + _accessHelper.Select<AssnAddress>().Count);
            strBuilder.AppendLine("空值(上課地點)筆數：" + RATRecords.Count);
            strBuilder.AppendLine("(您可以匯出Excel以保存資料,再進行自動修正!!)");

            var SortedRATRecords = from RATRecord in RATRecords orderby RATRecord.社團名稱, RATRecord.社團系統編號, RATRecord.上課學年度, RATRecord.上課學期 select RATRecord;
            Message.Data = SortedRATRecords.ToList();
            Message.Message = strBuilder.ToString();

            if (RATRecords.Count == 0)
            {
                MsgBox.Show("未檢查到錯誤資料!!");
            }

            return Message;
        }

        /// <summary>
        /// 自動修正選擇資料
        /// </summary>
        /// <param name="EntityIDs"></param>
        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            //Log
            StringBuilder sb = new StringBuilder();
            List<AssnAddress> list = new List<AssnAddress>();

            //如果是null / 表示修正所有資料
            if (K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs))
            {
                sb.AppendLine("已進行：社團上課地點空值檢查");
                sb.AppendLine("自動修正「所有」之錯誤資料");
                sb.AppendLine("刪除資料如下：");
                sb.AppendLine("");
                foreach (AssociationAddressRATRecord APeach in RATRecords)
                {
                    if (AssnciationDic.ContainsKey(APeach.上課地點UID))
                    {
                        AssnAddress aa = AssnciationDic[APeach.上課地點UID];
                        string schoolyear = CourseDic[aa.AssociationID].SchoolYear.HasValue ? CourseDic[aa.AssociationID].SchoolYear.Value.ToString() : "";
                        string Semester = CourseDic[aa.AssociationID].Semester.HasValue ? CourseDic[aa.AssociationID].Semester.Value.ToString() : "";
                        sb.AppendLine("上課地點「" + aa.Address + "」" + "資料系統編號(UID)「" + aa.UID + "」上課學年度「" + aa.SchoolYear + "」上課學期「" + aa.Semester + "」");
                        sb.AppendLine("(社團對應資料 - 學年度「" + schoolyear + "」學期「" + Semester + "」名稱「" + CourseDic[aa.AssociationID].Name + "」)");
                        list.Add(aa);
                    }
                    //_accessHelper.Select<AssnAddress>
                }
            }
            else //修正部份
            {
                sb.AppendLine("已進行：社團上課地點空值檢查");
                sb.AppendLine("自動修正「已選擇」之錯誤項目");
                sb.AppendLine("刪除資料如下：");
                sb.AppendLine("");
                foreach (string Entityeach in EntityIDs)
                {
                    if (AssnciationDic.ContainsKey(Entityeach))
                    {
                        AssnAddress aa = AssnciationDic[Entityeach];
                        string schoolyear = CourseDic[aa.AssociationID].SchoolYear.HasValue ? CourseDic[aa.AssociationID].SchoolYear.Value.ToString() : "";
                        string Semester = CourseDic[aa.AssociationID].Semester.HasValue ? CourseDic[aa.AssociationID].Semester.Value.ToString() : "";
                        sb.AppendLine("上課地點「" + aa.Address + "」" + "資料系統編號(UID)「" + aa.UID + "」上課學年度「" + aa.SchoolYear + "」上課學期「" + aa.Semester + "」");
                        sb.AppendLine("(社團對應資料 - 學年度「" + schoolyear + "」學期「" + Semester + "」名稱「" + CourseDic[aa.AssociationID].Name + "」)");
                        list.Add(AssnciationDic[Entityeach]);
                    }
                }
            }

            DialogResult dr = MsgBox.Show("即將刪除選取之資料\n您確定要進行自動修正嗎??", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2);
            if (dr == DialogResult.Yes)
            {
                _accessHelper.DeletedValues(list.ToArray());
                ApplicationLog.Log("資料合理性檢查", "社團上課地點空值檢查(高雄市適用)", sb.ToString());
            }
            else
            {
                MsgBox.Show("已取消自動修正!!");
            }
        }

        /// <summary>
        /// 自動修正所有資料
        /// </summary>
        public void ExecuteAutoCorrect()
        {
            ExecuteAutoCorrect(null);
        }

        #endregion

        #region IDataRationality 成員

        /// <summary>
        /// 選擇加入課程待處理
        /// </summary>
        /// <param name="EntityIDs"></param>
        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            MsgBox.Show("未提供加入待處理功能");
        }

        /// <summary>
        /// 所有加入社團待處理
        /// </summary>
        public void AddToTemp()
        {
            MsgBox.Show("未提供加入待處理功能");
        }

        /// <summary>
        /// 分類
        /// </summary>
        public string Category
        {
            get { return "社團"; }
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
                strBuilder.AppendLine("社團模組之上課地點(UDT資料)");
                strBuilder.AppendLine("如為空值則列出清單");
                return strBuilder.ToString();
            }
        }

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name
        {
            get { return "社團上課地點空值檢查(高雄市適用)"; }
        }

        #endregion
    }
}
