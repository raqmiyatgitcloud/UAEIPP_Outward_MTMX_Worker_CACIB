using Dapper;
using Microsoft.Extensions.Options;
using Raqmiyat.Framework.Custom;
using Raqmiyat.Framework.Domain;
using Raqmiyat.Framework.Model;
using Raqmiyat.Framework.NLogService;
using System.Data;


namespace UAEIPP_Outward_MTMX_Worker
{
    public class MXtoMXWorker : BackgroundService
    {
        private readonly MXtoMXWorkerLog _logger;
        private readonly IOptions<ServiceParams> _serviceParams;
        private IDbConnection _idbConnection;
        private readonly ConnectCustom _connectCustom;
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient _httpClient;
        private readonly SqlData _sqlData;
        private readonly Conversion _conversion;
        private readonly AppDbContext _appDbContext;
        private readonly MasterTableList _masterTableList;

        public MXtoMXWorker(MXtoMXWorkerLog logger, IOptions<ServiceParams> serviceParams, IDbConnection idbConnection, ConnectCustom connectCustom, SqlData sqlData, Conversion conversion, AppDbContext appDbContext, MasterTableList masterTableList,HttpClient httpClient,IHttpClientFactory httpClientFactory)
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
            _logger.Info("MXtoMXWorker", "ExecuteAsync", $"Service Started at {DateTimeOffset.Now.ToString("o")}");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await InitializeAsync();
                    try
                    {
                        var PacsMessages = await _sqlData.GetPacsMessageAsync();
                        if(PacsMessages.Any())
                        {
                           await _conversion.TransformMXtoMXAsync(PacsMessages);
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(_serviceParams.Value.WorkerIntervalInSeconds), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("MXtoMXWorker", "ExecuteAsync", $"MQ Receiver Error: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                        await SaveEmailAsync("MXtoMXWorker", "MTtoMXConversion", "MXtoMXWorker", "ExecuteAsync", "1000", ex.Message);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(_serviceParams.Value.WorkerIntervalInMilliSeconds), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("MXtoMXWorker", "ExecuteAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
        }
        private async Task InitializeAsync()
        {
            _logger.Info("MXtoMXWorker", "InitializeAsync", $"Started.");
            try
            {
                await Task.Run(() =>
                   {
                       if (_idbConnection == null)
                       {
                           _logger.Info("MXtoMXWorker", "InitializeAsync", "Re-Initialize the DbConnection Connection.");
                           _idbConnection = _connectCustom!.GetSingletonIDbConnection(_logger._log);
                       }
                   });
            }
            catch (Exception ex)
            {
                _logger.Error("MXtoMXWorker", "InitializeAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
                throw;
            }
            _logger.Info("MXtoMXWorker", "InitializeAsync", $"Completed.");
        }
     
        private async Task SaveEmailAsync(string refno, string module, string classname, string method, string code, string desc)
        {
            try
            {
                _logger.Info("MXtoMXWorker", "IbanEnquiry", $"Started.");
                var parameters = new DynamicParameters();
                parameters.Add("@Mail_RefNo", refno, DbType.String);
                parameters.Add("@Mail_Module", module, DbType.String);
                parameters.Add("@Mail_ServiceName", "UAEIPP_Outward_MTMX_Worker", DbType.String);

                parameters.Add("@Mail_ClassName", classname, DbType.String);
                parameters.Add("@Mail_MethodName", method, DbType.String);
                parameters.Add("@Mail_ResponseCode", code, DbType.String);
                parameters.Add("@Mail_Description", desc, DbType.String);
                await _idbConnection.QueryAsync<string>("IPP_Insert_sp_SendMail", parameters, commandTimeout: 1200, commandType: CommandType.StoredProcedure, transaction: null);
                _logger.Info("MXtoMXWorker", "SaveEmailAsync", $"SaveEmailAsync Data is Inserted.");

            }
            catch (Exception ex)
            {
                _logger.Info("MXtoMXWorker", "SaveEmailAsync", $"Exception: {ex.Message},StackTrace: {ex.StackTrace}, InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}");
            }
        }
   
    }
}