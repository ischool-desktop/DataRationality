using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JHSchool.DataRationality
{
    public class GradeYear3StudentAttendanceSummaryRAT : AbstractAttendanceSummaryRAT
    {
        public override string Name
        {
            get { return "學生（三年級）缺曠統計值檢查"; }
        }

        public override string Category
        {
            get { return "學務"; }
        }

        public override List<string> StudentIDs
        {
            get
            {
                return K12.Data.Student.SelectAll().Where(x=>x.Class!=null && x.Class.GradeYear.HasValue && x.Class.GradeYear.Value==3).Select(x => x.ID).ToList();
            }
        }

        public override string Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.AppendLine("檢查範圍：");
                strBuilder.AppendLine("1.三年級學生的日常生活表現紀錄。");
                strBuilder.AppendLine("2.排除預設學年度學期的日常生活表現紀錄，以避免使用者未進行手動結算情況。");
                strBuilder.AppendLine("檢查項目：檢查學生日常生活表現紀錄中的缺曠統計值是否與自動計算之缺曠統計值相同。");
                strBuilder.AppendLine("檢查意義：缺曠統計值已改為自動計算，若是結算統計值與自動計算值不一致，會造成自動結算的統計有誤。");

                return strBuilder.ToString();
            }
        }
    }
}