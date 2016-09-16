using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PathIntegrationTaskClient
{
    public enum LoggerMessageType { Info, Warning, Error };

    struct MessageToLog { public string message; public LoggerMessageType messageType; }

    public static class Logger
    {
        private static Text _screenLogger = null;
        private static string _externalPath = Path.Combine(Application.persistentDataPath, "Logs");
        private static bool _logToExternalFile;
        private static DateTime _lastLogTime;
        private static readonly IList<MessageToLog> LogBufferList = new List<MessageToLog>();
        private static readonly string _logFormat = "[{0}]: {1}";

        private static Color GetLoggerMessageColor(LoggerMessageType messageType)
        {
            Color col = Color.black;
            switch (messageType)
            {
                case LoggerMessageType.Info:
                    col = Color.blue;
                    break;
                case LoggerMessageType.Warning:
                    col = Color.black;
                    break;
                case LoggerMessageType.Error:
                    col = Color.red;
                    break;
                default:
                    break;
            }
            return col;
        }

        public static void SetLoggerLogToExternal(bool logToExternal)
        {
            _logToExternalFile = logToExternal;

            //Setting external logger
            if (_logToExternalFile)
            {
                if (!Directory.Exists(_externalPath))
                {
                    Directory.CreateDirectory(_externalPath);
                    Log("Created directory for external logger", LoggerMessageType.Info);
                }
            }
        }

        public static void SetLoggerUIFrame(Text text)
        {
            _screenLogger = text;
        }

        public static void Log(string mes, LoggerMessageType mesType)
        {
            MessageToLog mtl = new MessageToLog() { message = mes, messageType = mesType };

            try
            {
                WriteConsole(mtl);
                if (!_logToExternalFile)
                    return;

                lock (LogBufferList)
                {
                    LogBufferList.Add(mtl);
                    if (_lastLogTime.AddSeconds(60).Ticks > DateTime.UtcNow.Ticks)
                        return;

                    string interpolated = String.Concat("PTI-", DateTime.Today.ToString("yyyy-MM-dd"), "-", DateTime.Now.ToString("HH"), ".txt");
                    using
                        (
                            var log = File.AppendText(Path.Combine(_externalPath, interpolated))
                        )
                    {
                        foreach (var line in LogBufferList)
                        {
                            log.WriteLine(GetLoggerLine(line));
                        }

                        _lastLogTime = DateTime.Now;
                        log.Flush();
                        LogBufferList.Clear();

                    }

                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }


        }

        private static void WriteConsole(MessageToLog mtl)
        {
            if (_screenLogger != null)
            {
                _screenLogger.text = String.Format(_logFormat, mtl.messageType.ToString(), mtl.message);
                _screenLogger.color = GetLoggerMessageColor(mtl.messageType);
            }

            if (Application.isEditor)
                Debug.Log(mtl.message);

        }

        private static string GetLoggerLine(MessageToLog mtl)
        {
            return string.Concat("[", DateTime.Now.ToString("HH::mm:ss"), "] : ", String.Format(_logFormat, mtl.messageType.ToString(), mtl.message));
        }

    }
}
