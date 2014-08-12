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
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}