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
    public class CourseSubjectNameDbRAT : IDataRationality
    {

        List<CourseSubjectNameDb> RATList = new List<CourseSubjectNameDb>();

        public void AddToTemp(System.Collections.Generic.IEnumerable<string> EntityIDs)
        {
            // 將課程加入課程待處理
            // 取得目前課程待處理內ID
            List<string> TempCourseIDList = K12.Presentation.NLDPanels.Course.TempSource;

            if (EntityIDs == null)
            {
                foreach (CourseSubjectNameDb csnb in RATList)
                {
                    if (!TempCourseIDList.Contains(csnb.課程系統編號))
                        TempCourseIDList.Add(csnb.課程系統編號);
                }
            }
            else
            {
                foreach (CourseSubjectNameDb csnb in RATList)
                {

                    if (EntityIDs.Contains(csnb.課程系統編號))
                    {
                        if (!TempCourseIDList.Contains(csnb.課程系統編號))
                            TempCourseIDList.Add(csnb.課程系統編號);
                    }
                }
            }

            // 加入課程處理
            K12.Presentation.NLDPanels.Course.AddToTemp(TempCourseIDList);

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

                strBuilder.AppendLine("檢查範圍：學生狀態(一般、延俢)，各學年度學期俢課資料。");
                strBuilder.AppendLine("檢查項目：學生修習課程在單一學年度學期科目名稱+科目級別有重覆");
                strBuilder.AppendLine("檢查意義：當學生單一學年度學期修習課程科目名稱+科目級別有重覆時，在計算學期科目成績只會取其中一個課程成績計算，將造成科目成績有誤，此檢查功能可找出有疑問學生俢課課程，並可匯出資料或是將課程加入課程待處理。");

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
                // 檢查並取得資料,status 1 一般,2 延俢
                QueryHelper qh_Err = new QueryHelper();
                string strQuery_Err = "select student.id as sid,student.student_number,student.name,course.school_year,course.semester,course.subject,course.subj_level,count(sc_attend.id) as ssid from student inner join sc_attend on student.id = sc_attend.ref_student_id inner join course on course.id=sc_attend.ref_course_id  where student.status in(1,2)  and course.subject <>'' group by sid,student.student_number,student.name,school_year,semester,subject,course.subj_level having count(sc_attend.id)>1";
                DataTable dt_Err = qh_Err.Select(strQuery_Err);
                foreach (DataRow dr in dt_Err.Rows)
                {
                    string StudentID = dr["sid"].ToString();
                    if (!Err_StudentIDList.Contains(StudentID))
                        Err_StudentIDList.Add(StudentID);
                }

                if (Err_StudentIDList.Count > 0)
                {
                    // 使用找到有疑問StudentID透過Query反查相關呈現資料
                    QueryHelper qh_Data = new QueryHelper();
                    // -- 欄位：學生系統編號、學號、年級、班級、座號、學生姓名、學年度、學期、課程系統編號、課程名稱、科目名稱,科目級別
                    string strQueryData = "select student.id as sid,student.student_number,class.grade_year,class.class_name,student.seat_no,student.name,course.school_year,course.semester,course.id as coid,course.course_name,course.subject,course.subj_level from student inner join sc_attend on student.id = sc_attend.ref_student_id inner join course on course.id=sc_attend.ref_course_id left join class on student.ref_class_id=class.id where course.subject <>'' and student.id in(" + string.Join(",", Err_StudentIDList.ToArray()) + ")";
                    DataTable dt_Data = qh_Data.Select(strQueryData);

                    // 比對資料並填入
                    foreach (DataRow drd in dt_Data.Rows)
                    {
                        foreach (DataRow dre in dt_Err.Rows)
                        {
                            // sid,school_year,semester,subject
                            if (dre["sid"].ToString() == drd["sid"].ToString() && dre["school_year"].ToString() == drd["school_year"].ToString() && dre["semester"].ToString() == drd["semester"].ToString() && dre["subject"].ToString() == drd["subject"].ToString() && dre["subj_level"].ToString() == drd["subj_level"].ToString())
                            {
                                CourseSubjectNameDb csnd = new CourseSubjectNameDb();
                                csnd.班級 = drd["class_name"].ToString();
                                csnd.課程系統編號 = drd["coid"].ToString();
                                csnd.課程名稱 = drd["course_name"].ToString();
                                csnd.年級 = drd["grade_year"].ToString();
                                csnd.學年度 = drd["school_year"].ToString();
                                csnd.座號 = drd["seat_no"].ToString();
                                csnd.學期 = drd["semester"].ToString();
                                csnd.學生系統編號 = drd["sid"].ToString();
                                csnd.學生姓名 = drd["name"].ToString();
                                csnd.學號 = drd["student_number"].ToString();
                                csnd.科目名稱 = drd["subject"].ToString();
                                csnd.科目級別 = drd["subj_level"].ToString();
                                RATList.Add(csnd);
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
            sb.AppendLine("有重覆俢課紀錄筆數: " + RATList.Count + " 筆");

            Message.Message = sb.ToString();
            // 排序
            var SortedRATRecords = from RATRecord in RATList orderby RATRecord.年級, RATRecord.班級, K12.Data.Int.ParseAllowNull(RATRecord.座號), RATRecord.學年度, RATRecord.學期, RATRecord.科目名稱 select RATRecord;
            Message.Data = SortedRATRecords.ToList();

            return Message;
        }

        public string Name
        {
            get { return "學生修習課程科目名稱+科目級別重覆檢查"; }
        }
    }

    public class CourseSubjectNameDb
    {
        /// <summary>
        /// 課程系統編號
        /// </summary>
        public string 課程系統編號 { get; set; }


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
        /// 課程名稱
        /// </summary>
        public string 課程名稱 { get; set; }

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
