using FISCA.UDT;

namespace JHSchool.DataRationality
{
    //社團模組 - 上課地點物件
    //透過AssociationAddressRAT檢查空值之內容
    [TableName("JHSchool.Association.UDT.Address")]
    class AssnAddress : ActiveRecord
    {
        //Key ID
        /// <summary>
        /// 社團ID(課程)
        /// </summary>
        [Field(Field = "AssociationID", Indexed = true)]
        public string AssociationID { get; set; }

        /// <summary>
        /// Key 學年度
        /// </summary>
        [Field(Field = "SchoolYear", Indexed = false)]
        public string SchoolYear { get; set; }

        /// <summary>
        /// Key 學期
        /// </summary>
        [Field(Field = "Semester", Indexed = false)]
        public string Semester { get; set; }

        /// <summary>
        /// 上課地點
        /// </summary>
        [Field(Field = "Address", Indexed = false)]
        public string Address { get; set; }
    }
}
