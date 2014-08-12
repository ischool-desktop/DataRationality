using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataRationality;
using FISCA.Presentation.Controls;
using JHSchool.Data;

namespace JHSchool.DataRationality
{
    public class ExportStudentStatusRAT : ICorrectableDataRationality
    {

        private List<string> CanAutoCorrectStudentIDs = new List<string>();
        private StringBuilder LogBuilder = new StringBuilder();

        #region IDataRationality Members

        public string Name
        {
            get { return "轉出生學生狀態為『一般』檢查"; }
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

                strBuilder.AppendLine("檢查範圍：狀態為一般的學生。");
                strBuilder.AppendLine("檢查項目：檢查學生的異動記錄是否有轉出異動。");
                strBuilder.AppendLine("檢查意義：若是學生有轉出異動，其狀態應為『畢業或離校』或『刪除』");

                return strBuilder.ToString();
            }
        }

        public DataRationalityMessage Execute()
        {
            CanAutoCorrectStudentIDs.Clear();
            LogBuilder = new StringBuilder();
            LogBuilder.AppendLine("學生系統編號,身分證號,學號,班級,座號,姓名,狀態,學年度,學期,異動類別,原因及事項,異動日期,轉出後學校");

            DataRationalityMessage Message = new DataRationalityMessage();
            
            List<JHStudentRecord> Students = JHStudent.SelectAll().Where(x => x.Status == K12.Data.StudentRecord.StudentStatus.一般).ToList();

            List<JHUpdateRecordRecord> UpdateRecords= JHUpdateRecord.SelectByStudentIDs(Students.Select(x => x.ID)).Where(x => x.UpdateCode.Equals("4")).ToList();

            StringBuilder strBuilder = new StringBuilder();

            strBuilder.AppendLine("檢查學生筆數："+ Students.Count);
            strBuilder.AppendLine("問題學生筆數：" + UpdateRecords.Count);

            if (UpdateRecords.Count > 0)
            {
                strBuilder.AppendLine("建議修正方案：");
                strBuilder.AppendLine("1.將學生加入至待處理，手動變更學生的狀態為『畢業或離校』或『刪除』。");
                strBuilder.AppendLine("2.運用本合理性檢查自動修正功能，將學生的狀態批次變更為『刪除』。");
                strBuilder.AppendLine("3.若您運用本合理性檢查自動修正功能，建議先單選1位學生進行嘗試，確認正確變更為『刪除』狀態再進行批次作業。");
            }

            var StudentUpdateRecords = UpdateRecords.Join(Students, UpdateRecord => UpdateRecord.StudentID, Student => Student.ID, (UpdateRecord, Student) => 
                new 
                {
                    學生系統編號 = Student.ID ,
                    身分證號 = Student.IDNumber, 
                    學號 = Student.StudentNumber,
                    班級 = Student.Class != null ? Student.Class.Name : string.Empty,
                    座號 = K12.Data.Int.GetString(Student.SeatNo),
                    姓名 = Student.Name,
                    狀態 = Student.StatusStr,
                    學年度 = K12.Data.Int.GetString(UpdateRecord.SchoolYear),
                    學期 = K12.Data.Int.GetString(UpdateRecord.Semester),
                    異動類別 = "轉出",
                    原因及事項 = UpdateRecord.UpdateDescription,
                    異動日期 = UpdateRecord.UpdateDate,
                    轉出後學校 = UpdateRecord.ImportExportSchool,
                });

            foreach(var r in StudentUpdateRecords)
                LogBuilder.AppendLine(r.學生系統編號+","+r.身分證號+","+r.學號+","+r.班級+","+r.座號+","+r.姓名+","+r.狀態+","+ r.學年度+","+r.學期+","+r.異動類別+","+r.原因及事項+","+r.異動日期+","+r.轉出後學校);


            Message.Data = StudentUpdateRecords.ToList();
            Message.Message = strBuilder.ToString();

            CanAutoCorrectStudentIDs.AddRange(UpdateRecords.Select(x => x.StudentID).Distinct());

            return Message;
        }

        #endregion

        #region IAutoCorrectDataRationality Members

        public void ExecuteAutoCorrect()
        {
            ExecuteAutoCorrect(null);
        }

        public void ExecuteAutoCorrect(IEnumerable<string> EntityIDs)
        {
            if (!K12.Data.Utility.Utility.IsNullOrEmpty(CanAutoCorrectStudentIDs))
            {
                if (MsgBox.Show("自動修正將依照檢查結果學其狀態變更為『刪除』，強烈建議您務必將檢查結果匯出備份；是否進行自動修正？", "您是否要進行自動修正?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        //取得要變更為『刪除』狀態的學生編號
                        List<string> AutoCorrectStudentIDs = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? CanAutoCorrectStudentIDs : EntityIDs.ToList();

                        //取得所有學生資料
                        List<JHStudentRecord> AllStudent = JHStudent.SelectAll();

                        //取得要變更為『刪除』狀態的學生
                        List<JHStudentRecord> Students = AllStudent.Where(x => AutoCorrectStudentIDs.Contains(x.ID)).ToList();

                        //取得『刪除』狀態的學生
                        List<JHStudentRecord> DeleteStudents = AllStudent.Where(x => x.Status == K12.Data.StudentRecord.StudentStatus.刪除).ToList();

                        #region 檢查『學號』在一般及刪除狀態重覆
                        List<string> RepeatStudentNumber = DeleteStudents.Select(x => x.StudentNumber).Intersect(Students.Select(x => x.StudentNumber)).ToList();

                        if (RepeatStudentNumber.Count>0)
                        {
                            MsgBox.Show("以下學號"+string.Join(",",RepeatStudentNumber.ToArray())+"在『一般』及『刪除』狀態重覆，無法變更狀態!");
                            return;
                        }
                        #endregion

                        #region 檢查『身分證號』在一般及刪除狀態重覆
                        List<string> RepeatIDNumber = DeleteStudents.Select(x => x.IDNumber).Intersect(Students.Select(x => x.IDNumber)).ToList();

                        if (RepeatIDNumber.Count > 0)
                        {
                            MsgBox.Show("以下身分證號" + string.Join(",", RepeatIDNumber.ToArray()) + "在『一般』及『刪除』狀態重覆，無法變更狀態!");
                            return;
                        }
                        #endregion

                        #region 檢查『登入帳號』在一般及刪除狀態重覆
                        List<string> RepeatLoginName = DeleteStudents.Select(x => x.SALoginName).Intersect(Students.Select(x => x.SALoginName)).ToList();

                        if (RepeatLoginName.Count > 0)
                        {
                            MsgBox.Show("以下登入帳號" + string.Join(",", RepeatLoginName.ToArray()) + "在『一般』及『刪除』狀態重覆，無法變更狀態!");
                            return;
                        }
                        #endregion

                        Students.ForEach(x=> x.Status = K12.Data.StudentRecord.StudentStatus.刪除);

                        int SuccessCount = JHStudent.Update(Students);

                        FISCA.LogAgent.ApplicationLog.Log("資料合理性檢查.一般生轉出異動記錄檢查", "將一般生有轉出異動記錄學生狀態改為『刪除』", LogBuilder.ToString());
                    }
                    catch (Exception e)
                    {
                        SmartSchool.ErrorReporting.ReportingService.ReportException(e);

                        MsgBox.Show(e.Message);
                    }

                    MsgBox.Show("已自動修正完成!");
                }
            }
        }

        public void AddToTemp()
        {
            AddToTemp(null);
        }

        public void AddToTemp(IEnumerable<string> EntityIDs)
        {
            List<string> PrimaryKeys = K12.Data.Utility.Utility.IsNullOrEmpty(EntityIDs) ? CanAutoCorrectStudentIDs : EntityIDs.ToList();

            PrimaryKeys.AddRange(K12.Presentation.NLDPanels.Student.TempSource);

            K12.Presentation.NLDPanels.Student.AddToTemp(PrimaryKeys.Distinct().ToList());
        }

        #endregion
    }
}