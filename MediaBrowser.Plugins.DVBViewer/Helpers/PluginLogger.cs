using System;
using System.Text;

using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.DVBViewer.Interfaces;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    /// <summary>
    /// Wrapper class for the Emby logging manager
    /// </summary>
    public class PluginLogger : IPluginLogger
    {
        readonly ILogger _logger;

        public PluginLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(string message, params object[] paramList)
        {
            _logger.Debug("{0}", String.Format(message, paramList));
        }

        public void Error(string message, params object[] paramList)
        {
            _logger.Error("{0}", String.Format(message, paramList));
        }

        public void ErrorException(string message, Exception exception, params object[] paramList)
        {
            _logger.FatalException("{0}", exception, String.Format(message, paramList));
        }

        public void Error(Exception exception, string message, params object[] paramList)
        {
            _logger.FatalException("{0}", exception, String.Format(message, paramList));
        }

        public void Fatal(string message, params object[] paramList)
        {
            _logger.Fatal("{0}", String.Format(message, paramList));
        }

        public void FatalException(string message, Exception exception, params object[] paramList)
        {
            _logger.FatalException("{0}", exception, String.Format(message, paramList));
        }

        public void Fatal(Exception exception, string message, params object[] paramList)
        {
            _logger.FatalException("{0}", exception, String.Format(message, paramList));
        }

        public void Info(string message, params object[] paramList)
        {
            if (Plugin.Instance.Configuration.EnableLogging)
            {
                _logger.Info("{0}", String.Format(message, paramList));
            }
            else
            {
                _logger.Debug("{0}", String.Format(message, paramList));
            }  
        }

        public void Log(LogSeverity severity, string message, params object[] paramList)
        {
            _logger.Log(severity, "{0}", String.Format(message, paramList));
        }

        public void LogMultiline(string message, LogSeverity severity, StringBuilder additionalContent)
        {
            _logger.LogMultiline(String.Format("{0}", message), severity, additionalContent);
        }

        public void Warn(string message, params object[] paramList)
        {
            _logger.Warn("{0}", String.Format(message, paramList));
        }

        public void Log(LogSeverity severity, ReadOnlyMemory<char> message)
        {
            throw new NotImplementedException();
        }

        public void Error(ReadOnlyMemory<char> message)
        {
            throw new NotImplementedException();
        }

        public void Warn(ReadOnlyMemory<char> message)
        {
            throw new NotImplementedException();
        }

        public void Info(ReadOnlyMemory<char> message)
        {
            throw new NotImplementedException();
        }

        public void Debug(ReadOnlyMemory<char> message)
        {
            throw new NotImplementedException();
        }
    }
}
