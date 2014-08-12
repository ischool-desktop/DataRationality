using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Presentation.Controls;
using FISCA.LogAgent;
using FISCA.LogAgent.AccessLayer;

namespace JHSchool.DataRationality
{
    public class RepeatTheLogDataScreeningRATRecord
    {
        #region 屬性
        public string Log系統編號 { get; set; }

        public string 時間 { get; set; }

        public string 電腦名稱 { get; set; }

        public string 登入帳號 { get; set; }

        public string 動作 { get; set; }

        public string 動作分類 { get; set; }

        public string 詳細描述 { get; set; }


        #endregion
    }

    class RepeatTheLogDataScreeningRAT : IDataRationality
    {
        #region IDataRationality 成員

        public string Name
        {
            get { return "重覆執行懲戒自動修正Log記錄檢查(高雄市適用)"; }
        }

        public string Category
        {
            get { return "學務"; }
        }

        public string Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.AppendLine("檢查範圍：");
                strBuilder.AppendLine("日期於99/12/13日起始");
                strBuilder.AppendLine("所有執行<懲戒自動統計檢查(高雄市適用)>功能。");
                strBuilder.AppendLine("並執行了2次以上自動修正。");
                strBuilder.AppendLine("由於自動修正,對相同資料重覆執行多次,可能造成資料錯誤");
                return strBuilder.ToString();
            }
        }

        /// <summary>
        /// 問題資料集合
        /// </summary>
        private List<RepeatTheLogDataScreeningRATRecord> RATRecords = new List<RepeatTheLogDataScreeningRATRecord>();


        public DataRationalityMessage Execute()
        {
            RATRecords.Clear();

            DataRationalityMessage Message = new DataRationalityMessage();

            DateTime StartTime = new DateTime(2010,12,13); //起始日期

            FiscaAccessLayer f1 = new FiscaAccessLayer();
            ActionRecordCollection ActionColl = f1.GetActionByLog(StartTime, DateTime.Now, "資料合理性檢查");
            foreach (ActionRecord each in ActionColl)
            {
                if (each.Action == "懲戒明細不等於自動統計檢查")
                {
                    RepeatTheLogDataScreeningRATRecord Rep = new RepeatTheLogDataScreeningRATRecord();
                    Rep.Log系統編號 = each.ID;
                    Rep.時間 = DateTime.Parse(each.ServerTime).ToString("yyyy/MM/dd HH:mm");
                    Rep.動作 = each.ActionBy;
                    Rep.動作分類 = each.Action;
                    Rep.登入帳號 = each.Actor;
                    Rep.電腦名稱 = each.ClientInfo.HostName;
                    Rep.詳細描述 = each.Description;
                    RATRecords.Add(Rep);
                }
            }

            //錯誤說明
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("共檢查到：" + RATRecords.Count() + "筆資料");
            strBuilder.AppendLine("資料筆數大於2建議您檢查相關資料是否正確!");

            var SortedRATRecords = from RATRecord in RATRecords orderby RATRecord.時間 select RATRecord;
            Message.Data = SortedRATRecords.ToList();
            Message.Message = strBuilder.ToString();

            if (RATRecords.Count == 0)
            {
                MsgBox.Show("未檢查到錯誤資料!!");
            }

            return Message;

        }

        public void AddToTemp()
        {
            MsgBox.Show("本功能不提供加入待處理");
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            MsgBox.Show("本功能不提供加入待處理");
        }

        #endregion
    }
}
