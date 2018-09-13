namespace SyslogProxy.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    public class JsonSyslogMessage
    {
        private static readonly Regex _DateStampRegex = new Regex(@"\w{3} \w{3}.{4}\d{2}:\d{2}:\d{2}.\d{3}");
        
        // See https://tools.ietf.org/html/rfc5424#section-6 
        private static readonly string _rfc5424Regex = @"\<(?<PRI>\d+)\>(?<VERSION>\d{1,2}) (?<TIMESTAMP>(?:\b\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{6}[\+,\-]\d\d:\d\d\b|-)) (?<HOSTNAME>(?:[\x21-\x7E]{1,255}|-)){1,255} (?<APPNAME>(?:[\x21-\x7E]{1,48}|-)) (?<PROCID>[\x21-\x7E]{1,128})? (?<MSGID>(?:[\x21-\x7E]{1,32}|-)) (?<SD>(?:\[(?<SDID>[\x21\x23-\x3C\x3E-\x5A\x5C-\x7E]{1,32})(?: (?<SDPARAM>(?<PARAMNAME>[\x21\x23-\x3C\x3E-\x5A\x5C-\x7E]{1,32})=""(?<PARAMVALUE>[^""]*)""))*\]|-)) (?<MSG>.*)";
        private static readonly string _sdpairRegex = @"(?<PARAMNAME>[\x21\x23-\x3C\x3E-\x5A\x5C-\x7E]{1,32})=""(?<PARAMVALUE>[^ ""]*)""";

        public JsonSyslogMessage(string rawMessage)
        {
            //init private var
            StructuredDataElements = new Dictionary<string, string>();

            RawMessage = rawMessage;

            //if we can parse full rfc5424 return after
            if(ParseRFC5424())
                return;

            var splitLine = rawMessage.Split(' ');
            if (splitLine.Length < 4)
            {
                Invalid = true;
                return;
            }

            try
            {
                var priority = int.Parse(splitLine[0]);
                var facility = priority / 8;
                var severity = priority % 8;

                Facility = ((Facility)facility).ToString();
                Level = ((Severity)severity).ToString();
            }
            catch (Exception)
            {
                Logger.Warning("Could not parse priority. [{0}]", rawMessage);
                Invalid = true;
                return;
            }

            DateTime notUsed;
            Invalid = !DateTime.TryParse(splitLine[1], out notUsed);

            Timestamp = splitLine[1];
            Hostname = splitLine[2].Trim();
            ApplicationName = splitLine[3].Trim();
            Message = _DateStampRegex.Replace(string.Join(" ", splitLine.Skip(4)).Trim(), string.Empty).Trim();
        }

        private bool ParseRFC5424()
        {
            var regex = new Regex(_rfc5424Regex);
            var match = Regex.Match(RawMessage, _rfc5424Regex);

            //check for rfc5424, return to original match if no match
            if (!match.Success)
            {
                //ensure Invalid is set before return
                Invalid = true;
                return false;
            }

            //extract values from syslog
            Timestamp = match.Groups["TIMESTAMP"].Value;
            Hostname = match.Groups["HOSTNAME"].Value;
            ApplicationName = match.Groups["APPNAME"].Value;
            Message = match.Groups["MSG"].Value;
            MessageID = match.Groups["MSGID"].Value;
            StructuredData = match.Groups["SD"].Value;
            ProcID = match.Groups["PROCID"].Value;

            if (StructuredData != "-")
            {
                ParseStructuredData(StructuredData);
            }

            //ensure Invald = false
            Invalid = false;

            return true;
        }

        private void ParseStructuredData(string sd)
        {
            var sdmatches = Regex.Matches(sd, _sdpairRegex);
            if (sdmatches.Count > 0)
            {
                foreach (Match sdmatch in sdmatches)
                {
                    StructuredDataElements.Add(sdmatch.Groups["PARAMNAME"].Value, sdmatch.Groups["PARAMVALUE"].Value);
                }
            }

        }

        public bool Invalid { get; private set; }

        public string RawMessage { get; private set; }

        public string Timestamp { get; set; }

        public string Level { get; set; }

        public string Facility { get; set; }

        public string Hostname { get; set; }
        
        public string ApplicationName { get; set; }
        
        public string Message { get; set; }

        public string MessageID { get; set; }

        public string StructuredData { get; set; }

        public string ProcID { get; private set; }

        public Dictionary<string, string> StructuredDataElements { get; set; }

        public override string ToString()
        {

            return JsonConvert.SerializeObject(new SeqEventMessage()
            {
                Level = Level,
                Timestamp = Timestamp,
                MessageTemplate = Configuration.MessageTemplate,
                Properties = new { Facility, Hostname, ApplicationName, Message, StructuredDataElements }
            });
        }
    }
}
