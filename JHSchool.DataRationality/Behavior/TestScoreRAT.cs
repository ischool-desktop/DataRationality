using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;

namespace JHSchool.DataRationality
{
    /// <summary>
    /// 測試用 by dylan
    /// </summary>
    public class TestScoreRAT : ICorrectableDataRationality
    {
        private List<TestScoreRAT> RATRecords = new List<TestScoreRAT>();

        #region ICorrectableDataRationality 成員

        /// <summary>
        /// 自動修正選擇
        /// </summary>
        /// <param name="EntityIDs"></param>
        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 自動修正所有資料
        /// </summary>
        public void ExecuteAutoCorrect()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDataRationality 成員

        /// <summary>
        /// 開始檢查資料
        /// </summary>
        /// <returns></returns>
        public DataRationalityMessage Execute()
        {
            DataRationalityMessage Message = new DataRationalityMessage();

            return Message;
        }

        /// <summary>
        /// 將所選擇學生加入待處理
        /// </summary>
        /// <param name="EntityIDs"></param>
        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 將所有學生加入待處理
        /// </summary>
        public void AddToTemp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 分類
        /// </summary>
        public string Category
        {
            get { return "成績"; }
        }

        /// <summary>
        /// 說明
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.AppendLine("檢查範圍：所有資料。");
                return strBuilder.ToString();
            }
        }

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name
        {
            get { return "測試用功能"; }
        }

        #endregion
    }

    /// <summary>
    /// Record
    /// </summary>
    class TestScoreRATRecord
    {
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
}
