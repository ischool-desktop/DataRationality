
namespace JHSchool.DataRationality
{
    public static class Utility
    {
        public static string ADNumberCorrect(this string ADNumber)
        {
            if (!string.IsNullOrEmpty(ADNumber) && !ADNumber.Contains("字第"))
            {
                int P = ADNumber.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });

                if (P >= 0)
                {
                    string ADNumberInt = ADNumber.Substring(P);

                    return ADNumber.Replace(ADNumberInt, "字第" + ADNumberInt);
                }

                return ADNumber;
            }

            return ADNumber;
        }
    }
}