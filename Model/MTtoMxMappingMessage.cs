namespace Raqmiyat.Framework.Model
{
    public class PacsMessage
    {
        public int? SwiftID { get; set; }
        public string? MessageType { get; set; }
        public string? SenderReference { get; set; }
        public int NoofTransaction { get; set; }
        public string? SenderAccountNumber { get; set; }
        public string? Txnid { get; set; }
        public string? UETR { get; set; }
        public string? SenderName { get; set; }
        public string? SenderNameAndAddress { get; set; }
        public string? ReceiverAccountNumber { get; set; }
        public string? ReceiverNameAndAddress { get; set; }
        public string? DebtorInstitution { get; set; }
        public string? CreditorInstitution { get; set; }
        public string? InstructingReimbursementAgent { get; set; }
        public string? EndToEndID { get; set; }

        public string? InstructedReimbursementAgent { get; set; }
        public string? ThirdReimbursementAgent { get; set; }
        public string? IntermediaryBankInformation { get; set; }

        public string? BeneficiaryAccountNumber { get; set; }
        public string? BeneficiaryNameAndAddress { get; set; }

        public string? Currency { get; set; }
        public string? ValueDate { get; set; }
        public decimal? InterbankSettlementAmount { get; set; }
        public decimal? InstructedAmount { get; set; }
        public string? SenderCorrespondentBank { get; set; }
        public string? ReceiverCorrespondentBank { get; set; }
        public string? OrderingCustomer { get; set; }
        public string? DetailsOfCharges { get; set; }
        public string? RemittanceInformation { get; set; }
        public string? SenderToReceiverInformation { get; set; }
        public string? InstructionCode { get; set; }
        public string? TranType { get; set; }
        public string? RegulatoryReporting { get; set; }
        public DateTime DateOfIssue { get; set; }
        public string? MessagePriority { get; set; }
        public string? NonBankIssuer { get; set; }
        public string? FilePath { get; set; }
        public string? BankOperationCode { get; set; }
        public string? Status { get; set; }
        public string? MessageId { get; set; }
        public string? CreationDatetime { get; set; }  
        public int NumberOfTransactions { get; set; }
        public string? SttlmMtd { get; set;}
        public string? InstructingAgent { get; set; }
        public string? InstructedAgent { get; set; }
        public List<ErrorMessage>? errorMessages  { get; set; }
    }
    public class ErrorMessage
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
}
