using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace SolrCommand.Core
{
    /// <summary>
    /// This class is used to send email notification messages.
    /// </summary>
    public class Notification
    {
        //Fields
        private static string _from;
        private static string _to;
        private static string _smtpHost;
        private static NotifyOptions _notify;
        private static NetworkCredential _smtpCredential;


        //Properties
        /// <summary>
        /// Gets or Sets the From address for notification email messages.
        /// </summary>
        public static string From
        {
            get { return _from; }
            set { _from = value; }
        }

        /// <summary>
        /// Gets or Sets the To address for notification email messages.
        /// </summary>
        public static string To
        {
            get { return _to; }
            set { _to = value; }
        }

        /// <summary>
        /// Gets or Sets the name or ip address of the host used for SMTP transactions.
        /// </summary>
        public static string SmtpHost
        {
            get { return _smtpHost; }
            set { _smtpHost = value; }
        }

        /// <summary>
        /// Gets or sets a boolean value used to control if email messages are to be sent or not.
        /// </summary>
        public static NotifyOptions Notify
        {
            get { return _notify; }
            set { _notify = value; }
        }

        public enum NotifyOptions
        {
            All,
            Warnings,
            Errors,
            None
        }

        /// <summary>
        /// Gets a value indicating if notifications can be sent based on the properties required to send messages (From, To, and SmtpHost).
        /// </summary>
        public static bool IsEmailNotificationEnabled
        {
            get
            {
                //If we have all required param values to send email, then set the sendEmail bool value to true
                return (Notify != NotifyOptions.None &&
                    !string.IsNullOrEmpty(From) &&
                    !string.IsNullOrEmpty(To) &&
                    !string.IsNullOrEmpty(SmtpHost));
            }
        }

        /// <summary>
        /// Sets the credentials used to send email notifications.  If credentials are not explicitly set,
        /// default credentials will be used.  Should contain comma delimited values for username, password, domain (in that order)
        /// </summary>
        public static string SmtpCredentialString
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    string[] creds = value.Split(",".ToCharArray());
                    if (creds.Length == 3)
                    {
                        _smtpCredential = new NetworkCredential(creds[0].ToString().Trim(), creds[1].ToString().Trim(), creds[2].ToString().Trim());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current credentials used to send email notifications if they have explicitly been set.
        /// </summary>
        public static NetworkCredential SmtpCredential
        {
            get { return _smtpCredential; }
        }

        //Constructors
        /// <summary>
        /// This class is not meant to be instantiated.
        /// </summary>
        private Notification()
        {

        }

        //Public Methods
        /// <summary>
        /// Sends an email message with the subject, body and priority provided.
        /// </summary>
        /// <param name="subject">The subject of the email message to send.</param>
        /// <param name="body">The body of the email message to send.</param>
        /// <param name="mailPriority">The priority of the email message to send.</param>
        /// <returns>String containing status message.</returns>
        public static string SendEmail(string subject, string body, MailPriority mailPriority)
        {
            if (IsEmailNotificationEnabled)
            {
                try
                {
                    System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(From, To);
                    msg.Subject = subject;
					msg.Body = string.Format("<pre>{0}</pre>", body);
                    msg.Priority = mailPriority;
                    msg.IsBodyHtml = true;
					
                    System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(SmtpHost);
                    if (SmtpCredential != null)
                    {
                        smtp.Credentials = SmtpCredential;
                    }
                    smtp.Send(msg);

                    return "Email sent.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email notification: " + ex.Message);
                    return string.Format("Error sending email: {0}; Exception: {1}", ex.Message, ex.ToString());
                }
            }
            else
            {
                return "Email notification is not enabled.";
            }
        }

    }
}
