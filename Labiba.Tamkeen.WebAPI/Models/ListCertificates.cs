using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labiba.Tamkeen.WebAPI.Models
{
    public class ListCertificates
    {


        public class Certificateobject
        {
            public Datum[] data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
        }

        public class Datum
        {
            public string name { get; set; }
            public string id { get; set; }
        }

        public class CertificateRequest
        {
            public string Name { get; set; }
            public string Field { get; set; }
            public string Language { get; set; }
            public string Page { get; set; }

        }

        public class CertificateActionRequest
        {
            public string Name { get; set; }
            public string Language { get; set; }
            public string field { get; set; }
            public string pageIndex { get; set; }


        }


        public class CertificatesDetailsobject
        {
            public Data data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
        }

        public class Data
        {
            public string eligibility { get; set; }
            public string payment_structure { get; set; }
            public string payment_cap { get; set; }
            public string awarding { get; set; }
            public ProviderResponse[] providers { get; set; }
            public string name { get; set; }
            public string id { get; set; }
        }

        public class ProviderResponse
        {
            public string email { get; set; }
            public string phone { get; set; }
            public string name { get; set; }
            public string id { get; set; }
        }

        public class DetailsRequest
        {
            public string Id { get; set; }
            public string Language { get; set; }
          

        }
        public class DetailsActionRequest
        {
            public string Id { get; set; }
            public string Language { get; set; }
          

        }



    }

}