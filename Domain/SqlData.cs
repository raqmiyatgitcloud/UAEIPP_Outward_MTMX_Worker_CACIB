using Dapper;
using Microsoft.Extensions.Options;
using Raqmiyat.Framework.Model;
using Raqmiyat.Framework.NLogService;
using System.Data;
using UAEIPP_Outward_MTMX_Worker.Model;

namespace Raqmiyat.Framework.Domain
{
    public class SqlData
    {
        private readonly MTtoMXConversionWorkerLog _logger;
        private readonly IOptions<ServiceParams> _serviceParams;
        private readonly IOptions<StoredProcedureParams> _storedProcedureParams;
        private readonly IDbConnection _idbConnection;

        public SqlData(MTtoMXConversionWorkerLog logger, IOptions<ServiceParams> serviceParams, IOptions<StoredProcedureParams> storedProcedureParams, IDbConnection idbConnection)
        {
            _logger = logger;
            _serviceParams = serviceParams;
            _storedProcedureParams = storedProcedureParams;
            _idbConnection = idbConnection;
        }
        public async Task<List<SwiftMessage>> GetAsync()
        {
            _logger.Info("SqlData", "GetAsync", $"Started.");
            List<SwiftMessage> swiftMessages = [];
            try
            {
                var parameters = new DynamicParameters();
                var reader = await _idbConnection.QueryMultipleAsync(_storedProcedureParams.Value.GetAsync!, parameters, commandType: CommandType.StoredProcedure, transaction: null, commandTimeout: _serviceParams.Value.CommandTimeout);
                if (reader != null)
                {
                    try
                    {
                        swiftMessages = reader.Read<SwiftMessage>().ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("SqlData", "GetAsync", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("SqlData", "GetAsync", ex.Message);
            }
            _logger.Info("SqlData", "GetAsync", $"Completed. List Count:{swiftMessages.Count}");
            return swiftMessages!;
        }

        public async Task<List<SwiftMessage>> GetPacsMessageAsync()
        {
            _logger.Info("SqlData", "GetPacsMessageAsync", $"Started.");
            List<SwiftMessage> pacsMessages = [];
            try
            {
                var parameters = new DynamicParameters();
                var reader = await _idbConnection.QueryMultipleAsync(_storedProcedureParams.Value.GetPacsMessageAsync!, parameters, commandType: CommandType.StoredProcedure, transaction: null, commandTimeout: _serviceParams.Value.CommandTimeout);
                if (reader != null)
                {
                    try
                    {
                        pacsMessages = (await reader.ReadAsync<SwiftMessage>()).ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("SqlData", "GetPacsMessageAsync", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("SqlData", "GetAsync", ex.Message);
            }
            _logger.Info("SqlData", "GetAsync", $"Completed. List Count:{pacsMessages.Count}");
            return pacsMessages!;
        }

        public async Task UpdateAsync(decimal? id, string status, string endToEndId, decimal? amount, string type, string filePath)
        {
            try
            {
                _logger.Info("SqlData", "UpdateAsync", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("@ID", id, DbType.Decimal);
                parameters.Add("@Status", status, DbType.String);
                parameters.Add("@EndToEndID", endToEndId, DbType.String);
                parameters.Add("@Amount", amount, DbType.Decimal);
                parameters.Add("@Type", type, DbType.String);
                parameters.Add("@FilePath", filePath, DbType.String);
                parameters.Add("@OutputStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 4000);

                await _idbConnection.QueryAsync<string>(_storedProcedureParams.Value.UpdateAsync!, parameters, commandTimeout: _serviceParams.Value.CommandTimeout, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("SqlData", "UpdateAsync", $"Completed. OutputStatus:{parameters.Get<string>("OutputStatus")}");
            }
            catch (Exception ex)
            {
                _logger.Error("SqlData", "UpdateAsync", ex.Message);
            }
        }

        public async Task<List<MasterTableList>> GetMasterAsync()
        {
            _logger.Info("SqlData", "GetMasterAsync", $"Started.");
            var masterTableList = new List<MasterTableList>();
            try
            {
                var parameters = new DynamicParameters();
                var reader = await _idbConnection.QueryMultipleAsync(_storedProcedureParams.Value.GetMasterTableAsync!, parameters, commandTimeout: _serviceParams.Value.CommandTimeout, commandType: CommandType.StoredProcedure, transaction: null);
                if (reader != null)
                {
                    try
                    {
                        masterTableList = reader.Read<MasterTableList>().ToList();
                        reader.Dispose();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("SqlData", "GetMasterAsync", ex.Message);
            }
            _logger.Info("SqlData", "GetMasterAsync", $"GetMasterAsync is done.");
            return masterTableList;
        }
        public async Task SaveBatchPaymentAsync(DataTable dataTable)
        {
            try
            {
                _logger.Info("SqlData", "SaveBatchPaymentAsync", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("paymentDT", dataTable, DbType.Object);
                parameters.Add("OutputStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
                await _idbConnection.QueryAsync<string>(_storedProcedureParams.Value.SaveBatchPaymentAsync!, parameters, commandTimeout: _serviceParams.Value.CommandTimeout, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("SqlData", "SaveBatchPaymentAsync", $"Completed. OutputStatus:{parameters.Get<string>("OutputStatus")}");
            }
            catch (Exception ex)
            {
                _logger.Info("SqlData", "SaveBatchPaymentAsync", ex.Message);
            }
        }
        public string  CheckEndtoEndIDExistsAsync(string EndtoEndID)
        {
            string result = string.Empty;
            try
            {
                _logger.Info("SqlData", "CheckEndtoEndIDExistsAsync", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("EndtoEndID", EndtoEndID, DbType.String);
                parameters.Add("OutputStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
                 _idbConnection.Query<string>(_storedProcedureParams.Value.CheckEndtoEndIDExists!, parameters, commandTimeout: _serviceParams.Value.CommandTimeout, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("SqlData", "CheckEndtoEndIDExistsAsync", $"Completed. OutputStatus:{parameters.Get<string>("OutputStatus")}");
                result = parameters.Get<string>("OutputStatus");
            }
            catch (Exception ex)
            {
                _logger.Info("SqlData", "CheckEndtoEndIDExists", ex.Message);
            }
            return result;
        }
        public bool CheckValidIban(string Iban)
        {
            string bankcode = _serviceParams.Value.BankCode!;
            try
            {
                _logger.Info("SqlData", "CheckValidIban", "Started.");

                var parameters = new DynamicParameters();
                parameters.Add("IBAN_No", Iban, DbType.String);
                parameters.Add("bankcode", bankcode, DbType.String);

                var result = _idbConnection.QuerySingleOrDefault<dynamic>(
                    _storedProcedureParams.Value.CheckValidIban!,
                    parameters,
                    commandTimeout: _serviceParams.Value.CommandTimeout,
                    commandType: CommandType.StoredProcedure,
                    transaction: null
                );

                _logger.Info("SqlData", "CheckValidIban", $"Stored procedure result: {result.Result}, {result.ErrMsg}");

                if (result.Result == 1)
                {
                    _logger.Info("SqlData", "CheckValidIban", "IBAN is valid.");
                    return true;
                }
                else
                {
                    _logger.Info("SqlData", "CheckValidIban", "IBAN is invalid: " + result.ErrMsg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("SqlData", "CheckValidIban", $"Exception: {ex.Message}");
                return false; // Return false if an exception occurs
            }
        }


        public async Task SaveEmailAsync(EmailParams emailParams, string SenderReference)
        {
            try
            {
                _logger.Info("SqlData", "SaveEmailAsync", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("mail_ref_no", emailParams.RefNbr, DbType.String);
                parameters.Add("mail_module", emailParams.Module, DbType.String);
                parameters.Add("mail_type", emailParams.Type, DbType.String);
                parameters.Add("mail_description", emailParams.Description, DbType.String);
                parameters.Add("SwiftReferenceNbr", SenderReference, DbType.String);
                parameters.Add("OutputStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
                await _idbConnection.QueryAsync<string>(_storedProcedureParams.Value.SaveEmailAsync!, parameters, commandTimeout: _serviceParams.Value.CommandTimeout, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("SqlData", "SaveEmailAsync", $"Completed. OutputStatus:{parameters.Get<string>("OutputStatus")}");
            }
            catch (Exception ex)
            {
                _logger.Info("SqlData", "SaveEmailAsync", ex.Message);
            }

        }
        public async Task<string> SaveRepairQueue(PacsMessage pacsMessage)
        {
            _logger.Info("SqlData", "SaveRepairQueue", $"Started.");
            var errorMessageList = pacsMessage.errorMessages;
            var returnValue = string.Empty;
            try
            {

               
                var parameters = new DynamicParameters();
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("SwiftID", typeof(int));
                dataTable.Columns.Add("Amount", typeof(decimal));
                dataTable.Columns.Add("SenderReference", typeof(string));
                dataTable.Columns.Add("SenderAccountNumber", typeof(string));
                dataTable.Columns.Add("ReceiverAccountNumber", typeof(string));
                dataTable.Columns.Add("ValueDate", typeof(string));
                dataTable.Columns.Add("InstructionCode", typeof(string));
                dataTable.Columns.Add("TranType", typeof(string));
                dataTable.Columns.Add("Currency", typeof(string));
                dataTable.Columns.Add("SenderNameAndAddress", typeof(string));
                dataTable.Columns.Add("DebtorInstitution", typeof(string));
                dataTable.Columns.Add("SenderCorrespondentBank", typeof(string));
                dataTable.Columns.Add("ReceiverCorrespondentBank", typeof(string));
                dataTable.Columns.Add("CreditorInstitution", typeof(string));
                dataTable.Columns.Add("ReceiverNameAndAddress", typeof(string));
                dataTable.Columns.Add("RemittanceInformation", typeof(string));
                dataTable.Columns.Add("DetailsOfCharges", typeof(string));
                dataTable.Columns.Add("SenderToReceiverInformation", typeof(string));
                dataTable.Columns.Add("Error_Code", typeof(string));
                dataTable.Columns.Add("Error_Description", typeof(string));
                dataTable.Columns.Add("Created_By", typeof(string));

                //foreach (var modelDetails in pacsMessage)
                //{
                DataRow dataRow = dataTable.NewRow();
                dataRow["SwiftID"] = pacsMessage.SwiftID;
                dataRow["Amount"] = pacsMessage.InterbankSettlementAmount;
                dataRow["SenderReference"] = pacsMessage.SenderReference;
                dataRow["SenderAccountNumber"] = pacsMessage.SenderAccountNumber;
                dataRow["ReceiverAccountNumber"] = pacsMessage.ReceiverAccountNumber;
                dataRow["ValueDate"] = pacsMessage.ValueDate;
                dataRow["InstructionCode"] = pacsMessage.InstructionCode;
                dataRow["TranType"] = pacsMessage.TranType;
                dataRow["Currency"] = pacsMessage.Currency;
                dataRow["SenderNameAndAddress"] = pacsMessage.SenderNameAndAddress;
                dataRow["DebtorInstitution"] = pacsMessage.DebtorInstitution;
                dataRow["SenderCorrespondentBank"] = pacsMessage.SenderCorrespondentBank;
                dataRow["ReceiverCorrespondentBank"] = pacsMessage.ReceiverCorrespondentBank;
                dataRow["CreditorInstitution"] = pacsMessage.CreditorInstitution;
                dataRow["ReceiverNameAndAddress"] = pacsMessage.ReceiverNameAndAddress;
                dataRow["RemittanceInformation"] = pacsMessage.RemittanceInformation;
                dataRow["DetailsOfCharges"] = pacsMessage.DetailsOfCharges;
                dataRow["SenderToReceiverInformation"] = pacsMessage.SenderToReceiverInformation;
                List<string> errorCodes = new List<string>();
                List<string> errorDescriptions = new List<string>();
                foreach (var error in errorMessageList!)
                {
                    errorCodes.Add(error.Code!);
                    errorDescriptions.Add(error.Description!);
                }
                dataRow["Error_Code"] = string.Join("|", errorCodes);
                dataRow["Error_Description"] = string.Join("|", errorDescriptions);
                dataRow["Created_By"] ="System" ;
                dataTable.Rows.Add(dataRow);
                // }

                parameters.Add("@RepairQueue", dataTable, DbType.Object); 
                parameters.Add("@OutputStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
                await _idbConnection.QueryAsync<string>(_storedProcedureParams.Value.SaveRepairQueue!, parameters, commandTimeout: _serviceParams.Value.CommandTimeout, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("SqlData", "SaveRepairQueue", $"Completed.");

            }
            catch (Exception ex)
            {
                _logger.Info("SqlData", "SaveRepairQueue", ex.Message);
            }
            return returnValue;
        }
        public async Task <RepairQueueMessage> GetUpdatedRepairQueue()
        {
            _logger.Info("SqlData", "GetUpdatedRepairQueue", "Started.");

            RepairQueueMessage repairQueueMessages = null!;

            try
            {
                var parameters = new DynamicParameters();
                var reader = await _idbConnection.QueryMultipleAsync(
                    _storedProcedureParams.Value.GetUpdatedRepairQueue!,
                    parameters,
                    commandTimeout: _serviceParams.Value.CommandTimeout,
                    commandType: CommandType.StoredProcedure,
                    transaction: null);

                if (reader != null)
                {
                    try
                    {

                        repairQueueMessages = reader.Read<RepairQueueMessage>().FirstOrDefault()!;
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("SqlData", "GetUpdatedRepairQueue", ex.Message);              
                throw;
            }
            _logger.Info("SqlData", "GetUpdatedRepairQueue", "GetUpdatedRepairQueue is done.");
            return repairQueueMessages!;
        }

    }
}
