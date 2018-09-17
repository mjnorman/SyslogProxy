#pragma warning disable Serilog004 // Constant MessageTemplate verifier
namespace SyslogProxy
{
    using System;
    using System.Net.Http;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Compact;
    using SyslogProxy.Messages;

    public class SeqWriter
    {
        private int retryCount;
        
        public SeqWriter()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Seq(
                   serverUrl: Configuration.SeqServer.ToString(),
                   apiKey: Configuration.APIKey.ToString())
                .CreateLogger();
            Log.Information("SyslogProxy connected");
        }

        public async Task WriteToSeq(JsonSyslogMessage message, int delay = 0)
        {
            //LogEvent logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("{Hostname}:{ApplicationName} {Message}", message.Hostname, message.ApplicationName, message.Message), message.Properties);
            Log.Information(Configuration.MessageTemplate, message.GetSEQProperties());
        }

        //public async Task WriteToSeq(JsonSyslogMessage message, int delay = 0)
        //{
        //    if (message.Invalid)
        //    {
        //        Logger.Warning("Skipping incomplete/invalid message. [{0}]", message.RawMessage);
        //        return;
        //    }

        //    await Task.Delay(Math.Min(delay, 60000));

        //    ExceptionDispatchInfo capturedException = null;
        //    try
        //    {
        //        await this.WriteMessage(message).ConfigureAwait(false);
        //    }
        //    catch (Exception ex)
        //    {
        //        capturedException = ExceptionDispatchInfo.Capture(ex);
        //    }

        //    if (capturedException != null)
        //    {
        //        this.retryCount++;
        //        Logger.Warning("Couldn't write to SEQ. Retry Count:[{0}] Exception: [{1}]", this.retryCount, capturedException.SourceException.Message);
        //        await this.WriteToSeq(message, (int)Math.Pow(100, this.retryCount));
        //    }
        //}

        private async Task WriteMessage(JsonSyslogMessage message)
        {

            
            using (var http = new HttpClient())
            {
                using (var content = new StringContent("{\"events\":[" + message.ToString() + "]}", Encoding.UTF8, "application/json"))
                {
                    var response = await http.PostAsync(new Uri(Configuration.SeqServer, "api/events/raw"), content).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}