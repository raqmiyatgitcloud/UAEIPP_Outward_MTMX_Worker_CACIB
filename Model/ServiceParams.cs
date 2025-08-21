namespace Raqmiyat.Framework.Model
{
	public class ServiceParams
	{
		public int WorkerIntervalInMilliSeconds { get; set; }
        public int WorkerIntervalInSeconds { get; set; }
        public int CommandTimeout { get; set; }
        public string? ThresholdEmailDescription { get; set; }
        public string? OnboardEmailDescription { get; set; }
        public string? BankCode { get; set; }
        public string? APIURL { get; set; }
        public string? IBanEnquiryURL { get; set; }
        public bool EmailIsEnable { get; set; }
        public bool CBIBANEnquiry { get; set; }

    }
}
