

namespace UAEIPP_Outward_MTMX_Worker.Model
{
    public class RepairQueueMessage
    {

        public decimal? SwiftID { get; set; }
        public string? HDR_Message_Id { get; set; }
        public string? DET_EndToEnd_Identification { get; set; }
        public string? DET_Debtor_IBAN { get; set; }
        public string? UETR { get; set; }
        public string? DET_Debtor_Name { get; set; }
        public string? DET_Creditor_IBAN { get; set; }
        public string? DET_Creditor_Name { get; set; }
        public string? DET_Creditor_Institution_Identification { get; set; }
        public string? DET_Debtor_Institution_Identification { get; set; }
        public string? DET_Active_Currency { get; set; }
        public string? HDR_IntrBkSttlmDt { get; set; }
        public decimal? DET_Interbank_Settlement_Amount { get; set; }
        public string? DET_Category_Purpose_Code { get; set; }
        public string? DET_Remittance_Information { get; set; }       
        public string? DET_Charge_Bearer { get; set; }

    }
}
