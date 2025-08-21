namespace Raqmiyat.Framework.Model
{
	public class StoredProcedureParams
	{
		public string? GetAsync { get; set; }
        public string? GetPacsMessageAsync { get; set; }
        public string? UpdateAsync { get; set; }
        public string? SaveBatchPaymentAsync { get; set; }
        public string? GetMasterTableAsync { get; set; }
        public string? SaveEmailAsync { get; set; }
        public string? SaveRepairQueue { get; set; }
        public string? GetUpdatedRepairQueue { get; set; }
        public string? CheckEndtoEndIDExists { get; set; }
        public string? CheckValidIban { get; set; }
      
    }
}
