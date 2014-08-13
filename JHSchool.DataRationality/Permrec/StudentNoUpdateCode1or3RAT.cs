using DataRationality;
using K12.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.Data;
using System.Data;

namespace JHSchool.DataRationality
{
    /// <summary>
    /// 查詢沒有新生異動與轉入異動的學生
    /// </summary>
    public class StudentNoUpdateCode1or3RAT : ICorrectableDataRationality
    {
        List<StudentNoRecord1or3RATRecord> DisplayRecords = new List<StudentNoRecord1or3RATRecord>();
        List<StudentRecord> StudentRecordList = new List<StudentRecord>();
        List<string> StudentIDList = new List<string>();
        
        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            
        }

        public void ExecuteAutoCorrect()
        {
            FISCA.Presentation.Controls.MsgBox.Show("沒有自動更新功能!");
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            List<string> addIDList = new List<string>();
            // 檢查並加入待處理,EntityIDs=null 全部加入
            if (EntityIDs == null)
            {
                foreach (string id in StudentIDList)
                    if (!K12.Presentation.NLDPanels.Student.TempSource.Contains(id))
                        addIDList.Add(id);
            }
            else
            {
                foreach (string id in EntityIDs)
                    if (!K12.Presentation.NLDPanels.Student.TempSource.Contains(id))
                        addIDList.Add(id);
            }
            K12.Presentation.NLDPanels.Student.AddToTemp(addIDList);
        }

        public void AddToTemp()
        {
            AddToTemp(null);
        }

        public string Category
        {
            get { return "學籍"; }
        }

        public string Description
        {
            get {
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.AppendLine("檢查範圍：所有學生的異動。");
                strBuilder.AppendLine("檢查項目：檢查學生沒有新生異動也沒有轉入異動。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            DataRationalityMessage Message = new DataRationalityMessage();
            DisplayRecords.Clear();
            StudentIDList.Clear();

            // 取得所有學生沒有新生異動也沒有轉入異動的學生ID
            QueryHelper qh = new QueryHelper();
            string query = @"select student.id as sid,student.name as sname,student_number,id_number,class.class_name,student.seat_no,(case student.status when 1 then'一般' when 2 then'延修' when 4 then'休學' when 8 then'輟學' when 16 then'畢業或離校' end ) as stud_status from student left join class on student.ref_class_id=class.id 
where student.status <>256 and student.id not in(select ref_student_id from update_record where update_code in('1','3')) order by student.status,class.class_name,student.seat_no";
            DataTable dt = qh.Select(query);
            foreach (DataRow dr in dt.Rows)
            {
                StudentNoRecord1or3RATRecord sdr = new StudentNoRecord1or3RATRecord();
                sdr.學生系統編號 = dr["sid"].ToString();
                sdr.學號 = dr["student_number"].ToString();
                sdr.身分證號 = dr["id_number"].ToString();
                sdr.班級 = dr["class_name"].ToString();
                sdr.座號 = dr["seat_no"].ToString();
                sdr.狀態 = dr["stud_status"].ToString();
                sdr.姓名 = dr["sname"].ToString();
                if (!StudentIDList.Contains(sdr.學生系統編號))
                    StudentIDList.Add(sdr.學生系統編號);

                DisplayRecords.Add(sdr);
            }

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("問題筆數：" + DisplayRecords.Count());
            if (DisplayRecords.Count > 0)
            {
                strBuilder.AppendLine("建議修正方案：");
                strBuilder.AppendLine("1.將學生加入至待處理，使用手動新增新生異動、轉入異動。");
                strBuilder.AppendLine("2.將學生加入至待處理，批次產生新生異動或使用匯入方式匯入新生異動、轉入異動。");
            }

            Message.Data = DisplayRecords;
            Message.Message = strBuilder.ToString();

            return Message;
        }

        public string Name
        {
            get { return "學生沒有新生異動也沒有轉入異動檢查"; }
        }
    }

    class StudentNoRecord1or3RATRecord
    {
        public string 學生系統編號 { get; set; }

        public string 學號 { get; set; }

        public string 身分證號 { get; set; }

        public string 班級 { get; set; }

        public string 座號 { get; set; }

        public string 姓名 { get; set; }

        public string 狀態 { get; set; }
    }
}
