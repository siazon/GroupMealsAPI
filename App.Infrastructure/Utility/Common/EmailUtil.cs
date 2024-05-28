using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using App.Domain.Common.Setting;
using App.Infrastructure.Extensions.Entity;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace App.Infrastructure.Utility.Common
{
    public interface IEmailUtil
    {
        Task<bool> SendEmail(List<DbSetting> settings, string fromEmail, string fromEmailDescription, string toEmail, string toEmailDescription, string subject, string body,
            string bodyHtml, string CCEmail = null, List<string> attachments = null, bool important = true);

        Task<bool> SendEventEmail(List<DbSetting> settings, string fromEmail, string fromEmailDescription, List<string> toEmails, string subject, string body,
            string bodyHtml, string description, string location, DateTime startTime, DateTime endTime, int? eventID = null, bool isCancel = false, bool important = true);

        bool IsValidEmail(string email);
    }

    public class EmailUtil : IEmailUtil
    {
        public async Task<bool> SendEmail(List<DbSetting> settings, string fromEmail, string fromEmailDescription, string toEmail, string toEmailDescription, 
            string subject, string body, string bodyHtml,string CCEmail=null, List<string> attachments = null, bool important = true)
        {
            try
            {
                if (!settings.GetEmailSettingsSmtpEnabled())
                    return false;

                var apiKey = settings.GetEmailSettingsAPIKey();
                var client = new SendGridClient(apiKey);

                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromEmailDescription),
                    Subject = subject,
                    PlainTextContent = null,
                    HtmlContent = bodyHtml
                };

                msg.AddTo(new EmailAddress(toEmail));
                if(CCEmail!= null) 
                msg.AddCc(new EmailAddress(toEmail));

                if (string.IsNullOrEmpty(fromEmail))
                    msg.From = new EmailAddress("noreply@groupmeals.com", "Groupmeals.com");

                if (important)
                {
                    var headers = new Dictionary<string, string> { { "Priority", "Urgent" }, { "Importance", "high" } };
                    msg.AddHeaders(headers);
                }

                var response = await client.SendEmailAsync(msg);

                return response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {
                Trace.TraceError("Util:[EmailException] {0}-{1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public async Task<bool> SendEventEmail(List<DbSetting> settings, string fromEmail, string fromEmailDescription, List<string> toEmails, string subject, string body, string bodyHtml,
            string description, string location, DateTime startTime, DateTime endTime, int? eventID = null,
            bool isCancel = false, bool important = true)
        {
            try
            {
                if (!settings.GetEmailSettingsSmtpEnabled())
                    return false;

                var apiKey = settings.GetEmailSettingsAPIKey();
                var client = new SendGridClient(apiKey);

                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromEmailDescription),
                    Subject = subject,
                    PlainTextContent = null,
                    HtmlContent = bodyHtml
                };

                foreach (var toEmail in toEmails)
                {
                    msg.AddTo(new EmailAddress(toEmail));
                }

                if (string.IsNullOrEmpty(fromEmail))
                    msg.From = new EmailAddress("no-reply@wiiya.com", "Wiiya Ireland");

                if (important)
                {
                    var headers = new Dictionary<string, string> { { "Priority", "Urgent" }, { "Importance", "high" } };
                    msg.AddHeaders(headers);
                }

                var calendarContent = MeetingRequestString(fromEmail, toEmails[0], subject, description, location, startTime, endTime);
                using (var stream = GenerateStreamFromString(calendarContent))
                {
                    await msg.AddAttachmentAsync("booking.ics", stream);
                }

                var response = await client.SendEmailAsync(msg);

                return response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {
                Trace.TraceError("Util:[EmailException] {0}-{1}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private string MeetingRequestString(string from, string toUser, string subject, string desc, string location, DateTime startTime, DateTime endTime, int? eventID = null, bool isCancel = false)
        {
            var str = new StringBuilder();

            str.AppendLine("BEGIN:VCALENDAR");
            str.AppendLine("PRODID:-//Microsoft Corporation//Outlook 12.0 MIMEDIR//EN");
            str.AppendLine("VERSION:2.0");
            str.AppendLine(string.Format("METHOD:{0}", (isCancel ? "CANCEL" : "REQUEST")));
            str.AppendLine("BEGIN:VEVENT");

            str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", startTime.ToUniversalTime()));
            str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmss}", DateTime.UtcNow));
            str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", endTime.ToUniversalTime()));
            str.AppendLine(string.Format("LOCATION: {0}", location));
            str.AppendLine(string.Format("UID:{0}", (eventID.HasValue ? "blablabla" + eventID : Guid.NewGuid().ToString())));
            str.AppendLine(string.Format("DESCRIPTION:{0}", desc.Replace("\n", "<br>")));
            str.AppendLine(string.Format("X-ALT-DESC;FMTTYPE=text/html:{0}", desc.Replace("\n", "<br>")));
            str.AppendLine(string.Format("SUMMARY:{0}", subject));

            str.AppendLine(string.Format("ORGANIZER;CN=\"{0}\":MAILTO:{1}", from, from));
            str.AppendLine(string.Format("ATTENDEE;CN=\"{0}\";RSVP=TRUE:mailto:{1}", toUser, toUser));

            str.AppendLine("BEGIN:VALARM");
            str.AppendLine("TRIGGER:-PT15M");
            str.AppendLine("ACTION:DISPLAY");
            str.AppendLine("DESCRIPTION:Reminder");
            str.AppendLine("END:VALARM");
            str.AppendLine("END:VEVENT");
            str.AppendLine("END:VCALENDAR");

            return str.ToString();
        }
    }
}