using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using NLog;
using NLog.Targets;
using NLog.Internal;
using NLog.Config;


namespace ALPR_Core
{
    /// <summary>
    /// logger singleton
    /// </summary>
    public class CLogger
    {
        public bool createNewLogFile(string strFileName)
        {
            try
            {
                LoggingConfiguration config = LogManager.Configuration;

                var logFile = new FileTarget();

                logFile.Name = "sysFile";
                logFile.FileName = strFileName;//string.Format("C:\\Temp\\SPA\\LogFile-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now); 
                logFile.Layout = "${date} | ${message}";
                logFile.CreateDirs = true;

                try
                {
                    config.RemoveTarget(logFile.Name);
                }
                finally
                {

                }
                config.AddTarget(logFile.Name, logFile);

                LogManager.Configuration = config;
            }
            catch (Exception ex)
            {

            }

            return true;
        }

        private Logger logInstance = null;
        public Logger getLogger()
        {
            logInstance = LogManager.GetLogger("ALPR_Logger");
            
            return logInstance;
        }

        private static CLogger _instance;
        public static CLogger Instance()
        {
            if (null == _instance)
            {
                _instance = new CLogger();
            }

            return _instance;
        }

        public static CLogger Instance(string filename)
        {
            if (null == _instance)
            {
                _instance = new CLogger();
                _instance.createNewLogFile(filename);
            }

            return _instance;
        }

        private CLogger()
        {
            //Instantiate the individual 
        }
    }
}
