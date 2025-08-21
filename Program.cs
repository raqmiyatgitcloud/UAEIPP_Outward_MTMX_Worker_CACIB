using Dapper;
using Microsoft.EntityFrameworkCore;
using NLog;
using Raqmiyat.Framework.Custom;
using Raqmiyat.Framework.Domain;
using Raqmiyat.Framework.Model;
using Raqmiyat.Framework.NLogService;
using System.Data;
using System.Data.SqlClient;
using UAEIPP_Outward_MTMX_Worker.Worker;

namespace UAEIPP_Outward_MTMX_Worker
{
    public static class Program
    {
        private static readonly Logger _logger = LogManager.GetLogger("ServiceLog");
        public static async Task Main(string[] args)
        {

            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();

        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MTtoMXConversionWorkerLog>();
                    services.AddSingleton<MXtoMXWorkerLog>();
                    services.AddSingleton<RepairQueueWorkerLog>();
                    services.AddSingleton<ConversionWorkerLog>();
                    services.AddSingleton<Conversion>();
                    services.AddSingleton<ServiceLog>();
                    services.AddSingleton<ConnectCustom>();
                    services.AddSingleton<SqlData>();

                    services = GetConfigurationSection(hostContext, services);
                    services = GetSingletonIDbConnection(hostContext, services);
                    services = GetSingletonMasterTableList(hostContext, services);

                    var _dataBaseConnectionParams = hostContext.Configuration.GetSection(nameof(DataBaseConnectionParams)).Get<DataBaseConnectionParams>();
                    services.AddDbContext<AppDbContext>(option => option.UseSqlServer(SqlConManager.GetEntityConnectionString(_dataBaseConnectionParams!.DBConnection!, _dataBaseConnectionParams.IsEncrypted)), ServiceLifetime.Singleton);

                    services.AddHostedService<MTtoMXConversionWorker>();
                    services.AddHostedService<MXtoMXWorker>();
                    services.AddHostedService<RepairQueueWorker>();
                    services.AddTransient<HttpClientHandler>();
                    services.AddHttpClient("HttpClient").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler() { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; } });
                    //builder.Services.AddHttpClient("HttpClient").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
                });
        public static IServiceCollection GetConfigurationSection(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.Configure<ServiceParams>(hostContext.Configuration.GetSection("ServiceParams"));
            services.Configure<DataBaseConnectionParams>(hostContext.Configuration.GetSection("DataBaseConnectionParams"));
            services.Configure<StoredProcedureParams>(hostContext.Configuration.GetSection("StoredProcedureParams"));
            return services;
        }
        public static IServiceCollection GetSingletonIDbConnection(HostBuilderContext hostContext, IServiceCollection services)
        {

            services.AddSingleton<IDbConnection>(provider =>
            {
                var connectionStrings = hostContext.Configuration.GetSection(nameof(DataBaseConnectionParams)).Get<DataBaseConnectionParams>();
                IDbConnection dbConnection = new SqlConnection(SqlConManager.GetConnectionString(connectionStrings!.DBConnection!, connectionStrings.IsEncrypted));
                if (dbConnection.State != ConnectionState.Closed)
                {
                    _logger.Info(ForStructuredLog("Program", "GetSingletonIDbConnection", "DBConnection is already opened."));
                    return dbConnection;
                }
                dbConnection.Open();
                _logger.Info(ForStructuredLog("Program", "GetSingletonIDbConnection", "DBConnection opened"));
                return dbConnection;
            });
            return services;
        }
        public static IServiceCollection GetSingletonMasterTableList(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddSingleton<MasterTableList>(provider =>
            {
                var _storedProcedureParams = hostContext.Configuration.GetSection(nameof(StoredProcedureParams)).Get<StoredProcedureParams>();
                var connectionStrings = hostContext.Configuration.GetSection(nameof(DataBaseConnectionParams)).Get<DataBaseConnectionParams>();
                var masterTableList = new MasterTableList();
                try
                {
                    using (IDbConnection dbConnection = new SqlConnection(SqlConManager.GetConnectionString(connectionStrings!.DBConnection!, connectionStrings.IsEncrypted)))
                    {
                        if (dbConnection.State == System.Data.ConnectionState.Closed)
                        {
                            dbConnection.Open();
                            _logger.Info(ForStructuredLog("Program", "GetSingletonMasterTableList", "DBConnection opened"));
                        }
                        var parameters = new DynamicParameters();
                        var reader = dbConnection.QueryMultiple(_storedProcedureParams!.GetMasterTableAsync!, parameters, commandType: CommandType.StoredProcedure, transaction: null);
                        try
                        {
                            masterTableList.bankOnBoardList = reader.Read<BankOnBoard>().ToList();
                            masterTableList.thresholdAmount = reader.Read<ThresholdAmount>().SingleOrDefault();
                            masterTableList.categoryPurposeCode = reader.Read<CategoryPurposeCode>().ToList();
                            reader.Dispose();
                        }
                        catch { }
                    }

                    _logger.Info(ForStructuredLog("Program", "GetSingletonMasterTableList", "ExecuteSQL is completed"));
                }
                catch (Exception ex2)
                {
                    _logger.Error(ForStructuredLog("Program", "GetSingletonMasterTableList", ex2.Message));
                }
                return masterTableList;
            });
            return services;
        }
        private static string ForStructuredLog(string ControllerName, string MethodName, string Message)
        {
            return $"-----------------------------------------------\r\n class Name :{ControllerName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
        }
    }
}
