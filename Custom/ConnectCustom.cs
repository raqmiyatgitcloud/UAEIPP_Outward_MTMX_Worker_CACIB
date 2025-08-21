using Microsoft.Extensions.Options;
using NLog;
using Raqmiyat.Framework.Model;
using System.Data;
using System.Data.SqlClient;

namespace Raqmiyat.Framework.Custom
{
    public class ConnectCustom
    {
        private readonly IOptions<DataBaseConnectionParams> _dataBaseConnectionParams;

        public ConnectCustom(IOptions<DataBaseConnectionParams> dataBaseConnectionParams)
        {
            _dataBaseConnectionParams = dataBaseConnectionParams;
        }
        public IDbConnection GetSingletonIDbConnection(Logger _logger)
        {
            IDbConnection dbConnection = new SqlConnection(SqlConManager.GetConnectionString(_dataBaseConnectionParams.Value!.DBConnection!, _dataBaseConnectionParams.Value.IsEncrypted));
            if (dbConnection.State != ConnectionState.Closed)
            {
                _logger.Info(ForStructuredLog("ConnectCustom", "GetSingletonIDbConnection", "DBConnection is already opened."));
                return dbConnection;
            }
            dbConnection.Open();
            return dbConnection;

        }
        private string ForStructuredLog(string ControllerName, string MethodName, string Message)
        {
            return $"-----------------------------------------------\r\n class Name :{ControllerName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
        }
    }

}
