using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NPSSWEBAPI.Models;
using Raqmiyat.Framework.Model;
using Raqmiyat.Framework.Model.Pacs008;
using Raqmiyat.Framework.Model.ServiceResponse;
using Raqmiyat.Framework.NLogService;
using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UAEIPP_Outward_MTMX_Worker;

namespace Raqmiyat.Framework.Domain
{
    public class Conversion
    {
        private readonly ConversionWorkerLog _logger;
        private readonly IOptions<ServiceParams> _serviceParams;
        private readonly IOptions<StoredProcedureParams> _storedProcedureParams;
        private readonly IDbConnection _idbConnection;
        private readonly MasterTableList _masterTableList;
        private readonly SqlData _sqlData;
        private readonly AppDbContext _appDbContext;
        public Conversion(ConversionWorkerLog logger, IOptions<ServiceParams> serviceParams, IOptions<StoredProcedureParams> storedProcedureParams, IDbConnection idbConnection, MasterTableList masterTableList, SqlData sqlData, AppDbContext appDbContext)
        {
            _logger = logger;
            _serviceParams = serviceParams;
            _storedProcedureParams = storedProcedureParams;
            _idbConnection = idbConnection;
            _masterTableList = masterTableList;
            _sqlData = sqlData;
            _appDbContext = appDbContext;
        }
        public async Task<List<PacsMessage>> TransformMTToMXAsync(List<SwiftMessage> swiftMessageList)
        {
            _logger.Info("Conversion", "TransformMTToMXAsync", $"Started.");
            List<PacsMessage> pacsMessages = [];


            await Task.Run(() =>
            {
                try
                {
                    if (swiftMessageList.Any())
                    {
                        foreach (var swiftMessage in swiftMessageList)
                        {
                            List<ErrorMessage> errorMessages = new List<ErrorMessage>();
                            try
                            {
                                _logger.Info("Conversion", "TransformMTToMXAsync", $"Looping Started.{swiftMessage.ID}");
                                PacsMessage mxMappingMessage = new();

                                Regex senderRefRegex = new Regex(@"20:(?<SenderRef>.+)");
                                Regex instructionCodeRegex = new Regex(@"23B:(?<InstructionCode>.{4})");
                                Regex TranTypeRegex = new Regex(@"26T:(?<TranType>.{3})");
                                Regex valueDateAmountRegex = new Regex(@"32A:(?<ValueDate>\d{6})(?<Currency>[A-Z]{3})((?<IntrBkSttlmAmt>\d+(?:,\d+)?))");
                                Regex amountRegex = new Regex(@"33B:(?<Currency>[A-Z]{3})((?<Amount>\d+(?:,\d+)?))");
                                Regex senderAccountRegex = new Regex(@"50[KFA]:(?<SenderAccount>.+)");
                                Regex senderNameAddressRegex = new Regex(@"50[KFA].*\r?\n(?<SenderNameAddress>[^\r\n]+)");
                                Regex DebtorInstitutionRegex = new Regex(@"52[AD]:(?<DebtorInstitution>.+?)\r\n");
                                Regex senderCorrespondentBankRegex = new Regex(@"53[ABD]:(?<SenderCorrespondentBank>.+)");
                                Regex receiverCorrespondentBankRegex = new Regex(@"54[ABD]:(?<ReceiverCorrespondentBank>.+)");
                                //Regex thirdReimbursementAgent = new Regex(@"55A:(?<ThrdRmbrsmntAgtAcct>.+)");
                                //Regex intermediaryBankRegex = new Regex(@"56:(?<IntermediaryBankInformation>.+)");
                                Regex CreditorInstitutionRegex = new Regex(@"57[ABCD]:(?<CreditorInstitution>.+?)\r\n");
                                // Regex CreditorInstitutionRegex = new Regex(@"(?:51A:(?<CreditorInstitution51A>.+?)\r\n)|(?:57[ABCD]:(?<CreditorInstitution57A>.+?)\r\n)");
                                Regex receiverAccountRegex = new Regex(@"59[A]:(?<ReceiverAccount>.+)");
                                Regex receiverNameAddressRegex = new Regex(@":59(?:[A-Z])?:.*\r?\n(?<ReceiverNameAddress>[^\r\n]+)");
                                Regex RemittanceInformationRegex = new Regex(@"70:(?<RltdRmtInf>[^:]+)");
                                Regex detailsOfChargesRegex = new Regex(@"71A:(?<DetailsOfCharges>.+)");
                                Regex senderToReceiverInfoRegex = new Regex(@"72:(?<senderToReceiverInfo>.+)");
                                //Regex regulatoryReportingRegex = new Regex(@"77B:(?<RegulatoryReporting>.+)");

                                int count = swiftMessageList.Count;
                                mxMappingMessage.NoofTransaction = count;
                                mxMappingMessage.SwiftID = swiftMessage.ID;

                                // Match sender's reference
                                Match match = senderRefRegex.Match(swiftMessage.Request!);
                                mxMappingMessage.SenderReference = match.Groups["SenderRef"].Value.Trim();
                                if (match.Success)
                                {
                                    if (match.Success)
                                    {
                                        mxMappingMessage.SenderReference = match.Groups["SenderRef"].Value.Trim();
                                    }
                                    //    if (_sqlData.CheckEndtoEndIDExistsAsync(match.Groups["SenderRef"].Value.Trim()) != "N")
                                    //{
                                    //    errorMessages.Add(new ErrorMessage() { Code = "EndtoEndId", Description = "SenderReference Already Exists" });
                                    //}
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "EndtoEndId", Description = "SenderReference Must Be In 16 Digit Number" });
                                }


                                // Match instruction code
                                match = instructionCodeRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.InstructionCode = match.Groups["InstructionCode"].Value.Trim();
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "1002", Description = "InstructionCode is empty" });
                                }

                                // Match CategoryPurpose code
                                match = TranTypeRegex.Match(swiftMessage.Request!);
                                mxMappingMessage.TranType = match.Groups["TranType"].Value.Trim();
                                if (match.Success)
                                {
                                    if (!_masterTableList.categoryPurposeCode!.Exists(r => r.Code_Value == mxMappingMessage.TranType && r.IsApplicable == "Y"))
                                    {
                                        errorMessages.Add(new ErrorMessage() { Code = "CatPrCd", Description = "TranType Is Not Applicable" });
                                    }
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "CatPrCd", Description = "TranType Syntax Error" });
                                }
                                // Match interbank  amount
                              
                                match = valueDateAmountRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    string dateStr = match.Groups["ValueDate"].Value.Trim();
                                    mxMappingMessage.Currency = match.Groups["Currency"].Value.Trim();
                                    mxMappingMessage.ValueDate = dateStr;
                                    if (DateTime.TryParseExact(dateStr, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                                    {
                                        // date = date.AddYears(2000); // Adjusting the year
                                        if (dateStr == DateTime.Now.ToString("yyMMdd"))
                                        {
                                            mxMappingMessage.ValueDate = dateStr;
                                        }
                                        else
                                        {
                                            errorMessages.Add(new ErrorMessage() { Code = "StlDt", Description = "Invalid ValueDate" });
                                        }
                                    }
                                    else
                                    {
                                        errorMessages.Add(new ErrorMessage() { Code = "StlDt", Description = "Invalid ValueDate" });
                                    }
                                    string IntrBkSttlmAmt = match.Groups["IntrBkSttlmAmt"].Value.Replace(",", "");
                                    if (decimal.TryParse(IntrBkSttlmAmt, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount))
                                    {
                                        mxMappingMessage.InterbankSettlementAmount = amount;
                                    }
                                    else
                                    {
                                        errorMessages.Add(new ErrorMessage() { Code = "1005", Description = "IntrBkSttlmAmt is empty" });
                                    }
                                }
                                // Match payment amount
                                if (amountRegex != null)
                                {
                                    match = amountRegex.Match(swiftMessage.Request!);
                                    if (match.Success)
                                    {
                                        mxMappingMessage.Currency = match.Groups["Currency"].Value.Trim();
                                        string amountStr = match.Groups["Amount"].Value.Replace(",", "");
                                        if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                                        {
                                            mxMappingMessage.InstructedAmount = amount;
                                        }
                                    }
                                }
                                // Match receiver's correspondent bank
                                
                                    match = receiverCorrespondentBankRegex.Match(swiftMessage.Request!);
                                    if (match.Success)
                                    {
                                        mxMappingMessage.ReceiverCorrespondentBank = match.Groups["ReceiverCorrespondentBank"].Value.Trim();
                                    }
                                    else
                                    {
                                        //errorMessages.Add(new ErrorMessage() { Code = "1010", Description = "ReceiverCorrespondentBank is empty" });
                                    }
                                

                                // Match sender's account number

                                //Regex senderAccountRegex = new Regex(@"^(?<SenderAccount>[\w/]+)$");
                                mxMappingMessage.SenderAccountNumber = match.Groups["SenderAccount"].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(swiftMessage.Request))
                                {
                                    match = senderAccountRegex.Match(swiftMessage.Request);
                                    if (match.Success)
                                    {
                                        string senderAccount = match.Groups["SenderAccount"].Value.Trim();
                                        string cleanedSenderAccount = senderAccount.Replace("/", "");
                                        mxMappingMessage.SenderAccountNumber = cleanedSenderAccount;


                                        if (!_sqlData.CheckValidIban(cleanedSenderAccount))
                                        {
                                            errorMessages.Add(new ErrorMessage() { Code = "DbtAcc", Description = "Invalid DebtorAcc Number" });

                                        }
                                        else
                                        {
                                            // IBAN is valid, no error message needs to be added
                                        }

                                    }
                                    else
                                    {

                                        errorMessages.Add(new ErrorMessage() { Code = "DbtAcc", Description = "Invalid DebtorAcc Number" });
                                    }
                                }
                                else
                                {

                                    errorMessages.Add(new ErrorMessage() { Code = "DbtAcc", Description = "SenderAccount is empty" });
                                }


                                // Match sender's name and address
                                match = senderNameAddressRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.SenderNameAndAddress = match.Groups["SenderNameAddress"].Value.Trim();
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "1007", Description = "SenderNameAddress is empty" });
                                }
                                // Match DebtorInstitutioncode
                                
                                match = DebtorInstitutionRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.DebtorInstitution = match.Groups["DebtorInstitution"].Value.Trim();
                                }
                                else
                                {
                                    //errorMessages.Add(new ErrorMessage() { Code = "1008", Description = "DebtorInstitution is empty" });
                                }
                                

                                // Match sender's correspondent bank
                               
                                match = senderCorrespondentBankRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.SenderCorrespondentBank = match.Groups["SenderCorrespondentBank"].Value.Trim();
                                }
                                else
                                {
                                    //
                                }
                                

                                // Match intermediary bank information
                                //match = thirdReimbursementAgent.Match(swiftMessage.Request!);
                                //if (match.Success)
                                //{
                                //    mxMappingMessage.ThirdReimbursementAgent = match.Groups["ThrdRmbrsmntAgtAcct"].Value.Trim();
                                //}
                                //else
                                //{
                                //    errorMessages.Add(new ErrorMessage() { Code = "1011", Description = "ThrdRmbrsmntAgtAcct is empty" });
                                //}


                                // Match intermediary bank information
                                //match = intermediaryBankRegex.Match(swiftMessage.Request!);
                                //if (match.Success)
                                //{
                                //    mxMappingMessage.IntermediaryBankInformation = match.Groups["IntermediaryBankInformation"].Value.Trim();
                                //}
                                //else
                                //{
                                //    errorMessages.Add(new ErrorMessage() { Code = "1012", Description = "IntermediaryBankInformation is empty" });
                                //}

                                // Match  Creditor Institution code
                                match = CreditorInstitutionRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.CreditorInstitution = match.Groups["CreditorInstitution"].Value.Trim();
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "1013", Description = "CreditorInstitution is empty" });
                                }

                                // Match receiver's account number
                                if (!string.IsNullOrWhiteSpace(swiftMessage.Request))
                                {
                                    match = receiverAccountRegex.Match(swiftMessage.Request);
                                    if (match.Success)
                                    {
                                        string receiverAccount = match.Groups["ReceiverAccount"].Value.Trim();

                                        string cleanedReceiverAccount = receiverAccount.Replace("/", "");

                                        mxMappingMessage.ReceiverAccountNumber = cleanedReceiverAccount;
                                        if (!_sqlData.CheckValidIban(cleanedReceiverAccount))
                                        {
                                            errorMessages.Add(new ErrorMessage() { Code = "CdrAcc", Description = "Invalid CreditorAcc Number" });

                                        }
                                        


                                        //    if (!cleanedReceiverAccount.StartsWith("AE"))
                                        //    {

                                        //        errorMessages.Add(new ErrorMessage() { Code = "CdrAcc", Description = "Invalid IBAN format for ReceiverAccount (must start with AE)" });
                                        //    }
                                        //}
                                        //else
                                        //{

                                        //    errorMessages.Add(new ErrorMessage() { Code = "CdrAcc", Description = "Invalid CreditorAcc Number" });
                                        //}
                                    }

                                    else
                                    {
                                        errorMessages.Add(new ErrorMessage() { Code = "CdrAcc", Description = "Receiver AccountNumber is empty" });
                                    }
                                }
                                else
                                {

                                    errorMessages.Add(new ErrorMessage() { Code = "CdrAcc", Description = "Receiver AccountNumber is empty" });
                                }


                                // Match receiver's name and address
                                if (receiverNameAddressRegex != null)
                                { 
                                    match = receiverNameAddressRegex.Match(swiftMessage.Request!);
                                    if (match.Success)
                                    {
                                        mxMappingMessage.ReceiverNameAndAddress = match.Groups["ReceiverNameAddress"].Value.Trim();
                                    }
                                    else
                                    {
                                        errorMessages.Add(new ErrorMessage() { Code = "1015", Description = "ReceiverNameAddress is empty" });
                                    }
                                }
                                // Match Remittance Information
                                match = RemittanceInformationRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.RemittanceInformation = match.Groups["RltdRmtInf"].Value.Trim();
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "1016", Description = "RltdRmtInf is empty" });
                                }
                                // Match details of charges
                                match = detailsOfChargesRegex.Match(swiftMessage.Request!);
                                if (match.Success)
                                {
                                    mxMappingMessage.DetailsOfCharges = match.Groups["DetailsOfCharges"].Value.Trim();
                                }
                                else
                                {
                                    errorMessages.Add(new ErrorMessage() { Code = "1017", Description = "DetailsOfCharges is empty" });
                                }
                                match = senderToReceiverInfoRegex.Match(swiftMessage.Request!);

                                if (match.Success)
                                {
                                    string senderToReceiverInfo = match.Groups["SenderToReceiverInfo"].Value.Trim();

                                    if (!string.IsNullOrWhiteSpace(senderToReceiverInfo))
                                    {
                                        mxMappingMessage.SenderToReceiverInformation = senderToReceiverInfo;
                                    }
                                    else
                                    {
                                        errorMessages.Add(new ErrorMessage() { Code = "1018", Description = "SenderToReceiverInfo is empty" });
                                    }
                                }

                                // Match regulatory reporting
                                //match = regulatoryReportingRegex.Match(swiftMessage.Request!);
                                //if (match.Success)
                                //{
                                //    mxMappingMessage.RegulatoryReporting = match.Groups["RegulatoryReporting"].Value.Trim();
                                //}
                                //else
                                //{
                                //    errorMessages.Add(new ErrorMessage() { Code = "1019", Description = "RegulatoryReporting is empty" });
                                //}


                                if (errorMessages.Any())
                                {
                                    mxMappingMessage.Status = "ERROR";
                                    mxMappingMessage.errorMessages = errorMessages;
                                }
                                else
                                {
                                    mxMappingMessage.Status = "SUCCESS";
                                }
                                pacsMessages.Add(mxMappingMessage);

                            }
                            catch (Exception ex)
                            {
                                _logger.Error("Conversion", "foreachloop-TransformMTToMXAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Conversion", "TransformMTToMXAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                }
            });
            _logger.Info("Conversion", "TransformMTToMXAsync", "Completed.");
            return pacsMessages;
        }
        //public  async void TransformMXtoMXAsync(List<SwiftMessage> PacsMessageList)
        //{
        //    try
        //    {
        //        _logger.Info("MXtoMTConversionWorker", "TransformMXtoMXAsync", $"TransformMXtoMXAsync is Started.");
        //        if (PacsMessageList?.Any() == true)
        //        {
        //            foreach (var swiftMessage in PacsMessageList)
        //            {
        //               if (string.IsNullOrWhiteSpace(swiftMessage.Request))
        //                continue;
        //                var bodyMatches = Regex.Matches(swiftMessage.Request, @"<Body[\s\S]*?</Body>", RegexOptions.IgnoreCase);

        //                if (bodyMatches.Count == 0)
        //                {
        //                    _logger.Warn("Conversion", "TransformMXtoMXAsync", "No <Body> tag found in request.");
        //                    continue;
        //                }
        //                var allEndToEndIdsList = new List<string>();
        //                decimal totalAmount = 0;
        //                foreach (Match match in bodyMatches)
        //               {
        //                    try
        //                    {
        //                        var singleBodyXml = match.Value;

        //                        _logger.Info("Conversion", "TransformMXtoMXAsync", $"Single PacsMsg{singleBodyXml}.");
        //                        var pacs008Request = await Deserialize<Body>(singleBodyXml);
        //                        if (pacs008Request == null)
        //                            continue;
        //                        var pacsMessage = MapPacs008ToPacsMessage(pacs008Request, swiftMessage.ID ?? 0);
        //                        try
        //                        {
        //                            await SavePacs008BatchxmlAsync(pacsMessage);
        //                            var endToEndIds = pacs008Request?.Document?.FIToFICstmrCdtTrf?.CdtTrfTxInf? .Where(txn => txn?.PmtId?.EndToEndId != null) .Select(txn => txn.PmtId!.EndToEndId).ToList();
        //                            if (endToEndIds != null)
        //                            {

        //                                allEndToEndIdsList.AddRange(endToEndIds!);
        //                                totalAmount += pacs008Request?.Document?.FIToFICstmrCdtTrf?.CdtTrfTxInf?.Sum(txn => txn?.IntrBkSttlmAmt?.Value ?? 0) ?? 0;
        //                                string allEndToEndIds = string.Join(",", allEndToEndIdsList);

        //                                _logger.Info("Conversion", "TransformMXtoMXAsync", $"EndToEndId{allEndToEndIds}.");

        //                                await _sqlData.UpdateAsync(pacsMessage.SwiftID, "MP", allEndToEndIds, totalAmount, "IPP", string.Empty);
        //                            }
        //                            else
        //                            {
        //                                _logger.Error("Conversion", "TransformMXtoMXAsync", "endToEndIds is null");
        //                            }
        //                        }
        //                        catch (Exception innerEx)
        //                        {

        //                            _logger.Error("MTtoMXConversionWorker", "Processing Message", $"Inner Exception: {innerEx.InnerException}");
        //                        }
        //                    }
        //                    catch (Exception bodyEx)
        //                    {
        //                        _logger.Error("Conversion", "TransformMXtoMXAsync", $"Error deserializing Body: {bodyEx.Message}");
        //                    }
        //                }
        //            }
        //        }
        //        _logger.Info("MXtoMTConversionWorker", "TransformMXtoMXAsync", $"TransformMXtoMXAsync is done.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Conversion", "TransformMXtoMXAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
        //    }

        //}
        public async Task TransformMXtoMXAsync(List<SwiftMessage> PacsMessageList)
        {
            try
            {
                _logger.Info("MXtoMTConversionWorker", "TransformMXtoMXAsync", "TransformMXtoMXAsync is Started.");

                if (PacsMessageList?.Any() == true)
                {
                    foreach (var swiftMessage in PacsMessageList)
                    {
                        if (string.IsNullOrWhiteSpace(swiftMessage.Request))
                            continue;

                        var bodyMatches = Regex.Matches(swiftMessage.Request, @"<Body[\s\S]*?</Body>", RegexOptions.IgnoreCase);
                        
                        if (bodyMatches.Count == 0)
                        {
                            _logger.Warn("Conversion", "TransformMXtoMXAsync", "No <Body> tag found in request.");
                            continue;
                        }
                        int successCount = 0;
                        int failedCount = 0;
                        int Totalcount = bodyMatches.Count;
                        var allEndToEndIdsList = new List<string>();
                        decimal totalAmount = 0;

                        foreach (Match match in bodyMatches)
                        {
                            try
                            {
                                var singleBodyXml = match.Value;
                                _logger.Info("Conversion", "TransformMXtoMXAsync", $"Single PacsMsg: {singleBodyXml}.");

                                var pacs008Request = await Deserialize<Body>(singleBodyXml);
                                if (pacs008Request == null)
                                    continue;

                              
                                var pacsMessage = MapPacs008ToPacsMessage(pacs008Request, swiftMessage.ID ?? 0);

                               
                                var validationErrors = ValidatePacs008(pacs008Request);
                                if (validationErrors.Any())
                                {
                                  
                                    pacsMessage.Status = "ERROR";
                                    pacsMessage.errorMessages = validationErrors;
                                    failedCount++;
                                    await _sqlData.SaveRepairQueue(pacsMessage);

                                    await _sqlData.UpdateAsync(pacsMessage.SwiftID, "ME", pacsMessage.SenderReference ?? string.Empty,  pacsMessage.InterbankSettlementAmount,"REPAIRQUEUE",
                                       string.Join(" | ", validationErrors.Select(e => $"{e.Code}:{e.Description}")), failedCount, successCount, Totalcount
                                   );
                                   
                                    _logger.Warn("Conversion", "TransformMXtoMXAsync",
                                        $"Message {pacsMessage.SwiftID} sent to Repair Queue. Reason: {string.Join(" | ", validationErrors.Select(e => e.Description))}");
                                    continue;
                                    //PacsMessageList.Remove(pacsMessage);
                                }

                               
                                var txnThresholdAmount = _masterTableList.thresholdAmount!.IPP_Max_Credit_Amount;
                                if (txnThresholdAmount > 0 && pacsMessage.InterbankSettlementAmount > txnThresholdAmount)
                                {
                                    EmailParams emailParams = new()
                                    {
                                        RefNbr = Convert.ToString(pacsMessage.SwiftID),
                                        Module = "OUTWARD",
                                        Type = "AUTO",
                                        Description = _serviceParams.Value.ThresholdEmailDescription
                                    };

                                    await _sqlData.SaveEmailAsync(emailParams, pacsMessage.SenderReference ?? string.Empty);
                                    await _sqlData.UpdateAsync(pacsMessage.SwiftID, "ME", pacsMessage.SenderReference ?? string.Empty, pacsMessage.InterbankSettlementAmount, "EMAIL",
                                        string.Empty, failedCount, successCount, Totalcount
                                    );

                                    _logger.Info("Conversion", "TransformMXtoMXAsync",
                                        $"Message {pacsMessage.SwiftID} sent to Email Queue (Above threshold: {pacsMessage.InterbankSettlementAmount}).");

                                    continue; 
                                }

                               
                                pacsMessage.Status = "SUCCESS";
                                await SavePacs008BatchxmlAsync(pacsMessage);
                                successCount++;
                                var endToEndIds = pacs008Request.Document?.FIToFICstmrCdtTrf?.CdtTrfTxInf?
                                    .Where(txn => txn?.PmtId?.EndToEndId != null)
                                    .Select(txn => txn.PmtId!.EndToEndId)
                                    .ToList();

                                if (endToEndIds != null && endToEndIds.Any())
                                {
                                    allEndToEndIdsList.AddRange(endToEndIds!);
                                    totalAmount += pacs008Request!.Document!.FIToFICstmrCdtTrf!.CdtTrfTxInf!
                                        .Sum(txn => txn?.IntrBkSttlmAmt?.Value ?? 0);

                                    string allEndToEndIds = string.Join(",", allEndToEndIdsList);

                                    _logger.Info("Conversion", "TransformMXtoMXAsync", $"EndToEndId: {allEndToEndIds}.");

                                    await _sqlData.UpdateAsync(pacsMessage.SwiftID, "MP", allEndToEndIds,  totalAmount, "IPP",
                                        string.Empty, failedCount, successCount, Totalcount
                                    );
                                }
                                else
                                {
                                    _logger.Error("Conversion", "TransformMXtoMXAsync", "EndToEndIds is null or empty");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error("Conversion", "TransformMXtoMXAsync",
                                $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                            }
                        }   
                    }
                }

                _logger.Info("MXtoMTConversionWorker", "TransformMXtoMXAsync", "TransformMXtoMXAsync is done.");
            }
            catch (Exception ex)
            {
                _logger.Error("Conversion", "TransformMXtoMXAsync",
                    $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
        }


        private PacsMessage MapPacs008ToPacsMessage(Body body,int id)
        {
            try
            {
                _logger.Info("Conversion", "MapPacs008ToPacsMessage", $"Started.");
                if (body == null)
                {
                    _logger.Error("Conversion", "MapPacs008ToPacsMessage", "Body is null");
                    return null!;
                }
                var Traninfo = body.Document?.FIToFICstmrCdtTrf!.CdtTrfTxInf!.FirstOrDefault()!;
                var pacsMessage = new PacsMessage
                {
                    MessageId = body.Document!.FIToFICstmrCdtTrf!.GrpHdr!.MsgId,
                    CreationDatetime = body.Document.FIToFICstmrCdtTrf.GrpHdr.CreDtTm.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    NumberOfTransactions = body.Document.FIToFICstmrCdtTrf.GrpHdr.NbOfTxs,
                    SttlmMtd = body.Document.FIToFICstmrCdtTrf.GrpHdr.SttlmInf!.SttlmMtd,
                    InstructingAgent = body.Document.FIToFICstmrCdtTrf.CdtTrfTxInf!.FirstOrDefault()!.InstgAgt!.FinInstnId!.BICFI,
                    InstructedAgent = body.Document.FIToFICstmrCdtTrf.CdtTrfTxInf!.FirstOrDefault()!.InstdAgt!.FinInstnId!.BICFI,
                    SenderReference = body.Document!.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.PmtId!.EndToEndId,
                    Currency = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.IntrBkSttlmAmt!.Ccy,
                    InterbankSettlementAmount = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf?.FirstOrDefault()!.IntrBkSttlmAmt!.Value,
                    UETR = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.PmtId!.UETR,
                    ValueDate = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.IntrBkSttlmDt.ToString("yyMMdd"),
                    TranType = Traninfo?.PmtTpInf?.CtgyPurp?.Cd ?? Traninfo?.Purp?.Prtry,
                    DebtorInstitution = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.DbtrAgt!.FinInstnId!.BICFI,
                    SenderName = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.Dbtr!.Nm,
                    SenderAccountNumber = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.DbtrAcct!.Id!.IBAN,
                    CreditorInstitution = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.CdtrAgt!.FinInstnId!.BICFI,
                    ReceiverNameAndAddress = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.Cdtr!.Nm,
                    ReceiverAccountNumber = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.CdtrAcct!.Id!.IBAN,
                    SwiftID = id,
                    RemittanceInformation = body.Document.FIToFICstmrCdtTrf?.CdtTrfTxInf!.FirstOrDefault()!.RmtInf!.Ustrd

                };
                return pacsMessage;
            }
           
            catch (Exception ex)
            {
                _logger.Error("Conversion", "MapPacs008ToPacsMessage", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                
                throw;
            }
           
        }
        private List<ErrorMessage> ValidatePacs008(Body body)
        {
            var errorMessages = new List<ErrorMessage>();

            if (body?.Document?.FIToFICstmrCdtTrf?.CdtTrfTxInf == null)
                return errorMessages;

            foreach (var tx in body.Document.FIToFICstmrCdtTrf.CdtTrfTxInf)
            {
                // Rule 1: Value Date must be today's date
                if (tx.IntrBkSttlmDt.Date != DateTime.Today)
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "StlDt",
                        Description = "Settlement Date must be today's date"
                    });
                }

                // Rule 1: Value Date must be today's date
                if (tx.IntrBkSttlmAmt?.Value == null || tx.IntrBkSttlmAmt.Value <= 0)
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "1005",
                        Description = "IntrBkSttlmAmt is missing or invalid"
                    });
                }
                if (tx.IntrBkSttlmAmt?.Value == null || tx.IntrBkSttlmAmt.Value <= 0 || tx.IntrBkSttlmAmt.Value > 50000)
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "1005",
                        Description = "IntrBkSttlmAmt is missing, invalid, or exceeds the maximum limit (50,000)"
                    });
                }

                // Rule 2: Debtor Name mandatory
                if (string.IsNullOrWhiteSpace(tx.PmtId?.EndToEndId))

                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "EndtoEndId",
                        Description = "EndtoEndId is missing"
                    });
                }
                // Rule 3: Debtor Account mandatory and length must be 23
                if (string.IsNullOrWhiteSpace(tx.DbtrAcct?.Id?.IBAN))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "DbtAcc",
                        Description = "Debtor Account (IBAN) is missing"
                    });
                }
                else if (tx.DbtrAcct.Id.IBAN.Length !=23)
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "DbtAcc",
                        Description = "Debtor Account (IBAN) must be 23 characters long"
                    });
                }
                 else if (!string.IsNullOrWhiteSpace(tx.DbtrAcct.Id.IBAN))
                {
                    if (!IbanExistsInMasterAccounts( tx.DbtrAcct.Id.IBAN))
                    {
                        errorMessages.Add(new ErrorMessage
                        {
                            Code = "DbtAcc",
                            Description = "Debtor Account (IBAN) does not exist in Master Accounts"
                        });
                    }
                }

                // Rule 4: Debtor Name mandatory
                if (string.IsNullOrWhiteSpace(tx.Dbtr?.Nm))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "1007",
                        Description = "Debtor Name is missing"
                    });
                }

                // Rule 5: Creditor Account mandatory
                if (string.IsNullOrWhiteSpace(tx.CdtrAcct?.Id?.IBAN))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "CdrAcc",
                        Description = "Creditor Account (IBAN) is missing"
                    });
                }
                else if (tx.CdtrAcct?.Id?.IBAN.Length != 23)
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "CdrAcc",
                        Description = "Creditor Account (IBAN) must be 23 characters long"
                    });
                }


                // Rule 6: Creditor Name mandatory
                if (string.IsNullOrWhiteSpace(tx.Cdtr?.Nm))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "1015",
                        Description = "Creditor Name is missing"
                    });
                }

                // Rule 7: Debtor Agent (DebtorInstitution) mandatory
                if (string.IsNullOrWhiteSpace(tx.DbtrAgt?.FinInstnId?.BICFI))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "1008",
                        Description = "Debtor Institution (BIC) is missing"
                    });
                }

                // Rule 8: Creditor Agent (CreditorInstitution) mandatory
                if (string.IsNullOrWhiteSpace(tx.CdtrAgt?.FinInstnId?.BICFI))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "1013",
                        Description = "Creditor Institution (BIC) is missing"
                    });
                }
                // Rule 8: Category Purpose Code validation
                if (string.IsNullOrWhiteSpace(tx.Purp?.Prtry))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "CatPrCd",
                        Description = "Category Purpose Code is missing"
                    });
                }
                else if (!_masterTableList.categoryPurposeCode!
                    .Exists(r => r.Code_Value == tx.Purp?.Prtry && r.IsApplicable == "Y"))
                {
                    errorMessages.Add(new ErrorMessage
                    {
                        Code = "CatPrCd",
                        Description = "Category Purpose Code is invalid or not applicable"
                    });
                }
            }

            return errorMessages;
        }


        private async Task SavePacs008BatchxmlAsync(PacsMessage pacsMessage)
        {
            var response = new IBANEnquiryResponse();
            _logger.Info("Conversion", "SavePacs008BatchxmlAsync", "$Started.");
            try
            {
                var IppBatchdetails = new IppCreditTransferpaymentdetails();
                response = IbanEnquiry(pacsMessage.SenderAccountNumber!, pacsMessage);
                IppBatchdetails.Core_Message_Identification = pacsMessage.MessageId;
                IppBatchdetails.Core_Creation_DateTime = pacsMessage.CreationDatetime;
                IppBatchdetails.Core_Number_Of_Transactions = pacsMessage.NumberOfTransactions;
                IppBatchdetails.Core_Settlement_Method = pacsMessage.SttlmMtd;
                IppBatchdetails.Core_Instructing_Agent_FI_ID = pacsMessage.InstructingAgent;
                IppBatchdetails.Core_Instructed_Agent_FI_ID = pacsMessage.InstructedAgent;
                IppBatchdetails.Instruction_Identification = pacsMessage.SenderReference;
                IppBatchdetails.EndToEnd_Identification = pacsMessage.SenderReference;
                IppBatchdetails.Transaction_Identification = pacsMessage.SenderReference;
                IppBatchdetails.UETR = pacsMessage.UETR;
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
                _logger.Info("Conversion", "SavePacs008BatchxmlAsync", "Inserted Successfully Done");
            }

            catch (Exception ex)
            {
                _logger.Error("Conversion", "SavePacs008BatchxmlAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");


            }
        }

        public IBANEnquiryResponse IbanEnquiry(string iban, PacsMessage pacsMessage)
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
                _sqlData.UpdateAsync(pacsMessage.SwiftID, "ENQ_FAIL", pacsMessage.SenderReference!, pacsMessage.InterbankSettlementAmount, "IBAN_ENQUIRY_FAILED", string.Empty,0,0,0);
                _logger.Error("Conversion", "IbanEnquiry", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
            return enquiryresponse;
        }
        //public async Task<List<DataTable>> TransformMXtoMXAsync(List<SwiftMessage> PacsMessageList)
        //{
        //    var dataTables = new List<DataTable>();

        //    try
        //    {
        //        if (PacsMessageList.Any())
        //        {
        //            foreach (var swiftMessage in PacsMessageList)
        //            {
        //                if (!string.IsNullOrEmpty(swiftMessage.Request))
        //                {
        //                    var pacs008Request = await Deserialize<Body>(swiftMessage.Request);
        //                    if (pacs008Request != null)
        //                    {
        //                        var debtorIban = pacs008Request.Document.FIToFICstmrCdtTrf.CdtTrfTxInf.FirstOrDefault()?.DbtrAcct?.Id?.IBAN ?? string.Empty;
        //                        var creditorIban = pacs008Request.Document.FIToFICstmrCdtTrf.CdtTrfTxInf.FirstOrDefault()?.CdtrAcct?.Id?.IBAN ?? string.Empty;

        //                        var ibanResponse = IbanEnquiry(debtorIban, new PacsMessage
        //                        {
        //                            SwiftID = swiftMessage.ID ?? 0,
        //                            SenderReference = pacs008Request.Document.FIToFICstmrCdtTrf.GrpHdr.MsgId,
        //                            InterbankSettlementAmount = pacs008Request.Document.FIToFICstmrCdtTrf.CdtTrfTxInf.FirstOrDefault()!.IntrBkSttlmAmt?.Value ?? 0m
        //                        });

        //                        if (ibanResponse != null)
        //                        {
        //                            _logger.Info("IBANEnquiry", "TransformMXtoMXAsync", $"Debtor IBAN: {debtorIban}, Creditor IBAN: {creditorIban}, CustomerName: {ibanResponse.CustomerName}");
        //                        }
        //                        //var pacsMessage = MapPacs008ToPacsMessage(pacs008Request,swiftMessage.ID ?? 0);
        //                        var dataTable = ConvertToDataTableAsync(pacs008Request, ibanResponse);
        //                        await _sqlData.SaveBatchPaymentAsync(dataTable);
        //                        dataTables.Add(dataTable);
        //                        //await SavePacs008BatchxmlAsync(pacsMessage);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Conversion", "TransformMXtoMXAsync", ex.Message);
        //    }

        //    return dataTables;
        //}
        public async Task<T> Deserialize<T>(string xmlContent) where T : class
        {
            T? deserializedObject = null;
            await Task.Run(() =>
            {
                try
                {
                    XmlSerializer serializer = new(typeof(T));
                    using StringReader stringReader = new(xmlContent);
                    deserializedObject = serializer.Deserialize(stringReader) as T;
                }
                catch (Exception ex)
                {
                    _logger.Error("Conversion", "Deserialize", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");

                  
                }
            });
            return deserializedObject!;
        }

        private bool IbanExistsInMasterAccounts(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
                return false;

            iban = iban.Trim().ToUpper();

            return _appDbContext.masterAccounts.Any(a =>
                a.CustomerAccountNumber!.Trim().ToUpper() == iban &&
                (a.IsDeleted == false || a.IsDeleted == null) &&   // handle nulls
                a.Status!.Trim() == "20" && a.AccountStatus =="A"
            );
        }
    }
}
