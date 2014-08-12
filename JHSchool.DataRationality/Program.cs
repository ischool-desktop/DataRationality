using System;
using DataRationality;
using FISCA;

namespace JHSchool.DataRationality
{
    public static class Program
    {
        [MainMethod]
        public static void Main()
        {
            try
            {
                DataRationalityManager.Checks.Add(new SCETakeScoreRAT());
                DataRationalityManager.Checks.Add(new EmptySCETakeScoreRAT());
                DataRationalityManager.Checks.Add(new FlexibleDomainRAT());
                //DataRationalityManager.Checks.Add(new DisciplineSummaryRAT());
                //DataRationalityManager.Checks.Add(new DisciplineSummaryV2RAT());
                //DataRationalityManager.Checks.Add(new AllStudentAttendanceSummaryRAT());
                //DataRationalityManager.Checks.Add(new GradeYear1StudentAttendanceSummaryRAT());
                //DataRationalityManager.Checks.Add(new GradeYear2StudentAttendanceSummaryRAT());
                //DataRationalityManager.Checks.Add(new GradeYear3StudentAttendanceSummaryRAT());
                DataRationalityManager.Checks.Add(new ExportStudentStatusRAT());
                DataRationalityManager.Checks.Add(new StudentImportNewUpdateRecordMixRAT());
                DataRationalityManager.Checks.Add(new StudentUpdateRecordADNumberRAT());

                //空學期成績檢查
                DataRationalityManager.Checks.Add(new EmptySemesterScoreRAT());

                //懲戒自動統計檢查(高雄市適用)
                DataRationalityManager.Checks.Add(new DemeritDetailClearedButSummaryNotClearedRAT());

                //社團模組上課地點空值檢查
                DataRationalityManager.Checks.Add(new AssociationAddressRAT());

                //非明細資料檢查(高雄市適用)
                DataRationalityManager.Checks.Add(new DetailedInformationOnNonInspectionRAT());

                //重覆執行自動修正Log記錄檢查(高雄市適用)
                //DataRationalityManager.Checks.Add(new RepeatTheLogDataScreeningRAT());

            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}