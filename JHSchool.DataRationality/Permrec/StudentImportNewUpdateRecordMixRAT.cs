﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Presentation.Controls;
using JHSchool.Data;
using K12.Data;

namespace JHSchool.DataRationality
{
    class StudentImportNewUpdateRecordMixRATRecord
    {
        public string 學生系統編號 { get; set; }

        public string 學號 { get; set; }

        public string 身分證號 { get; set; }

        public string 班級 { get; set; }

        public string 座號 { get; set; }

        public string 姓名 { get; set; }

        public string 狀態 { get; set; } 

        public string 異動類別 { get; set;}

        public string 異動日期 { get; set;}
        
        public string 原因及事項 { get; set;}

        public string 轉入前學校 { get; set; }

        public string 說明 { get; set; }
    }

    public class StudentImportNewUpdateRecordMixRAT : ICorrectableDataRationality
    {
        private List<JHUpdateRecordRecord> CorrectableUpdateRecords = new List<JHUpdateRecordRecord>();
        private List<StudentImportNewUpdateRecordMixRATRecord> DisplayRecords = new List<StudentImportNewUpdateRecordMixRATRecord>();
        private Dictionary<string, StudentRecord> StudentRecords = new Dictionary<string, StudentRecord>();

        #region IDataRationality 成員

        public string Name
        {
            get { return "轉入生包含『新生異動記錄』檢查"; }
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

                strBuilder.AppendLine("檢查範圍：所有學生的轉入異動。");
                strBuilder.AppendLine("檢查項目：檢查轉入生是否有包含新生異動。");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            //初始化

            DataRationalityMessage Message = new DataRationalityMessage();

            CorrectableUpdateRecords.Clear();
            DisplayRecords.Clear();
            StudentRecords.Clear();

            //取得所有異動記錄，並篩選出轉入異動（異動代碼為3）以及新生異動（異動代碼為1）
            IEnumerable<JHUpdateRecordRecord> UpdateRecords = JHUpdateRecord.SelectAll().Where(x=>x.UpdateCode.Equals("3") || x.UpdateCode.Equals("1"));

            //取得異動記錄對應的學生資料
            StudentRecords = Student.SelectByIDs(UpdateRecords.Select(x=>x.StudentID).Distinct()).ToDictionary(x=>x.ID);

            foreach (var StudentUpdateRecords in UpdateRecords.GroupBy(x => x.StudentID))
            {
                //將異動記錄排序
                List<JHUpdateRecordRecord> OrderedStudentUpdateRecords = StudentUpdateRecords.OrderBy(x => x.UpdateCode).ToList();

                //判斷只有2筆異動記錄的情況，其中1筆為『新生異動』，另外1筆為『轉入異動』
                if (OrderedStudentUpdateRecords.Count == 2 && OrderedStudentUpdateRecords[0].UpdateCode.Equals("1") && OrderedStudentUpdateRecords[1].UpdateCode.Equals("3"))
                {
                    CorrectableUpdateRecords.Add(OrderedStudentUpdateRecords[0]);

                    StudentImportNewUpdateRecordMixRATRecord DisplayRecord = new StudentImportNewUpdateRecordMixRATRecord();

                    string StudentID = OrderedStudentUpdateRecords[1].StudentID;

                    if (StudentRecords.ContainsKey(StudentID))
                    {
                        DisplayRecord.學生系統編號 = StudentRecords[StudentID].ID;
                        DisplayRecord.學號 = StudentRecords[StudentID].StudentNumber;
                        DisplayRecord.身分證號 = StudentRecords[StudentID].IDNumber;
                        DisplayRecord.班級 = StudentRecords[StudentID].Class != null ? StudentRecords[StudentID].Class.Name : string.Empty;
                        DisplayRecord.座號 = K12.Data.Int.GetString(StudentRecords[StudentID].SeatNo);
                        DisplayRecord.姓名 = StudentRecords[StudentID].Name;
                        DisplayRecord.狀態 = StudentRecords[StudentID].StatusStr;                        
                    }

                    DisplayRecord.異動日期 = OrderedStudentUpdateRecords[1].UpdateDate;
                    DisplayRecord.異動類別 = "轉入";
                    DisplayRecord.原因及事項 = OrderedStudentUpdateRecords[1].UpdateDescription;
                    DisplayRecord.轉入前學校 = OrderedStudentUpdateRecords[1].ImportExportSchool;
                    DisplayRecord.說明 = "該生為轉入生也有新生異動";

                    DisplayRecords.Add(DisplayRecord);
                }
            }

            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查筆數："+UpdateRecords.Count());
            strBuilder.AppendLine("問題筆數：" + DisplayRecords.Count());

            if (DisplayRecords.Count > 0)
            {
                strBuilder.AppendLine("建議修正方案：");
                strBuilder.AppendLine("1.將學生加入至待處理，手動刪除學生的新生異動。");
                strBuilder.AppendLine("2.運用本合理性檢查自動修正功能，將學生的新生異動批次刪除。");
                strBuilder.AppendLine("3.若您運用本合理性檢查自動修正功能，建議先單選1位學生進行嘗試，確認正確刪除後並有存入日誌再進行批次作業。");
            }

            var OrderedDisplayRecords = from record in DisplayRecords orderby record.狀態, record.班級, K12.Data.Int.ParseAllowNull(record.座號) select record;

            Message.Data = OrderedDisplayRecords.ToList();
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
            List<JHUpdateRecordRecord> AutoCorrectRecords = new List<JHUpdateRecordRecord>();

            if (K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs))
                AutoCorrectRecords = CorrectableUpdateRecords;
            else 
                AutoCorrectRecords = CorrectableUpdateRecords.Where(x=>EntityIDs.Contains(x.StudentID)).ToList();                
                

            if (MsgBox.Show("自動修正將依照檢查結果建議值進行修正總共" + AutoCorrectRecords.Count + "筆，強烈建議您務必將檢查結果匯出備份，是否進行自動修正？", "您是否要進行自動修正?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {
                    StringBuilder strBuilder = new StringBuilder();

                    strBuilder.AppendLine("學號,狀態,學年度,學期,異動日期,入學年月,備註,入學資格-畢業國小名稱,異動班級,異動姓名,異動身分證號,異動地址,異動學號,異動性別,異動生日,核准日期,核准文號");

                    for (int i = 0; i < AutoCorrectRecords.Count; i++)
                    {
                        string StudentID = AutoCorrectRecords[i].StudentID;

                        List<string> LogValues = new List<string>();

                        LogValues.Add(StudentRecords[StudentID].StudentNumber);
                        LogValues.Add(StudentRecords[StudentID].StatusStr);
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

                        strBuilder.AppendLine(string.Join(",",LogValues.ToArray()));
                    }

                    JHUpdateRecord.Delete(AutoCorrectRecords);

                    AutoCorrectRecords.ForEach(x => CorrectableUpdateRecords.Remove(x));

                    FISCA.LogAgent.ApplicationLog.Log("資料合理性檢查.轉入生包含『新生異動記錄』檢查", "將轉入生『新生異動記錄』刪除", strBuilder.ToString());

                    MsgBox.Show("已自動修正完成，您可再次執行檢查確認!");
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