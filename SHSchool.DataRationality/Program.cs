using System;
using DataRationality;
using FISCA;

namespace SHSchool.DataRationality
{
    public static class Program
    {
        [MainMethod]
        public static void Main()
        {
            try
            {
                //DataRationalityManager.Checks.Add(new EmptySemesterScoreRAT());

                DataRationalityManager.Checks.Add(new EmptySemesterEntryRAT());

                // 學生修習課程科目名稱重覆檢查
                DataRationalityManager.Checks.Add(new CourseSubjectNameDbRAT());
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}