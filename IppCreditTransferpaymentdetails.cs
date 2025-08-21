using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UAEIPP_Outward_MTMX_Worker
{
    [Table("IPP_CreditTransfer_payment_details")]
    public class IppCreditTransferpaymentdetails
    {
        public string? Instruction_Identification { get; set; }
        [Key]
        public string? EndToEnd_Identification { get; set; }
        public string? Transaction_Identification { get; set; }
        public string? UETR { get; set; }
        public string? Payment_Mode { get; set; }
        public string? Active_Currency { get; set; }
        public string? Debtor_Category_Purpose_Code { get; set; }
        public string? Debtor_Institution_Identification { get; set; }
        public string? Debtor_Name { get; set; }
        public decimal Debtor_Interbank_Settlement_Amount { get; set; }
        public string? Debtor_Economic_Activity_Code { get; set; }
        public string? Debtor_Identity_Number { get; set; }
        public string? Debtor_Identity_Type { get; set; }
        public string? Debtor_BirthDate { get; set; }
        public string? Debtor_CityOfBirth { get; set; }
        public string? Debtor_CountryOfBirth { get; set; }
        public string? Debtor_IBAN { get; set; }
        public string? Debtor_Account_Type { get; set; }
        public string? Debtor_Mobile { get; set; }
        public string? Debtor_Purpose_Of_Payment { get; set; }
        public string? Debtor_Issuer_Type_Code { get; set; }
        public string? Debtor_Trade_Licence_Number { get; set; }
        public string? Debtor_Emirate_Code { get; set; }
        public string? Debtor_Issuer { get; set; }
        public string? Debtor_Type_PVTorORG { get; set; }
        public string? Creditor_Issuer { get; set; }
        public string? Creditor_Name { get; set; }
        public string? Creditor_IBAN { get; set; }
        public string? Creditor_Institution_Identification { get; set; }
        public string? Creditor_Mobile { get; set; }
        public string? Acceptance_Time { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public DateTime ApprovedOn { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Payment_Status { get; set; }
        public string? Rejected_Reason { get; set; }
        public string? BATCH_REF_ID { get; set; }
        public string? Debit_Hold_Response { get; set; }
        public bool IsAuto { get; set; }
        public string? ValueDate { get; set; }
        public string? Remittance_Information { get; set; }

        public string? Core_Message_Identification { get; set; }
        public string? Core_Creation_DateTime { get; set; }

        public int Core_Number_Of_Transactions { get; set; }

        public string? Core_Settlement_Method { get; set; }
        public string? Core_Instructing_Agent_FI_ID { get; set; }
        public string? Core_Instructed_Agent_FI_ID { get; set; }

        public decimal SwiftID { get; set; }


    }
}
