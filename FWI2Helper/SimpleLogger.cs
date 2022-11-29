using System;

namespace FWI2Helper
{
    public class SimpleFileLogger : LoggerBase
    {
        private string _fileName;
        private StreamWriter _logWriter;

        public SimpleFileLogger(string fileName)
        {
            _fileName = fileName;
        }

        public override void Log(string message)
        {
            if (_logWriter == null)
            {
                _logWriter = new StreamWriter(_fileName, true);
            }

            _logWriter.WriteLine(this.FormatMessage(message));
        }

        public override void Dispose()
        {
            if (_logWriter != null) { _logWriter.Dispose(); }
        }
    }

    public class SimpleConsoleLogger : LoggerBase
    {
        public override void Log(string message)
        {
            Console.WriteLine(this.FormatMessage(message));
        }
    }

    public class MultiLogger : LoggerBase
    {
        private LoggerBase[] _logger;

        public MultiLogger(params LoggerBase[] logger)
        {
            _logger = logger;
        }

        public override void Log(string message)
        {
            foreach (var curLogger in _logger)
            {
                curLogger.Log(message);
            }
        }

        public override void Dispose()
        {
            foreach (var curLogger in _logger)
            {
                if (curLogger is IDisposable disposableLogger)
                {
                    disposableLogger.Dispose();
                }
            }
        }
    }

    public abstract class LoggerBase : IDisposable
    {
        public virtual void Dispose()
        {
        }

        public abstract void Log(string message);

        protected string FormatMessage(string message)
        {
            return $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] " + message;
        }
    }
}