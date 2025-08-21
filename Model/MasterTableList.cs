namespace Raqmiyat.Framework.Model
{
    public class BankOnBoard
    {
        public string? SwiftCode { get; set; }
        public bool? IPP_Real_Time { get; set; }
    }

    public class ThresholdAmount
    {
        public int IPP_Max_Credit_Amount { get; set; }
    }
    public class CategoryPurposeCode
    {
        public string? Code_Value { get; set; }
        public string? IsApplicable { get; set; }
    }

    public class MasterTableList
    {
        public List<BankOnBoard>? bankOnBoardList { get; set; }
        public ThresholdAmount? thresholdAmount { get; set; }
        public List<CategoryPurposeCode>? categoryPurposeCode { get; set; }
    }
}

