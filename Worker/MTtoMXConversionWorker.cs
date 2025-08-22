using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NPSSWEBAPI.Models;
using Raqmiyat.Framework.Custom;
using Raqmiyat.Framework.Domain;
using Raqmiyat.Framework.Model;
using Raqmiyat.Framework.Model.ServiceResponse;
using Raqmiyat.Framework.NLogService;
using System.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;



namespace UAEIPP_Outward_MTMX_Worker
{
    public class MTtoMXConversionWorker : BackgroundService
    {
        private readonly MTtoMXConversionWorkerLog _logger;
        private readonly IOptions<ServiceParams> _serviceParams;
        private IDbConnection _idbConnection;
        private readonly ConnectCustom _connectCustom;
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient _httpClient;
        private readonly SqlData _sqlData;
        private readonly Conversion _conversion;
        private readonly AppDbContext _appDbContext;
        private readonly MasterTableList _masterTableList;

        public MTtoMXConversionWorker(MTtoMXConversionWorkerLog logger, IOptions<ServiceParams> serviceParams, IDbConnection idbConnection, ConnectCustom connectCustom, SqlData sqlData, Conversion conversion, AppDbContext appDbContext, MasterTableList masterTableList,HttpClient httpClient,IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _serviceParams = serviceParams;
            _idbConnection = idbConnection;
            _connectCustom = connectCustom;
            _sqlData = sqlData;
            _httpClient = httpClient;
            _clientFactory = httpClientFactory;
            _conversion = conversion;
            _appDbContext = appDbContext;
            _masterTableList = masterTableList;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("MTtoMXConversionWorker", "ExecuteAsync", $"Service Started at {DateTimeOffset.Now.ToString("o")}");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await InitializeAsync();
                    try
                    {
                        var swiftMessages = await _sqlData.GetAsync();
                        //var masterTableList = await _sqlData.GetMasterAsync();
                        List<BankOnBoard> bankOnBoardList = new();
                        if (swiftMessages.Any() && _masterTableList != null)
                        {
                            var pacsMessages = await _conversion.TransformMTToMXAsync(swiftMessages);
                            if (pacsMessages.Any())
                            {
                                var errorPacsMessages = pacsMessages.Where(r => r.Status!.ToUpper() == "ERROR").ToList();
                                if (errorPacsMessages.Any())
                                {
                                    var pacsMessagesCopy = errorPacsMessages.ToList();
                                    foreach (var pacsMessage in errorPacsMessages)
                                    {                                   
                                        await _sqlData.SaveRepairQueue(pacsMessage);
                                        await _sqlData.UpdateAsync(pacsMessage.SwiftID, "ME", pacsMessage.SenderReference!, pacsMessage.InterbankSettlementAmount, "REPAIRQUEUE", string.Empty);
                                        //await SavePacs008RepairBatchAsync(repairQueueMessage);
                                        //ToDo:Capture error list in ipp_corebank_receiver_error table.
                                        pacsMessages.Remove(pacsMessage);
                                    }
                                }

                                //Verify the transaction threshold amount.
                                if (_masterTableList.thresholdAmount!.IPP_Max_Credit_Amount > 0)
                                {
                                    var txnThresholdAmount = _masterTableList.thresholdAmount!.IPP_Max_Credit_Amount;
                                    if (txnThresholdAmount > 0)
                                    {
                                        var aboveThresholdsAmountMessages = pacsMessages.Where(r => r.InterbankSettlementAmount > txnThresholdAmount).ToList();
                                        if (aboveThresholdsAmountMessages.Any())
                                        {
                                            foreach (var pacsMessage in aboveThresholdsAmountMessages)
                                            {
                                                EmailParams emailParams = new() { RefNbr = Convert.ToString(pacsMessage.SwiftID), Module = "OUTWARD", Type = "AUTO", Description = _serviceParams.Value.ThresholdEmailDescription };
                                                await _sqlData.SaveEmailAsync(emailParams, pacsMessage.SenderReference!);
                                                await _sqlData.UpdateAsync(pacsMessage.SwiftID, "ME", pacsMessage.SenderReference!, pacsMessage.InterbankSettlementAmount, "EMAIL", string.Empty);
                                                pacsMessages.Remove(pacsMessage);
                                            }
                                        }
                                    }
                                }

                                //Swift code not in Master table.
                                bankOnBoardList = _masterTableList.bankOnBoardList!;
                                if (bankOnBoardList.Any() && pacsMessages.Any())
                                {

                                    foreach (var pacsMessage in pacsMessages.ToList())
                                    {
                                        var isValid = true;
                                        if (pacsMessage.CreditorInstitution!.Length == 11)
                                        {
                                            if (!bankOnBoardList.Any(r => r.SwiftCode == pacsMessage.CreditorInstitution!))
                                            {
                                                isValid = false;
                                            }
                                        }
                                        else
                                        {
                                           // var swiftCode = pacsMessage.CreditorInstitution!.Substring(0, pacsMessage.CreditorInstitution!.Length - 3);
                                            var swiftCode = pacsMessage.CreditorInstitution!;
                                            if (!bankOnBoardList.Any(r =>
                                                                    string.Equals(r.SwiftCode?.Trim(), swiftCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                                            {
                                                isValid = false;
                                            }
                                            else
                                            {
                                                isValid = true;
                                            }
                                        }
                                        if (!isValid)
                                        {
                                            EmailParams emailParams = new() { RefNbr = Convert.ToString(pacsMessage.SwiftID), Module = "OUTWARD", Type = "AUTO", Description = _serviceParams.Value.OnboardEmailDescription };
                                            await _sqlData.SaveEmailAsync(emailParams, pacsMessage.SenderReference!);
                                            await _sqlData.UpdateAsync(pacsMessage.SwiftID, "ME", pacsMessage.SenderReference!, pacsMessage.InterbankSettlementAmount, "EMAIL", string.Empty);
                                            pacsMessages.Remove(pacsMessage);
                                        }

                                    }
                                }
                                //Valid payment
                                if (pacsMessages.Any())
                                {
                                    foreach (var pacsMessage in pacsMessages)
                                    {
                                        try
                                        {
                                            await SavePacs008BatchAsync(pacsMessage);
                                            await _sqlData.UpdateAsync(pacsMessage.SwiftID, "MP", pacsMessage.SenderReference!, pacsMessage.InterbankSettlementAmount, "IPP", string.Empty);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.Error("MTtoMXConversionWorker", "Processing Message", ex.Message);
                                            _logger.Error("MTtoMXConversionWorker", "Processing Message", $"Inner Exception: {ex.InnerException}");
                                        }
                                    
                                    }
                                    //ToDo: we have commented as payment should go as batch.
                                    //var dataTable = await _conversion.ConvertToDataTableAsync(pacsMessages);
                                    //await _sqlData.SaveBatchPaymentAsync(dataTable);
                                }

                            }
                            else
                            {

                                await Task.Delay(TimeSpan.FromMilliseconds(_serviceParams.Value.WorkerIntervalInSeconds), stoppingToken);
                            }
                        }
                        //else
                        //{
                        //     await _conversion.TransformMXtoMXAsync(swiftMessages);
                        //}
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("MTtoMXConversionWorker", "ExecuteAsync", $"MQ Receiver Error: {ex.Message}");
                        await SaveEmailAsync("MTtoMXConversionWorker", "MTtoMXConversion", "MTtoMXConversionWorker", "ExecuteAsync", "1000", ex.Message);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(_serviceParams.Value.WorkerIntervalInMilliSeconds), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("MTtoMXConversionWorker", "ExecuteAsync", ex.Message);
            }
        }
        private async Task InitializeAsync()
        {
            _logger.Info("MTtoMXConversionWorker", "InitializeAsync", $"Started.");
            try
            {
                await Task.Run(() =>
                   {
                       if (_idbConnection == null)
                       {
                           _logger.Info("MQListenerWorker", "InitializeAsync", "Re-Initialize the DbConnection Connection.");
                           _idbConnection = _connectCustom!.GetSingletonIDbConnection(_logger._log);
                       }
                   });
            }
            catch (Exception ex)
            {
                _logger.Error("MTtoMXConversionWorker", "InitializeAsync", ex.Message);
                throw;
            }
            _logger.Info("MTtoMXConversionWorker", "InitializeAsync", $"Completed.");
        }
        private async Task SavePacs008BatchAsync(PacsMessage pacsMessage)
        {
            var response = new IBANEnquiryResponse();
            _logger.Info("MTtoMXConversionWorker", "SavePacs008BatchAsync", "Started.");
            try
            {
                var IppBatchdetails = new IppCreditTransferpaymentdetails();
                response = await IbanEnquiry(pacsMessage.SenderAccountNumber!, pacsMessage);
                IppBatchdetails.Instruction_Identification = pacsMessage.SenderReference;
                IppBatchdetails.EndToEnd_Identification = pacsMessage.SenderReference;
                IppBatchdetails.Transaction_Identification = pacsMessage.SenderReference;
                IppBatchdetails.UETR = Guid.NewGuid().ToString();
                IppBatchdetails.Payment_Mode = "BATC";
                IppBatchdetails.Active_Currency = pacsMessage.Currency;
                IppBatchdetails.Debtor_Category_Purpose_Code = pacsMessage.TranType;
                IppBatchdetails.Debtor_Institution_Identification = pacsMessage.DebtorInstitution;
                IppBatchdetails.Debtor_Name = response.CustomerName;
                IppBatchdetails.Debtor_Interbank_Settlement_Amount = Convert.ToDecimal(pacsMessage.InterbankSettlementAmount);
                IppBatchdetails.Debtor_Economic_Activity_Code = response.EonomicAcitvityCode;
                IppBatchdetails.Debtor_Trade_Licence_Number = response.TradeLicenseNumber;
                IppBatchdetails.Debtor_IBAN = pacsMessage.SenderAccountNumber;
                IppBatchdetails.Debtor_Account_Type = response.DebtorAccountType;
                IppBatchdetails.Debtor_Issuer_Type_Code = response.IssuerTypeCode;
                IppBatchdetails.Debtor_Emirate_Code = response.EmiratesCode;
                IppBatchdetails.Debtor_Type_PVTorORG = "BOID";
                IppBatchdetails.Debtor_Purpose_Of_Payment = "WEBI";
                IppBatchdetails.Creditor_Name = pacsMessage.ReceiverNameAndAddress;
                IppBatchdetails.Creditor_IBAN = pacsMessage.ReceiverAccountNumber;
                IppBatchdetails.Creditor_Institution_Identification = pacsMessage.CreditorInstitution;
                IppBatchdetails.Creditor_Mobile = "";
                IppBatchdetails.Acceptance_Time = "";
                IppBatchdetails.CreatedBy = "MttoMxConversionworker";
                IppBatchdetails.CreatedOn = DateTime.Now;
                IppBatchdetails.ApprovedOn = DateTime.Now;
                IppBatchdetails.ModifiedOn = DateTime.Now;
                IppBatchdetails.Payment_Status = "20";
                IppBatchdetails.Debit_Hold_Response = string.Empty;
                IppBatchdetails.IsAuto = true;
                IppBatchdetails.SwiftID = Convert.ToDecimal(pacsMessage.SwiftID);
                IppBatchdetails.ValueDate = pacsMessage.ValueDate!.ToString();
                IppBatchdetails.Remittance_Information = pacsMessage.RemittanceInformation;
                await _appDbContext._ippCreditTransferpaymentdetails.AddAsync(IppBatchdetails);
                await _appDbContext.SaveChangesAsync();
                _logger.Info("MTtoMXConversionWorker", "SavePacs008BatchAsync", "Inserted Successfully Done");
            }
          
            catch (Exception ex)
            {
                
                _logger.Error("MTtoMXConversionWorker", "SavePacs008BatchAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
               
                await SaveEmailAsync("MTtoMXConversionWorker", "MTtoMXConversion", "MTtoMXConversionWorker", "SavePacs008BatchAsync", "1000", ex.Message);
            }
        }
        public async Task<IBANEnquiryResponse> IbanEnquiry(string iban,PacsMessage pacsMessage)
        {
            var request = new RequestObject();
            var enquiryresponse = new IBANEnquiryResponse();
            try
            {
                _logger.Info("MTtoMXConversionWorker", "IbanEnquiry", $"Started.");
                var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; } };
                var httpClient = new HttpClient(handler);

                using (var response = httpClient.GetAsync(_serviceParams.Value.APIURL + "/api/DataValidate/IBANEnquiry?IBAN_NO=" + iban))
                {
                    string apiResponse = response.Result.Content.ReadAsStringAsync().Result.ToString();
                    enquiryresponse = JsonConvert.DeserializeObject<IBANEnquiryResponse>(apiResponse)!;
                    _logger.Info($"Conversion", "IbanEnquiry", $"Response: {enquiryresponse.TradeLicenseNumber}, " +
                   $"CustomerName: {enquiryresponse.CustomerName}, IssuerTypeCode: {enquiryresponse.IssuerTypeCode}, " +
                   $"EmiratesCode: {enquiryresponse.EmiratesCode}, DebtorAccountType: {enquiryresponse.DebtorAccountType}, " +
                   $"CustomerAccountNumber: {enquiryresponse.CustomerAccountNumber}, EconomicActivityCode: {enquiryresponse.EonomicAcitvityCode}");
                }
                if (_serviceParams.Value.CBIBANEnquiry)
                {
                    request.Iban = iban;
                    var myContent = JsonConvert.SerializeObject(request);
                    var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var result = await httpClient.PostAsync( _serviceParams.Value.IBanEnquiryURL + "/api/IbanEnquiry/CbwsiPost", byteContent);
                    if (result.IsSuccessStatusCode)
                    {
                        var responseDetail = await result.Content.ReadFromJsonAsync<ResponseDetail>();
                        if (responseDetail?.ResponseList != null)
                        {
                            if (responseDetail.RespStat?.ResponseCode == "000" && responseDetail.RespStat?.ResponseMessage == "success")
                            {
                                enquiryresponse!.TradeLicenseNumber = responseDetail.ResponseList.Response?.WsResponse?.Id;
                                enquiryresponse.CustomerName = responseDetail.ResponseList.Response?.WsResponse?.Title;
                                enquiryresponse.DebtorAccountType = responseDetail.ResponseList.Response?.WsResponse?.AccountType;
                            }
                            else
                            {
                                _logger.Info($"Conversion", "IbanEnquiry", "No valid data returned from the CB response. Enter the value manually.");
                            }
                        }
                        else
                        {
                            _logger.Info($"Conversion", "IbanEnquiry", "No valid data returned from the response.");
                        }
                    }
                    else
                    {
                        _logger.Info($"Conversion", "IbanEnquiry", "No response returned from the CB response.");
                    }
                }
                //              using (var response2 = httpClient.PostAsync(_serviceParams.Value.IBanEnquiryURL + "/api/IbanEnquiry/CbwsiPost", byteContent))
                //              {
                //                  {
                //                       string apiResponse2 = response2.Result.Content.ReadAsStringAsync().Result.ToString();
                //                       var Cbresponse = JsonConvert.DeserializeObject<ResponseDetail>(apiResponse2)!;
                //                      if (Cbresponse != null && Cbresponse.ResponseList != null)
                //                      {
                //                          enquiryresponse.TradeLicenseNumber = Cbresponse.ResponseList.Response!.WsResponse!.Id!;
                //                          enquiryresponse.CustomerName = Cbresponse.ResponseList.Response.WsResponse.Title;
                //                          _logger.Info($"Conversion", "IbanEnquiry", $"CBResponse: ResponseMessage:{Cbresponse!.RespStat!.ResponseMessage}, +   $TradeLicenseNumber:{enquiryresponse.TradeLicenseNumber}, " +
                //$"CustomerName: {enquiryresponse.CustomerName}");
                //                      }
                //                  }
                //              }
                _logger.Info("MTtoMXConversionWorker", "IbanEnquiry", $"Completed.");
            }
      
            catch (Exception ex)
            {
                await _sqlData.UpdateAsync(pacsMessage.SwiftID, "ENQ_FAIL", pacsMessage.SenderReference!, pacsMessage.InterbankSettlementAmount, "IBAN_ENQUIRY_FAILED", string.Empty);
                _logger.Error("MTtoMXConversionWorker", "IbanEnquiry", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
            return enquiryresponse;
        }
        private async Task SaveEmailAsync(string refno, string module, string classname, string method, string code, string desc)
        {
            try
            {
                _logger.Info("MTtoMXConversionWorker", "IbanEnquiry", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("@Mail_RefNo", refno, DbType.String);
                parameters.Add("@Mail_Module", module, DbType.String);
                parameters.Add("@Mail_ServiceName", "UAEIPP_Outward_MTMX_Worker", DbType.String);

                parameters.Add("@Mail_ClassName", classname, DbType.String);
                parameters.Add("@Mail_MethodName", method, DbType.String);
                parameters.Add("@Mail_ResponseCode", code, DbType.String);
                parameters.Add("@Mail_Description", desc, DbType.String);
                await _idbConnection.QueryAsync<string>("IPP_Insert_sp_SendMail", parameters, commandTimeout: 1200, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("MTtoMXConversionWorker", "SaveEmailAsync", $"SaveEmailAsync Data is Inserted.");

            }
            catch (Exception ex)
            {
                _logger.Info("MTtoMXConversionWorker", "SaveEmailAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
        }   
    }
}