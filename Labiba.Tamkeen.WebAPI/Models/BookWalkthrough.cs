using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labiba.Tamkeen.WebAPI.Models
{
    public class BookWalkthrough
    {


        public class Bookobject
        {
            public BookEvent[] data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
        }

        public class BookEventobject
        {
            public int status { get; set; }
            public string msg { get; set; }
        }
        public class BookEventAction
        {
            public string id { get; set; }
            public string name { get; set; }
            public string cpr { get; set; }
            public string phone { get; set; }
            public string block { get; set; }
            public string email { get; set; }
            public string gender { get; set; }
            public string segment { get; set; }
            public string Language { get; set; }
        }

        public class BookEventRequest
        {
            public string id { get; set; }
            public string name { get; set; }
            public string cpr { get; set; }
            public string phone { get; set; }
            public string block { get; set; }
            public string email { get; set; }
            public string gender { get; set; }
            public string segment { get; set; }
            public string Language { get; set; }
        }

        public class BookEvent
        {
            public string start_at { get; set; }
            public string end_at { get; set; }
            public string venue { get; set; }
            public int available_seats { get; set; }
            public string name { get; set; }
            public string id { get; set; }
        }

        public class BookActionRequest
        {
            public string name { get; set; }
            public string id { get; set; }
            public string language { get; set; }
        }

        public class BookRequest
        {
            public string name { get; set; }

            public string id { get; set; }

            public string language { get; set; }
        }

        public class Walkthroughsobject
        {
            public Walkthroughs[] data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
        }

        public class Walkthroughs
        {
            public string start_at { get; set; }
            public string end_at { get; set; }
            public string lang { get; set; }
            public string venue { get; set; }
            public int available_seats { get; set; }
            public string name { get; set; }
            public string id { get; set; }
        }
        public class WalkthroughsAction
        {
            public string Date { get; set; }
            public string language { get; set; }
            public string id { get; set; }
        }

        public class WalkthroughsActionRequest
        {
            public string Date { get; set; }
            public string language { get; set; }
            public string id { get; set; }

        }

        public class WalkthroughsBookobject
        {
            public string sessionid { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
        }

        public class WalkthroughsBookAction
        {
            public string id { get; set; }
            public string mobile { get; set; }
            public string otp { get; set; }
        }
        public class WalkthroughsBookRequest
        {
            public string id { get; set; }
            public string mobile { get; set; }
            public string otp { get; set; }
        }
    }
}



