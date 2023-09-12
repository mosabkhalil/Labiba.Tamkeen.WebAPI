using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labiba.Tamkeen.WebAPI.Models
{
    public class TemplatMessage
    {
        public string PageId { get; set; }
        public string Channel { get; set; }
        public string[] RecipientIds { get; set; }
        public Message[] Messages { get; set; }
    }

    public class Message
    {
        public string Type { get; set; }
        public string TemplateName { get; set; }
        public string Language { get; set; }
        public Templatemesssageparamaters TemplateMesssageParamaters { get; set; }
    }

    public class Templatemesssageparamaters
    {
        public Body[] Body { get; set; }
        public Option[] Options { get; set; }
    }

    public class Body
    {
        public string text { get; set; }
    }

    public class Option
    {
        public string value { get; set; }
    }

    public class TemplateReq
    {
        public string UserPhoneNumber { get; set; }
        public string CustomerName { get; set; }
        public string PaymentReferenceNumber { get; set; }


    }
    //CheckExpiryReq
    public class CheckExpiryReq
    {
        public string Token { get; set; }
    }


    public class GetNewAuthReq
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


    public class PassUserScopedParameters
    {
        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public USParameter[] Parameters { get; set; }
    }

    public class USParameter
    {
        public string key { get; set; }
        public string value { get; set; }
    }

}