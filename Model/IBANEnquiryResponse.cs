namespace NPSSWEBAPI.Models
{
    public class IBANEnquiryResponse
    {
        public string? CustomerName { get; set; }
        public string? CustomerAccountNumber { get; set; }
        public string? EmiratesCode { get; set; }
        public string? IssuerTypeCode { get; set; }
        public string? TradeLicenseNumber { get; set; }
        public string? EonomicAcitvityCode { get; set; }
        public string? OrganisationId { get; set; }
        public string? DebtorAccountType { get; set; }
    }   
}
