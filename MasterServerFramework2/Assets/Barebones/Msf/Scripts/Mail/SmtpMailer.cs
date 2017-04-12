using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Barebones.Logging;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class SmtpMailer : Mailer
    {
        [Header("E-mail settings")]
        public string SmtpHost = "smtp.gmail.com";
        public int SmtpPort = 587;
        public string SmtpUsername = "username@gmail.com";
        public string SmtpPassword = "password";
        public string EmailFrom = "YourGame@gmail.com";
        public string SenderDisplayName = "Awesome Game";

        private List<Exception> _sendMailExceptions;

#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
        protected SmtpClient SmtpClient;
#endif

        private BmLogger Logger = Msf.Create.Logger(typeof(SmtpMailer).Name);

        protected virtual void Awake()
        {
            _sendMailExceptions = new List<Exception>();
            SetupSmtpClient();
        }

        protected virtual void Update()
        {
            // Log errors for any exceptions that might have occured
            // when sending mail
            if (_sendMailExceptions.Count > 0)
            {
                lock (_sendMailExceptions)
                {
                    foreach (var exception in _sendMailExceptions)
                    {
                        Logger.Error(exception);
                    }

                    _sendMailExceptions.Clear();
                }
            }
        }

        protected virtual void SetupSmtpClient()
        {
#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
            // Configure mail client
            SmtpClient = new SmtpClient(SmtpHost, SmtpPort);

            // set the network credentials
            SmtpClient.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword) as ICredentialsByHost;
            SmtpClient.EnableSsl = true;

            SmtpClient.SendCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    lock (_sendMailExceptions)
                    {
                        _sendMailExceptions.Add(args.Error);
                    }
                }
            };

            ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                { return true; };
#endif
        }

        public override bool SendMail(string to, string subject, string body)
        {
#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
            // Create the mail message (from, to, subject, body)
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(EmailFrom, SenderDisplayName);
            mailMessage.To.Add(to);

            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;
            mailMessage.Priority = MailPriority.High;

            // send the mail
            SmtpClient.SendAsync(mailMessage, "");
#endif
            return true;
        }
    }
}