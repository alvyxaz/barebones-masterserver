using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class Mailer : MonoBehaviour
    {
        public virtual bool SendMail(string to, string subject, string body)
        {
            throw new NotImplementedException("SendMail method needs to be overriden");
        }
    }

}

