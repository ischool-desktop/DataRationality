using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Presentation.Controls;
using JHSchool.Data;
using K12.Data;

namespace JHSchool.DataRationality
{
    public class StudentUpdateRecordADNumberRAT : ICorrectableDataRationality
    {
        private List<JHUpdateRecordRecord> CorrectableUpdateRecords = new List<JHUpdateRecordRecord>();
        private List<StudentRecord> StudentRecords = new List<StudentRecord>();

        #region IDataRationality 成員

        public string Name
        {
            get { return "新生異動核准文號缺少『字第』檢查"; }
        }

        public string Category
        {
            get { return "學籍"; }
        }

        public string Description
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.AppendLine("檢查範圍：所有學生的『新生異動』。");
                strBuilder.AppendLine("檢查項目：檢查核准文號缺少『字號』。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            CorrectableUpdateRecords = JHUpdateRecord.SelectAll().Where
                (x=>
                    x.UpdateCode.Equals("1") && 
                    !string.IsNullOrEmpty(x.ADNumber) &&
                    !x.ADNumber.Contains("字第")
                ).ToList();

            StudentRecords = Student.SelectByIDs(CorrectableUpdateRecords.Select(x => x.StudentID));

            StringBuilder strBuilder = new StringBuilder();

            DataRationalityMessage Message = new DataRationalityMessage();

            var StudentUpdateRecords = CorrectableUpdateRecords.Join(StudentRecords, x => x.StudentID, y => y.ID, (x, y) =>
                new
                {
                    學生系統編號 = y.ID,
                    學號 = y.StudentNumber,
                    身分證號 = y.IDNumber,
                    班級 = y.Class != null ? y.Class.Name : string.Empty,
                    座號 = K12.Data.Int.GetString(y.SeatNo),
                    姓名 = y.Name,
                    狀態 = y.StatusStr,
                    學年度 = K12.Data.Int.GetString(x.SchoolYear),
                    學期 = K12.Data.Int.GetString(x.Semester),
                    異動日期 = x.UpdateDate,
                    入學年月 = x.EnrollmentSchoolYear,
                    核准文號 = x.ADNumber,
                    原核准文號 = x.ADNumber.Replace("字第","")
                });

            CorrectableUpdateRecords.ForEach(x => x.ADNumber = x.ADNumber.ADNumberCorrect());

            strBuilder.AppendLine("問題筆數：" + CorrectableUpdateRecords.Count());

            if (CorrectableUpdateRecords.Count > 0)
            {
                strBuilder.AppendLine("建議修正方案：");
                strBuilder.AppendLine("1.將資料匯出後於Excel上修改後，再用匯入學生新生異動功能更新。");
                strBuilder.AppendLine("2.運用本合理性檢查自動修正功能，補上『字第』後更新地址。");
                strBuilder.AppendLine("3.自動修正方案僅考慮在核准文號（數字）前加上字號，若您有特殊情況建議使用匯入學生新生異動功能進行更新。");
            }

            var OrderedStudentUpdateRecords = from record in StudentUpdateRecords orderby record.狀態, record.班級, K12.Data.Int.ParseAllowNull(record.座號) select record;

            Message.Data = OrderedStudentUpdateRecords.ToList() ;
            Message.Message = strBuilder.ToString();

            return Message;
        }

        #endregion

        #region ICorrectableDataRationality Members

        public void ExecuteAutoCorrect()
        {
            ExecuteAutoCorrect(null);
        }

        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            List<JHUpdateRecordRecord> AutoCorrectRecords = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? CorrectableUpdateRecords : CorrectableUpdateRecords.Where(x => EntityIDs.Contains(x.StudentID)).ToList();

            if (MsgBox.Show("自動修正將依照檢查結果建議值進行修正總共" + AutoCorrectRecords.Count + "筆，強烈建議您務必將檢查結果匯出備份，是否進行自動修正？", "您是否要進行自動修正?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {
                    StringBuilder strBuilder = new StringBuilder();

                    strBuilder.AppendLine("學號,狀態,學年度,學期,異動日期,入學年月,備註,入學資格-畢業國小名稱,異動班級,異動姓名,異動身分證號,異動地址,異動學號,異動性別,異動生日,核准日期,核准文號,原核准文號");

                    Dictionary<string, StudentRecord> StudentRecordsDic = StudentRecords.ToDictionary(x => x.ID);

                    for (int i = 0; i < AutoCorrectRecords.Count; i++)
                    {
                        string StudentID = AutoCorrectRecords[i].StudentID;

                        List<string> LogValues = new List<string>();

                        LogValues.Add(StudentRecordsDic[StudentID].StudentNumber);
                        LogValues.Add(StudentRecordsDic[StudentID].StatusStr);
                        LogValues.Add(AutoCorrectRecords[i].StudentNumber);
                        LogValues.Add(K12.Data.Int.GetString(AutoCorrectRecords[i].SchoolYear));
                        LogValues.Add(K12.Data.Int.GetString(AutoCorrectRecords[i].Semester));
                        LogValues.Add(AutoCorrectRecords[i].UpdateDate);
                        LogValues.Add(AutoCorrectRecords[i].EnrollmentSchoolYear);
                        LogValues.Add(AutoCorrectRecords[i].Comment);
                        LogValues.Add(AutoCorrectRecords[i].GraduateSchool);
                        LogValues.Add(AutoCorrectRecords[i].OriginClassName);
                        LogValues.Add(AutoCorrectRecords[i].StudentName);
                        LogValues.Add(AutoCorrectRecords[i].IDNumber);
                        LogValues.Add(AutoCorrectRecords[i].OriginAddress);
                        LogValues.Add(AutoCorrectRecords[i].StudentNumber);
                        LogValues.Add(AutoCorrectRecords[i].Gender);
                        LogValues.Add(AutoCorrectRecords[i].Birthdate);
                        LogValues.Add(AutoCorrectRecords[i].ADDate);
                        LogValues.Add(AutoCorrectRecords[i].ADNumber);
                        LogValues.Add(AutoCorrectRecords[i].ADNumber.Replace("字第",""));

                        strBuilder.AppendLine(string.Join(",", LogValues.ToArray()));
                    }

                    JHUpdateRecord.Update(AutoCorrectRecords);

                    AutoCorrectRecords.ForEach(x => CorrectableUpdateRecords.Remove(x));

                    FISCA.LogAgent.ApplicationLog.Log("資料合理性檢查.學生新生異動核准文號缺少『字號』", "補上『字號』值並更新異動記錄", strBuilder.ToString());

                    MsgBox.Show("已自動修正完成!");
                }
                catch (Exception e)
                {
                    SmartSchool.ErrorReporting.ReportingService.ReportException(e);

                    MsgBox.Show(e.Message);
                }
            }
        }

        public void AddToTemp()
        {
            AddToTemp(null);
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            List<string> PrimaryKeys = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? CorrectableUpdateRecords.Select(x => x.StudentID).ToList() : EntityIDs.ToList();

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());
        }

        #endregion
    }
}