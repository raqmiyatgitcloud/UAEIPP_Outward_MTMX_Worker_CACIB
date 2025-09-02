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
using UAEIPP_Outward_MTMX_Worker.Model;

namespace UAEIPP_Outward_MTMX_Worker.Worker
{
    public class RepairQueueWorker : BackgroundService
    {
        private readonly RepairQueueWorkerLog _logger;
        private readonly IOptions<ServiceParams> _serviceParams;
        private IDbConnection _idbConnection;
        private readonly ConnectCustom _connectCustom;
        private readonly SqlData _sqlData;
        private readonly Conversion _conversion;
        private readonly AppDbContext _appDbContext;
        private readonly MasterTableList _masterTableList;

        public RepairQueueWorker(RepairQueueWorkerLog logger, IOptions<ServiceParams> serviceParams, IDbConnection idbConnection, ConnectCustom connectCustom, SqlData sqlData, Conversion conversion, AppDbContext appDbContext, MasterTableList masterTableList)
        {
            _logger = logger;
            _serviceParams = serviceParams;
            _idbConnection = idbConnection;
            _connectCustom = connectCustom;
            _sqlData = sqlData;
            _conversion = conversion;
            _appDbContext = appDbContext;
            _masterTableList = masterTableList;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("RepairQueueWorker", "ExecuteAsync", $"Service Started at {DateTimeOffset.Now.ToString("o")}");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await InitializeAsync();
                    try
                    {
                        RepairQueueMessage repairQueueMessage = await _sqlData.GetUpdatedRepairQueue();
                        if (repairQueueMessage != null)
                        {
                            await SavePacs008RepairQueueBatchAsync(repairQueueMessage);
                        }
                        else 
                        {
                            _logger.Error("RepairQueueWorker", "ExecuteAsync", $"repairQueueMessage is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("RepairQueueWorker", "ExecuteAsync", $"Receiver Error: {ex.Message}");
                        await SaveEmailAsync("RepairQueueWorker", "RepairQueue", "RepairQueueWorker", "ExecuteAsync", "1000", ex.Message);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(_serviceParams.Value.WorkerIntervalInMilliSeconds), stoppingToken);
                }
            }
            catch(Exception ex)
            {
                await SaveEmailAsync("RepairQueueWorker", "RepairQueue", "RepairQueueWorker", "ExecuteAsync", "1000", ex.Message);
                _logger.Error("RepairQueueWorker", "ExecuteAsync", ex.Message);
            }
        }
        private async Task InitializeAsync()
        {
            _logger.Info("RepairQueueWorker", "InitializeAsync", $"Started.");
            try
            {
                await Task.Run(() =>
                {
                    if (_idbConnection == null)
                    {
                        _logger.Info("RepairQueueWorker", "Constructor", "Re-Initialize the DbConnection Connection.");
                        _idbConnection = _connectCustom!.GetSingletonIDbConnection(_logger._log);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error("RepairQueueWorker", "ExecuteAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                throw;
            }
            _logger.Info("RepairQueueWorker", "InitializeAsync", $"Completed.");
        }
        private async Task SavePacs008RepairQueueBatchAsync(RepairQueueMessage repairQueueMessage)
        {
            var response = new IBANEnquiryResponse();
            _logger.Info("RepairQueueWorker", "SavePacs008RepairQueueBatchAsync", "Invoked.");
            try
            {

                var IppBatchdetails = new IppCreditTransferpaymentdetails();
                 if (repairQueueMessage != null) 
                {
                    response = IbanEnquiry(repairQueueMessage.DET_Debtor_IBAN!);
                    IppBatchdetails.Instruction_Identification = repairQueueMessage.DET_EndToEnd_Identification;
                    IppBatchdetails.EndToEnd_Identification = repairQueueMessage.DET_EndToEnd_Identification;
                    IppBatchdetails.Transaction_Identification = repairQueueMessage.DET_EndToEnd_Identification;
                    IppBatchdetails.UETR = Guid.NewGuid().ToString();
                    IppBatchdetails.Payment_Mode = "BATC";
                    IppBatchdetails.Active_Currency = repairQueueMessage.DET_Active_Currency;
                    IppBatchdetails.Debtor_Category_Purpose_Code = repairQueueMessage.DET_Category_Purpose_Code;
                    IppBatchdetails.Debtor_Institution_Identification = repairQueueMessage.DET_Debtor_Institution_Identification;
                    IppBatchdetails.Debtor_Name = repairQueueMessage.DET_Debtor_Name;
                    IppBatchdetails.Debtor_Interbank_Settlement_Amount = Convert.ToDecimal(repairQueueMessage.DET_Interbank_Settlement_Amount);
                    IppBatchdetails.Debtor_Economic_Activity_Code = response.EonomicAcitvityCode;
                    IppBatchdetails.Debtor_IBAN = repairQueueMessage.DET_Debtor_IBAN;
                    IppBatchdetails.Debtor_Trade_Licence_Number = response.TradeLicenseNumber;
                    IppBatchdetails.Debtor_Account_Type = response.DebtorAccountType;
                    IppBatchdetails.Debtor_Issuer_Type_Code = response.IssuerTypeCode;
                    IppBatchdetails.Debtor_Emirate_Code = response.EmiratesCode;
                    IppBatchdetails.Debtor_Type_PVTorORG = "BOID";
                    IppBatchdetails.Debtor_Purpose_Of_Payment = "WEBI";
                    IppBatchdetails.Creditor_Name = repairQueueMessage.DET_Creditor_Name;
                    IppBatchdetails.Creditor_IBAN = repairQueueMessage.DET_Creditor_IBAN;
                    IppBatchdetails.Creditor_Institution_Identification = repairQueueMessage.DET_Creditor_Institution_Identification;
                    IppBatchdetails.Creditor_Mobile = "";
                    IppBatchdetails.Acceptance_Time = "";
                    IppBatchdetails.CreatedBy = "MttoMxConversionworker";
                    IppBatchdetails.CreatedOn = DateTime.Now;
                    IppBatchdetails.ApprovedOn = DateTime.Now;
                    IppBatchdetails.ModifiedOn = DateTime.Now;
                    IppBatchdetails.Payment_Status = "20";
                    IppBatchdetails.Debit_Hold_Response = string.Empty;
                    IppBatchdetails.IsAuto = true;
                    IppBatchdetails.SwiftID = Convert.ToDecimal(repairQueueMessage.SwiftID);
                    //IppBatchdetails.ValueDate = Convert.ToDateTime(repairQueueMessage.HDR_IntrBkSttlmDt).ToString();
                    DateTime dateTime = Convert.ToDateTime(repairQueueMessage.HDR_IntrBkSttlmDt);
                    string formatted = dateTime.ToString("yyMMdd");
                    IppBatchdetails.ValueDate = formatted;
                    IppBatchdetails.Remittance_Information = repairQueueMessage.DET_Remittance_Information;
                    IppBatchdetails.Remittance_Information = repairQueueMessage.DET_Remittance_Information;
                    await _appDbContext._ippCreditTransferpaymentdetails.AddAsync(IppBatchdetails);
                    await _appDbContext.SaveChangesAsync();
                    _logger.Info("RepairQueueWorker", "SavePacs008RepairQueueBatchAsync", "Inserted Successfully Done");
                }
                
            }
            catch (Exception ex)
            {
                _logger.Error("RepairQueueWorker", "RepairQueueWorker", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
               
                await SaveEmailAsync("RepairQueueWorker", "RepairQueue", "RepairQueueWorker", "SavePacs008RepairQueueBatchAsync", "1000", ex.Message);
            }
        }
        private async Task SaveEmailAsync(string refno, string module, string classname, string method, string code, string desc)
        {
            try
            {
                _logger.Info("RepairQueueWorker", "SaveEmailAsync", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("@Mail_RefNo", refno, DbType.String);
                parameters.Add("@Mail_Module", module, DbType.String);
                parameters.Add("@Mail_ServiceName", "UAEIPP_Outward_MTMX_Worker", DbType.String);

                parameters.Add("@Mail_ClassName", classname, DbType.String);
                parameters.Add("@Mail_MethodName", method, DbType.String);
                parameters.Add("@Mail_ResponseCode", code, DbType.String);
                parameters.Add("@Mail_Description", desc, DbType.String);
                await _idbConnection.QueryAsync<string>("IPP_Insert_sp_SendMail", parameters, commandTimeout: 1200, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("RepairQueueWorker", "SaveEmailAsync", $"SaveEmailAsync Data is Inserted.");

            }
            catch (Exception ex)
            {
                _logger.Info("RepairQueueWorker", "SaveEmailAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }

        }

        public IBANEnquiryResponse IbanEnquiry(string iban)
        {
            var request = new RequestObject();
            var enquiryresponse = new IBANEnquiryResponse();
            try
            {
                _logger.Info("Conversion", "IbanEnquiry", $"Started.");
                var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; } };
                var httpClient = new HttpClient(handler);

                using (var response = httpClient.GetAsync(_serviceParams.Value.APIURL + "/api/DataValidate/IBANEnquiry?IBAN_NO=" + iban))
                {
                    _logger.Info($"Conversion", "IbanEnquiry", $"Internal Api Response call");
                    string apiResponse = response.Result.Content.ReadAsStringAsync().Result.ToString();
                    enquiryresponse = JsonConvert.DeserializeObject<IBANEnquiryResponse>(apiResponse)!;
                    _logger.Info($"Conversion", "IbanEnquiry", $"Response: $ TradeLicenseNumber:  {enquiryresponse.TradeLicenseNumber}, " +
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

                    using (var response2 = httpClient.PostAsync(_serviceParams.Value.IBanEnquiryURL + "/api/IbanEnquiry/CbwsiPost", byteContent))
                    {
                        {
                            string apiResponse2 = response2.Result.Content.ReadAsStringAsync().Result.ToString();
                            var Cbresponse = JsonConvert.DeserializeObject<ResponseDetail>(apiResponse2)!;
                            if (Cbresponse != null && Cbresponse.ResponseList != null)
                            {
                                enquiryresponse.TradeLicenseNumber = Cbresponse.ResponseList.Response!.WsResponse!.Id!;
                                enquiryresponse.CustomerName = Cbresponse.ResponseList.Response.WsResponse.Title;
                                _logger.Info($"Conversion", "IbanEnquiry", $"CBResponse: ResponseMessage:{Cbresponse!.RespStat!.ResponseMessage}, +   $TradeLicenseNumber:{enquiryresponse.TradeLicenseNumber}, " +
       $"CustomerName: {enquiryresponse.CustomerName}");

                            }
                        }
                    }
                }
                _logger.Info("Conversion", "IbanEnquiry", $"Completed.");
            }

            catch (Exception ex)
            {
                _logger.Error("Conversion", "IbanEnquiry", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
            return enquiryresponse;
        }

    }
}

