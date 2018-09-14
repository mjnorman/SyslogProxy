//<78>2015-02-10T19:26:01.889893+00:00 mongo-monitor CROND  (ec2-user) CMD (/home/ec2-user/bin/run_node_metrics)


namespace SyslogProxy.UnitTests
{
    using FluentAssertions;
    using global::SyslogProxy.Messages;
    using System;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using Xunit;

    public class SyslogJsonTest
    {
        [Fact]
        public void CanParseThing()
        {
            var thing = new JsonSyslogMessage("78 2015-02-10T19:26:01.889893+00:00 mongo-monitor CROND  (ec2-user) CMD (/home/ec2-user/bin/run_node_metrics)");

            //Program.WriteToSeq(thing).Wait();
        }

        [Fact]
        public void CanParseRFC5424WithStructuredData()
        {
            JsonSyslogMessage syslog = new JsonSyslogMessage(@"164 <14>1 2018-09-07T16:15:38.477531+00:00 NCO.MOM.mom fea3e98a-f9da-4705-91f0-80ef34b38b31 [0] - [gauge@47450 name=""disk_quota"" value=""1.073741824e+09"" unit=""bytes""] ");

            syslog.Invalid.Should().BeFalse();
            syslog.Timestamp.Should().Be("2018-09-07T16:15:38.477531+00:00");
            syslog.Hostname.Should().Be("NCO.MOM.mom");
            syslog.ApplicationName.Should().Be("fea3e98a-f9da-4705-91f0-80ef34b38b31");
            syslog.StructuredData.Should().Be(@"[gauge@47450 name=""disk_quota"" value=""1.073741824e+09"" unit=""bytes""]");
            syslog.StructuredDataElements.Count.Should().Be(3);
            syslog.StructuredDataElements["name"].Should().Be("disk_quota");
            syslog.StructuredDataElements["value"].Should().Be(1.073741824e+09);
            syslog.StructuredDataElements["unit"].Should().Be("bytes");
            syslog.Message.Should().BeEmpty();
            syslog.ProcID.Should().Be("[0]");
        }

        [Fact]
        public void CanParseRFC5424WithoutStructuredData()
        {
            JsonSyslogMessage syslog = new JsonSyslogMessage(@"164 <14>1 2018-09-13T17:02:34.936791+00:00 hostname app-name [APP/PROC/WEB/0] - - message is here");

            syslog.Invalid.Should().BeFalse();
            syslog.Timestamp.Should().Be("2018-09-13T17:02:34.936791+00:00");
            syslog.Hostname.Should().Be("hostname");
            syslog.ApplicationName.Should().Be("app-name");
            syslog.StructuredData.Should().Be(@"-");
            syslog.ProcID.Should().Be("[APP/PROC/WEB/0]");
            syslog.Message.Should().Be("message is here");
            syslog.MessageID.Should().Be("-");
        }

        [Fact]
        public void CanOverrideToStringProperly()
        {
            JsonSyslogMessage syslog = new JsonSyslogMessage(@"164 <14>1 2018-09-07T16:15:38.477531+00:00 NCO.MOM.mom fea3e98a-f9da-4705-91f0-80ef34b38b31 [0] - [gauge@47450 name=""disk_quota"" value=""1"" unit=""bytes""] ");
            syslog.ToString().Should().Be(@"{""Timestamp"":""2018-09-07T16:15:38.477531+00:00"",""Level"":null,""Properties"":{""Facility"":null,""Hostname"":""NCO.MOM.mom"",""ApplicationName"":""fea3e98a-f9da-4705-91f0-80ef34b38b31"",""Message"":"""",""StructuredDataElements"":{""name"":""disk_quota"",""value"":1,""unit"":""bytes""}},""MessageTemplate"":null}");
        }
    }
}
