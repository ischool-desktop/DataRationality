//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Xml;
//using DataRationality;
//using JHSchool.Behavior.BusinessLogic;
//using JHSchool.Data;

//namespace JHSchool.DataRationality
//{
//    public class DisciplineSummaryV2RAT : IDataRationality
//    {

//        #region IDataRationality 成員

//        public string Name
//        {
//            get { return "學生獎懲統計值檢查（加強版）"; }
//        }

//        public string Category
//        {
//            get { return "學務"; }
//        }

//        public string Description
//        {
//            get
//            {
//                StringBuilder strBuilder = new StringBuilder();

//                strBuilder.AppendLine("檢查範圍：");
//                strBuilder.AppendLine("1.所有學生的日常生活表現紀錄。");
//                strBuilder.AppendLine("2.排除預設學年度學期的日常生活表現紀錄，以避免使用者未進行手動結算情況。");
//                strBuilder.AppendLine("3.排除使用者因為銷過但未重新進行結算所造成的不一致。");
//                strBuilder.AppendLine("檢查項目：檢查學生日常生活表現紀錄中的獎懲統計值是否與自動計算之獎懲統計值相同。");
//                strBuilder.AppendLine("檢查意義：獎懲統計值已改為自動計算，若是結算統計值與自動計算值不一致，會造成自動結算的統計有誤。");

//                return strBuilder.ToString();
//            }
//        }

//        public DataRationalityMessage Execute()
//        {
//            StringBuilder strBuilder = new StringBuilder();

//            int ProblemCount = 0;

//            //建立資料合理性檢查回傳物件
//            DataRationalityMessage Message = new DataRationalityMessage();

//            //建立詳細訊息物件列表
//            List<object> records = new List<object>();

//            //取得學生編號列表
//            List<string> StudentIDs = JHStudent.SelectAll().Select(x => x.ID).ToList();

//            //根據學生編號取得日常生活表現物件列表
//            List<JHMoralScoreRecord> moralscorerecords = JHMoralScore.Select(null, StudentIDs, null, null);

//            //根據學生編號取得自動計算獎懲物件列表
//            List<AutoSummaryRecord> autosummaryrecords = AutoSummary.Select(StudentIDs, null);

//            //取得預設學年度及學期
//            int SchoolYear = K12.Data.Int.Parse(K12.Data.School.DefaultSchoolYear);
//            int Semester = K12.Data.Int.Parse(K12.Data.School.DefaultSemester);

//            try
//            {
//                //以日常生活表現物件為主判斷對應的自動獎懲值是否正確，並且排除預設學年度學期的日常生活表現紀錄
//                foreach (JHMoralScoreRecord moralscorerecord in moralscorerecords.Where(x=>!(x.SchoolYear==SchoolYear && x.Semester==Semester)))
//                {
//                    if (moralscorerecord.Summary != null)
//                    {
//                        XmlElement MeritElm = moralscorerecord.Summary.SelectSingleNode("DisciplineStatistics/Merit") as XmlElement;
//                        XmlElement DemeritElm = moralscorerecord.Summary.SelectSingleNode("DisciplineStatistics/Demerit") as XmlElement;

//                        if (MeritElm != null && DemeritElm != null)
//                        {

//                            int MeritA = K12.Data.Int.Parse(MeritElm.GetAttribute("A"));
//                            int MeritB = K12.Data.Int.Parse(MeritElm.GetAttribute("B"));
//                            int MeritC = K12.Data.Int.Parse(MeritElm.GetAttribute("C"));

//                            int DemeritA = K12.Data.Int.Parse(DemeritElm.GetAttribute("A"));
//                            int DemeritB = K12.Data.Int.Parse(DemeritElm.GetAttribute("B"));
//                            int DemeritC = K12.Data.Int.Parse(DemeritElm.GetAttribute("C"));

//                            List<AutoSummaryRecord> filterrecords = autosummaryrecords.Where(x => x.RefStudentID.Equals(moralscorerecord.RefStudentID) && x.SchoolYear == moralscorerecord.SchoolYear && x.Semester == moralscorerecord.Semester).ToList();

//                            if (!(filterrecords.Count == 1))
//                                throw new Exception("學號" + moralscorerecord.Student.StudentNumber + "對於" + moralscorerecord.SchoolYear + "學年度" + moralscorerecord.Semester + "學期應有對應的自動缺曠獎懲統計物件!!筆數為" + records.Count);
//                            else
//                            {
//                                //假設AutoSummary計算的值沒有與Summary的值完全相等
//                                if (!(filterrecords[0].MeritA == MeritA && filterrecords[0].MeritB == MeritB && filterrecords[0].MeritC == MeritC && filterrecords[0].DemeritA == DemeritA && filterrecords[0].DemeritB == DemeritB && filterrecords[0].DemeritC == DemeritC))
//                                {
//                                    //假設AutoSummary計算的值再加上銷過數沒有與Summary的值完全相等
//                                    if (!(filterrecords[0].MeritA == MeritA && filterrecords[0].MeritB == MeritB && filterrecords[0].MeritC == MeritC && (filterrecords[0].DemeritA + filterrecords[0].ClearedDemeritA) == DemeritA && (filterrecords[0].DemeritB + filterrecords[0].ClearedDemeritB) == DemeritB && (filterrecords[0].DemeritC+ filterrecords[0].ClearedDemeritC) == DemeritC))
//                                    {
//                                        ProblemCount++;

//                                        var record = new
//                                        {
//                                            學編 = moralscorerecord.Student.ID,
//                                            學號 = moralscorerecord.Student.StudentNumber,
//                                            班級 = moralscorerecord.Student.Class != null ? moralscorerecord.Student.Class.Name : string.Empty,
//                                            座號 = moralscorerecord.Student.SeatNo,
//                                            姓名 = moralscorerecord.Student.Name,
//                                            學年度 = moralscorerecord.SchoolYear,
//                                            學期 = moralscorerecord.Semester,
//                                            大功 = MeritA,
//                                            大功自動 = filterrecords[0].MeritA,
//                                            大功不同 = filterrecords[0].MeritA!=MeritA?"Y":"",
//                                            小功 = MeritB,
//                                            小功自動 = filterrecords[0].MeritB,
//                                            小功不同 = filterrecords[0].MeritB!=MeritB?"Y":"",
//                                            嘉獎 = MeritC,
//                                            嘉獎自動 = filterrecords[0].MeritC,
//                                            嘉獎不同 = filterrecords[0].MeritC != MeritC ? "Y" : "",
//                                            大過 = DemeritA,
//                                            大過自動 = filterrecords[0].DemeritA,
//                                            大過不同 = filterrecords[0].DemeritA != DemeritA ? "Y" : "",
//                                            大過銷過 = filterrecords[0].ClearedDemeritA,
//                                            小過 = DemeritB,
//                                            小過自動 = filterrecords[0].DemeritB,
//                                            小過不同 = filterrecords[0].DemeritB != DemeritB ? "Y" : "",
//                                            小過銷過 = filterrecords[0].ClearedDemeritB,
//                                            警告 = DemeritC,
//                                            警告自動 = filterrecords[0].DemeritC,
//                                            警告不同 = filterrecords[0].DemeritC != DemeritC ? "Y" : "",
//                                            警告銷過 = filterrecords[0].ClearedDemeritC,
//                                            狀態 = ""+filterrecords[0].Student.Status
//                                        };

//                                        records.Add(record);
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Message.Message = e.Message;
//                Message.Data = autosummaryrecords;
//                return Message;
//            }

//            strBuilder.AppendLine("檢查獎懲統計筆數：" + moralscorerecords.Count);
//            strBuilder.AppendLine("問題獎懲統計筆數：" + ProblemCount);
//            strBuilder.AppendLine(ProblemCount>0?"系統中有學生日常生活表現獎懲統計值與自動獎懲統計值不一致，建議您運用本檢查匯出功能將自動結算值刪除後，再運用系統匯入功能將獎懲統計值匯入做調整。":string.Empty);

//            Message.Data = records;
//            Message.Message = strBuilder.ToString();

//            return Message;
//        }

//        #endregion
//    }
//}