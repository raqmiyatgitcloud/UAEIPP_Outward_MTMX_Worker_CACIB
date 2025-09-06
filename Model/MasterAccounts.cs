using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UAEIPP_Outward_MTMX_Worker.Model
{
    
    [Table("ipp_master_accounts")]
    public class MasterAccounts
    {
        [Key]
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAccountNumber { get; set; }
        public string? EmiratesCode { get; set; }
        public string? IssuerTypeCode { get; set; }
        public string? TradeLicenseNumber { get; set; }
        public string? EonomicAcitvityCode { get; set; }  
        public string? OrganisationId { get; set; }
        public string? DebtorAccountType { get; set; }
        public string? FileName { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public string? Status { get; set; }
        public string? CategoryPurposeCode { get; set; }
        public string? Action { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? CityOfBirth { get; set; }
        public string? CountryOfBirth { get; set; }
        public string? Identity_Type { get; set; }
        public string? Identity_Number { get; set; }
        public string? Mobile_Number { get; set; }
        public string? Email_ID { get; set; }
        public string? AccountStatus { get; set; }
        public string? PayerType_PVTorORG { get; set; }
    }
}
