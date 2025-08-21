using NLog;

namespace Raqmiyat.Framework.NLogService
{
    public class MTtoMXConversionWorkerLog
    {
        public readonly NLog.Logger _log = LogManager.GetLogger("MTtoMXConversionWorkerLog");
        public void Debug(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Debug(loggingMessage);
        }
        public void Error(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Error(loggingMessage);
        }
        public void Info(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Info(loggingMessage);
        }
        public void Warn(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Warn(loggingMessage);
        }
    }
    public class MXtoMXWorkerLog
    {
        public readonly NLog.Logger _log = LogManager.GetLogger("MXtoMXWorkerLog");
        public void Debug(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Debug(loggingMessage);
        }
        public void Error(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Error(loggingMessage);
        }
        public void Info(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Info(loggingMessage);
        }
        public void Warn(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Warn(loggingMessage);
        }
    }
    public class RepairQueueWorkerLog
    {
        public readonly NLog.Logger _log = LogManager.GetLogger("RepairQueueWorkerLog");
        public void Debug(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Debug(loggingMessage);
        }
        public void Error(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Error(loggingMessage);
        }
        public void Info(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Info(loggingMessage);
        }
        public void Warn(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Warn(loggingMessage);
        }
    }
    public class ConversionWorkerLog
    {
        public readonly NLog.Logger _log = LogManager.GetLogger("ConversionWorkerLog");
        public void Debug(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Debug(loggingMessage);
        }
        public void Error(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Error(loggingMessage);
        }
        public void Info(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Info(loggingMessage);
        }
        public void Warn(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Warn(loggingMessage);
        }
    }
    public class ServiceLog
    {
        public readonly NLog.Logger _log = LogManager.GetLogger("ServiceLog");
        public void Debug(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Debug(loggingMessage);
        }
        public void Error(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Error(loggingMessage);
        }
        public void Info(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Info(loggingMessage);
        }
        public void Warn(string ClassName, string MethodName, string Message)
        {
            string loggingMessage = $"-----------------------------------------------\r\n Class Name :{ClassName}.\r\n Method Name : {MethodName} \r\n Message : {Message}. \r\n ----------------------------------------------------------------------------";
            _log.Warn(loggingMessage);
        }
    }
}
