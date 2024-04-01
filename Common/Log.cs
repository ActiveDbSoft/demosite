using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoSite.Common
{
    public class Log : ActiveQueryBuilder.Core.ILog
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void Trace(string message)
        {
            Logger.Debug(message);
        }

        public void Warning(string message)
        {
            Logger.Warn(message);
        }

        public void Error(string message)
        {
            Logger.Error(message);
        }

        public void Error(string message, Exception ex)
        {
            Logger.Error(ex, message);
        }
    }
}
