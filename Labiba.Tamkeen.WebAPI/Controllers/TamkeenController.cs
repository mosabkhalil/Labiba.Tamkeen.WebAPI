using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Labiba.Actions.Logger;
using Labiba.Actions.Logger.Manager;
using Labiba.Tamkeen.WebAPI.Models;
using Newtonsoft.Json;
using static Labiba.Tamkeen.WebAPI.Models.LabibaResponses;
using static Labiba.Tamkeen.WebAPI.Models.LabibaResponses.HeroCardsModel;
using static Labiba.Tamkeen.WebAPI.Models.ListCertificates;
using static Labiba.Tamkeen.WebAPI.Models.BookWalkthrough;
using static Labiba.Tamkeen.WebAPI.Models.FollowUp;
using static Labiba.Tamkeen.WebAPI.Models.LiteDBServices;
using System.Globalization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System.Web.Hosting;
using System.IO;
using System.Threading;
using System.Configuration;

namespace Labiba.Tamkeen.WebAPI.Controllers
{
    public class TamkeenController : ApiController
    {
        static string ApplicationName = "Google Sheets API .NET Quickstart";
        static string[] Scopes = { SheetsService.Scope.Spreadsheets, SheetsService.Scope.Drive, SheetsService.Scope.DriveFile };

        string spreadsheetId = $"{ConfigurationManager.AppSettings["spreadsheetId"]}";
        String spreadsheetId2 = "1N4V1VrK8H3IFBCo-t2CJCMPjpIY7HidtbJ-8ByqDIwo";
        String sheet2 = "DB Mapping";
        string sheet1 = $"{ConfigurationManager.AppSettings["sheet1"]}";

        public SheetsService initializeService()
        {

            UserCredential credential;
            string Path = HostingEnvironment.MapPath("~/credentials.json");
            using (var stream =
                new FileStream(Path, FileMode.Open, FileAccess.ReadWrite))
            {
                string credPath = HostingEnvironment.MapPath("~/token.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        private async Task<IList<IList<Object>>> GetSheet1()
        {

            var service = initializeService();

            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, sheet1);

            ValueRange response = await request.ExecuteAsync();

            IList<IList<Object>> values = response.Values;

            return values;
        }


        public int pageSize = 9;
        [HttpPost]
        [Route("api/Tamkeen/ListCertificates")]
        [LogAction(ActionId = 4559, ClientId = 8708)]
        public async Task<HttpResponseMessage> ListCertificates(CertificateActionRequest certificate)
        {

            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);

            StateModel stateModel = new StateModel();
            List<hero_cards> cardResult = new List<hero_cards>();
            RootObject cardsRootObject = new RootObject();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(certificate);
            logDetails.URL = $"{ConfigurationManager.AppSettings["APIURLV2"]}/Certificates/List";
            logDetails.Forms = "";
            logDetails.Headers = "";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 
                if (string.IsNullOrWhiteSpace(certificate.Name) && string.IsNullOrWhiteSpace(certificate.field))
                {
                    stateModel.state = "Failure";

                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                    LogModel modell = new LogModel();
                    modell.LogTime = DateTime.Now;
                    modell.ActionName = "ListCertificates";
                    modell.Parameter = JsonConvert.SerializeObject(certificate);
                    // modell.LogText = responseBody;
                    modell.ResponseFromLabiba = JsonConvert.SerializeObject(stateModel);
                    LiteDBServices liteDBb = new LiteDBServices();
                    liteDBb.InsertLogRow(modell);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

                if (string.IsNullOrWhiteSpace(certificate.Language))
                {
                    certificate.Language = "en";
                }

                int page = 1;
                if (!string.IsNullOrWhiteSpace(certificate.pageIndex))
                {
                    //nextpage_2
                    if (certificate.pageIndex.Contains("nextPage_"))
                    {
                        page = int.Parse(certificate.pageIndex.Replace("nextPage_", ""));
                    }
                }

                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                CertificateRequest requestbody = new CertificateRequest();
                requestbody.Name = certificate.Name;
                requestbody.Language = certificate.Language;
                requestbody.Field = certificate.field;
                requestbody.Page = certificate.pageIndex;


                var reqBody = JsonConvert.SerializeObject(requestbody);
                logDetails.JSON = reqBody;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Certificates/List"),
                    Content = new StringContent(reqBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);

                var certificat = JsonConvert.DeserializeObject<Certificateobject>(responseBody);
                var certificatList = certificat.data.ToList();
                //if (certificat.status != 200)
                //{
                //    stateModel.state = "Failure";
                //    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                //}
                if (certificatList.Count == 0)
                {
                    stateModel.state = "not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);


                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);

                }
                if (certificatList.Count == 1)
                {
                    foreach (var certific in certificat.data)
                    {

                        stateModel.state = "Single";
                        stateModel.SlotFillingState = certific.id;

                    }

                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);


                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                int numResult = certificat.data.Count();
                double num = (double)numResult / pageSize;
                var numPages = Math.Ceiling(num);
                var dataList = certificat.data.ToList();
                dataList = dataList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                if (certificatList.Count > 1)
                {
                    foreach (var data in dataList)
                    {
                        if (certificate.Language.ToLower().Trim() == "ar")
                        {
                            cardResult.Add(new hero_cards()
                            {
                                Title = data.name,
                                Subtitle = certificate.field,
                                Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = "التفاصيل", Type = "postback", Value = data.id } }
                            });
                        }
                        if (certificate.Language.ToLower().Trim() == "en")
                        {
                            cardResult.Add(new hero_cards()
                            {
                                Title = data.name,
                                Subtitle = certificate.field,
                                Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = "Details", Type = "postback", Value = data.id } }
                            });
                        }
                    }

                    if (page < numPages)
                    {
                        var moreTitle = "More";
                        if (certificate.Language.ToLower().Trim() == "ar")
                        {
                            moreTitle = "المزيد";
                        }
                        int nextPage = page + 1;
                        cardResult.Add(new hero_cards()
                        {
                            Title = moreTitle,
                            Subtitle = " ",
                            Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = moreTitle, Type = "postback", Value = $"nextPage_{nextPage}" } }
                        });

                    }
                }



                cardsRootObject.hero_cards = cardResult;
                cardsRootObject.response = "Success";
                cardsRootObject.success_message = "Here are some results that I found";
                cardsRootObject.failure_message = "Oops. I couldn't find anything";


                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(cardsRootObject);

                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, cardsRootObject, Configuration.Formatters.JsonFormatter);
            }

            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                ActionLogger.LogException(ex, logDetails);

                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }

        }

        [HttpPost]
        [Route("api/Tamkeen/CertificatesDetails")]
        [LogAction(ActionId = 4561, ClientId = 8708)]
        public async Task<HttpResponseMessage> CertificatesDetails(DetailsActionRequest details)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            TextModel textModel = new TextModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(details);

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;


                if (string.IsNullOrWhiteSpace(details.Id))
                {

                    stateModel.state = "Failure";

                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);



                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);

                }
                if (string.IsNullOrWhiteSpace(details.Language))
                {
                    details.Language = "en";
                }

                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                DetailsRequest requestbody = new DetailsRequest();
                requestbody.Id = details.Id;
                requestbody.Language = details.Language;


                var reqBody = JsonConvert.SerializeObject(requestbody);
                logDetails.JSON = reqBody;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Certificates/Details/{details.Id}"),
                    Content = new StringContent(reqBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var Details = JsonConvert.DeserializeObject<CertificatesDetailsobject>(responseBody);
                var detailslist = Details.data;

                if (detailslist == null)
                {
                    stateModel.state = "not_found";



                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

                if (detailslist != null && details.Language.ToLower().Trim() == "ar")
                {

                    textModel.text = $"الاسم: {Details.data.name}<br>" + $"أهلية الأنتساب: {Details.data.eligibility}<br>" + $"آلية الدفع: {Details.data.payment_structure}<br>"
                        + $"رسوم الشهادة: {Details.data.payment_cap}<br><br><br>";
                    foreach (var prov in Details.data.providers)
                    {

                        textModel.text = textModel.text + $"مزودو خدمة التدريب:<br>" + $"اسم مزود خدمة التدريب: {prov.name}<br>" + $"البريد الألكتروني: {prov.email}<br>" + $"رقم الهاتف: {prov.phone}<br>";
                    }

                    stateModel.state = "Success";
                }
                if (detailslist != null && details.Language.ToLower().Trim() == "en")
                {

                    textModel.text = $"Name: {Details.data.name}<br>" + $"Eligibility: {Details.data.eligibility}<br>" + $"Payment Structure: {Details.data.payment_structure}<br>"
                        + $"Certificate Fees: {Details.data.payment_cap}<br><br><br>";
                    foreach (var prov in Details.data.providers)
                    {
                        textModel.text = textModel.text + $"Providers:<br>" + $"Provider Name: {prov.name}<br>" + $"Email: {prov.email}<br>" + $"Phone: {prov.phone}<br> ";
                    }

                    stateModel.state = "Success";
                }

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(textModel);
                ActionLogger.LogDetails(logDetails);

                return Request.CreateResponse(HttpStatusCode.OK, textModel, Configuration.Formatters.JsonFormatter);
            }

            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                ActionLogger.LogException(ex, logDetails);

                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ListEvents")]
        [LogAction(ActionId = 4620, ClientId = 8708)]
        public async Task<HttpResponseMessage> ListEvents(BookActionRequest bookRequest)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            List<hero_cards> cardResult = new List<hero_cards>();
            RootObject cardsRootObject = new RootObject();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(bookRequest);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                if (string.IsNullOrWhiteSpace(bookRequest.language))
                {
                    bookRequest.language = "en";
                }
                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                BookRequest requestbody = new BookRequest();

                requestbody.name = bookRequest.name;
                requestbody.language = bookRequest.language;

                var reqBody = JsonConvert.SerializeObject(requestbody);
                logDetails.JSON = reqBody;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Events/List"),
                    Content = new StringContent(reqBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var Eventbook = JsonConvert.DeserializeObject<Bookobject>(responseBody);
                var EventList = Eventbook.data.ToList();
                //if (Eventbook.status != 200)
                //{
                //    stateModel.state = "Failure";
                //    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                //}
                if (EventList.Count == 0)
                {
                    stateModel.state = "not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);


                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (EventList.Count == 1)
                {
                    foreach (var eventt in Eventbook.data)
                    {
                        stateModel.state = "One_Event";
                        stateModel.SlotFillingState = eventt.id;
                    }
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);


                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (EventList.Count > 1)
                {
                    if (bookRequest.language.ToLower().Trim() == "ar")
                    {
                        var count = 0;
                        foreach (var eventt in Eventbook.data)
                        {
                            count++;
                            cardResult.Add(new hero_cards()
                            {
                                Title = eventt.name,
                                Subtitle = eventt.venue + "<br>" + startAtDate(eventt.start_at),
                                Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = "التفاصيل", Type = "postback", Value = eventt.id } }
                            });
                            if (count == 0)
                            { break; }
                        }
                    }
                    if (bookRequest.language.ToLower().Trim() == "en")
                    {
                        var count = 0;
                        foreach (var eventt in Eventbook.data)
                        {
                            count++;
                            cardResult.Add(new hero_cards()
                            {
                                Title = eventt.name,
                                Subtitle = eventt.venue + "<br>" + startAtDate(eventt.start_at),
                                Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = "Details", Type = "postback", Value = eventt.id } }
                            });
                            if (count == 5)
                            { break; }
                        }
                    }
                }
                cardsRootObject.hero_cards = cardResult;
                cardsRootObject.response = "Success";
                cardsRootObject.success_message = "Here are some results that I found";
                cardsRootObject.failure_message = "Oops. I couldn't find anything";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(cardsRootObject);


                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, cardsRootObject, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                ActionLogger.LogException(ex, logDetails);


                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }

        }

        [HttpPost]
        [Route("api/Tamkeen/GetEventsDetails")]
        [LogAction(ActionId = 4621, ClientId = 8708)]
        public async Task<HttpResponseMessage> GetEventsDetails(BookActionRequest bookRequest)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            TextModel textModel = new TextModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(bookRequest);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                if (string.IsNullOrWhiteSpace(bookRequest.language))
                {
                    bookRequest.language = "en";
                }

                if (string.IsNullOrWhiteSpace(bookRequest.id))
                {
                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);


                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                BookRequest requestbody = new BookRequest();
                requestbody.id = bookRequest.id;
                requestbody.language = bookRequest.language;

                var reqBody = JsonConvert.SerializeObject(requestbody);
                logDetails.JSON = reqBody;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Events/List"),
                    Content = new StringContent(reqBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var EventList = JsonConvert.DeserializeObject<Bookobject>(responseBody);
                //if (EventList.status != 200)
                //{
                //    stateModel.state = "Failure";

                //    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                //}
                if (EventList.data == null)
                {
                    stateModel.state = "not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                //var EventId = EventList.data.Where(x => x.id ==).FirstOrDefault();
                if (EventList.data != null && bookRequest.language.ToLower().Trim() == "ar")
                {
                    var count = 0;
                    foreach (var eventt in EventList.data)
                    {
                        DateTime outDate;
                        string starttime = $"{eventt.start_at}";
                        bool IsValidfromDateFormat = DateTime.TryParseExact(starttime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outDate);
                        if (IsValidfromDateFormat)
                        {
                            starttime = outDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            starttime = $"{eventt.start_at}";
                        }
                        DateTime outendDate;
                        string endtime = $"{eventt.end_at}";
                        bool IsValidfromEndDateFormat = DateTime.TryParseExact(endtime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outendDate);
                        if (IsValidfromDateFormat)
                        {
                            endtime = outendDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            endtime = $"{eventt.end_at}";
                        }
                        count++;
                        textModel.text = textModel.text + $"الاسم: {eventt.name}<br>" + $"تاريخ البدء: {starttime}<br>" + $"تاريخ الإنتهاء: {endtime}<br>" + $"المكان: {eventt.venue}<br>" + $"المقاعد المتاحة: {eventt.available_seats}<br><br>";
                        if (count == 5)
                        { break; }
                    }
                }
                if (EventList.data != null && bookRequest.language.ToLower().Trim() == "en")
                {
                    var count = 0;
                    foreach (var eventt in EventList.data)
                    {
                        DateTime outDate;
                        string starttime = $"{eventt.start_at}";
                        bool IsValidfromDateFormat = DateTime.TryParseExact(starttime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outDate);
                        if (IsValidfromDateFormat)
                        {
                            starttime = outDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            starttime = $"{eventt.start_at}";
                        }
                        DateTime outendDate;
                        string endtime = $"{eventt.end_at}";
                        bool IsValidfromEndDateFormat = DateTime.TryParseExact(endtime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outendDate);
                        if (IsValidfromDateFormat)
                        {
                            endtime = outendDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            endtime = $"{eventt.end_at}";
                        }
                        count++;
                        textModel.text = textModel.text + $"Name: {eventt.name}<br>" + $"Start Date: {starttime}<br>" + $"End Date: {endtime}<br>" + $"Venue: {eventt.venue}<br>" + $"Available Seats: {eventt.available_seats}<br><br>";
                        if (count == 5)
                        { break; }
                    }
                }

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(textModel);
                ActionLogger.LogDetails(logDetails);

                return Request.CreateResponse(HttpStatusCode.OK, textModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);


                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }

        }


        [HttpPost]
        [Route("api/Tamkeen/BookEvents")]
        [LogAction(ActionId = 4622, ClientId = 8708)]
        public async Task<HttpResponseMessage> BookEvents(BookEventAction bookRequest)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                BookEventRequest request = new BookEventRequest();
                logDetails.RequestToApi = JsonConvert.SerializeObject(bookRequest);
                if (string.IsNullOrWhiteSpace(bookRequest.Language))
                {
                    bookRequest.Language = "en";
                    request.Language = bookRequest.Language;
                }

                if (!string.IsNullOrWhiteSpace(bookRequest.id))
                {
                    request.id = bookRequest.id;
                }
                if (!string.IsNullOrEmpty(bookRequest.name))
                {
                    request.name = bookRequest.name;
                }
                if (!string.IsNullOrEmpty(bookRequest.cpr))
                {
                    request.cpr = bookRequest.cpr;
                }
                if (!string.IsNullOrEmpty(bookRequest.phone))
                {
                    request.phone = bookRequest.phone;
                }
                if (!string.IsNullOrEmpty(bookRequest.block))
                {
                    request.block = bookRequest.block;
                }
                if (!string.IsNullOrEmpty(bookRequest.email))
                {
                    request.email = bookRequest.email;
                }
                if (!string.IsNullOrEmpty(bookRequest.gender))
                {
                    request.gender = bookRequest.gender;
                }
                if (!string.IsNullOrEmpty(bookRequest.segment))
                {
                    request.segment = bookRequest.segment;
                }

                //setting (gender,segemnt,block) to empty strings if they were null.
                //if (string.IsNullOrEmpty(bookRequest.block))
                //{
                //    request.block = "";
                //}
                //if (string.IsNullOrEmpty(bookRequest.gender))
                //{
                //    request.gender = "";
                //}
                //if (string.IsNullOrEmpty(bookRequest.segment))
                //{
                //    request.segment = "";
                //}


                var BodyRequest = JsonConvert.SerializeObject(request);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/Events/Book", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);




                var EventsInfo = JsonConvert.DeserializeObject<BookEventobject>(ClientResponseResult);

                if (EventsInfo.status == 200)
                {
                    stateModel.state = "Success";
                    stateModel.SlotFillingState = EventsInfo.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (EventsInfo.status == 401)
                {
                    stateModel.state = "event_not_found";
                    stateModel.SlotFillingState = EventsInfo.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (EventsInfo.status == 400)
                {
                    stateModel.state = "Failure";
                    stateModel.SlotFillingState = EventsInfo.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

                stateModel.state = "Success";
                stateModel.SlotFillingState = EventsInfo.msg;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                //LogModel model = new LogModel();
                //model.LogTime = DateTime.Now;
                //model.ActionName = "BOOKEVENT";
                //model.Parameter = JsonConvert.SerializeObject(bookRequest);
                //model.LogText = JsonConvert.SerializeObject(ex);
                //model.ResponseFromLabiba = JsonConvert.SerializeObject(stateModel);
                //LiteDBServices liteDB = new LiteDBServices();
                //liteDB.InsertLogRow(model);
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }



        [HttpPost]
        [Route("api/Tamkeen/ListWalkthrough")]
        [LogAction(ActionId = 4623, ClientId = 8708)]
        public async Task<HttpResponseMessage> ListWalkthrough(WalkthroughsAction walkRequest)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            List<hero_cards> cardResult = new List<hero_cards>();
            RootObject cardsRootObject = new RootObject();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(walkRequest);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                if (string.IsNullOrEmpty(walkRequest.language))
                {
                    walkRequest.language = "en";
                }
                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                WalkthroughsActionRequest requestbody = new WalkthroughsActionRequest();
                requestbody.language = walkRequest.language;
                if (!string.IsNullOrWhiteSpace(walkRequest.Date))
                {
                    DateTime exDate;
                    bool IsValidexDateFormat = DateTime.TryParseExact(walkRequest.Date, "M/d/yyyy h:mm:ss tt",
                                          System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out exDate);

                    if (IsValidexDateFormat)
                    {
                        requestbody.Date = exDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        bool dateEX = DateTime.TryParseExact(walkRequest.Date, "dd/M/yyyy h:mm:ss tt",
                                          System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out exDate);
                        if (dateEX)
                            requestbody.Date = exDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                }

                var reqBody = JsonConvert.SerializeObject(requestbody);
                logDetails.JSON = reqBody;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Walkthroughs/List"),
                    Content = new StringContent(reqBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var walkthroughs = JsonConvert.DeserializeObject<Walkthroughsobject>(responseBody);
                var walkthroughslist = walkthroughs.data.ToList();
                //if (walkthroughs.status != 200)
                //{
                //    stateModel.state = "Failure";
                //    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                //}
                if (walkthroughslist.Count == 0)
                {
                    stateModel.state = "not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                    //LogModel modell = new LogModel();
                    //modell.LogTime = DateTime.Now;
                    //modell.ActionName = "ListWalkthrough";
                    //modell.Parameter = JsonConvert.SerializeObject(walkRequest);
                    //modell.LogText = responseBody;
                    //modell.ResponseFromLabiba = JsonConvert.SerializeObject(stateModel);
                    //LiteDBServices liteDBb = new LiteDBServices();
                    //liteDBb.InsertLogRow(modell);

                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, cardsRootObject, Configuration.Formatters.JsonFormatter);
                }
                if (walkthroughslist.Count == 1)
                {
                    foreach (var walkk in walkthroughs.data)
                    {
                        stateModel.state = "Single";
                        stateModel.SlotFillingState = walkk.id;
                    }
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                    //LogModel modell = new LogModel();
                    //modell.LogTime = DateTime.Now;
                    //modell.ActionName = "ListWalkthrough";
                    //modell.Parameter = JsonConvert.SerializeObject(walkRequest);
                    //modell.LogText = responseBody;
                    //modell.ResponseFromLabiba = JsonConvert.SerializeObject(stateModel);
                    //LiteDBServices liteDBb = new LiteDBServices();
                    //liteDBb.InsertLogRow(modell);

                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (walkthroughslist.Count > 1)
                {
                    if (walkRequest.language.ToLower().Trim() == "ar")
                    {
                        var count = 0;
                        foreach (var walkk in walkthroughs.data)
                        {
                            count++;
                            cardResult.Add(new hero_cards()
                            {
                                // Title = $"CAROUSEL:{walkk.venue}",
                                Title = walkk.venue,
                                Subtitle = startAtDate(walkk.start_at),
                                Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = "التفاصيل", Type = "postback", Value = walkk.id } }
                            });
                            if (count == 0)
                            { break; }
                        }
                    }
                    if (walkRequest.language.ToLower().Trim() == "en")
                    {
                        var count = 0;
                        foreach (var walkk in walkthroughs.data)
                        {
                            count++;
                            cardResult.Add(new hero_cards()
                            {
                                Title = walkk.venue,
                                Subtitle = startAtDate(walkk.start_at),
                                Buttons = new List<HeroCardsModel.Button>() { new HeroCardsModel.Button() { Title = "Details", Type = "postback", Value = walkk.id } }
                            });
                            if (count == 5)
                            { break; }
                        }
                    }
                }
                cardsRootObject.hero_cards = cardResult;
                cardsRootObject.response = "Success";
                cardsRootObject.success_message = "Here are some results that I found";
                cardsRootObject.failure_message = "Oops. I couldn't find anything";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(cardsRootObject);

                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, cardsRootObject, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);

                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }

        }



        [HttpPost]
        [Route("api/Tamkeen/GetwalkthroughDetails")]
        [LogAction(ActionId = 4624, ClientId = 8708)]
        public async Task<HttpResponseMessage> GetwalkthroughDetails(WalkthroughsAction walkRequest)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            TextModel textModel = new TextModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(walkRequest);
            try
            {
                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                if (string.IsNullOrEmpty(walkRequest.language))
                {
                    walkRequest.language = "en";
                }

                if (string.IsNullOrEmpty(walkRequest.id))
                {
                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);

                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                WalkthroughsActionRequest requestbody = new WalkthroughsActionRequest();
                requestbody.id = walkRequest.id;
                requestbody.language = walkRequest.language;

                var reqBody = JsonConvert.SerializeObject(requestbody);
                logDetails.JSON = reqBody;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Walkthroughs/List"),
                    Content = new StringContent(reqBody, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var walkthrough = JsonConvert.DeserializeObject<Walkthroughsobject>(responseBody);
                //if (walkthrough.status != 200)
                //{
                //    stateModel.state = "Failure";
                //    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                //}
                //if (walkthrough.data.Length == 0)
                //{
                //    stateModel.state = "not_found";
                //    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);

                //}

                if (walkRequest.language.ToLower().Trim() == "ar")
                {
                    var count = 0;
                    foreach (var walkk in walkthrough.data)
                    {
                        DateTime outDate;
                        string starttime = $"{walkk.start_at}";
                        bool IsValidfromDateFormat = DateTime.TryParseExact(starttime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outDate);
                        if (IsValidfromDateFormat)
                        {
                            starttime = outDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            starttime = $"{walkk.start_at}";
                        }
                        DateTime outendDate;
                        string endtime = $"{walkk.end_at}";
                        bool IsValidfromEndDateFormat = DateTime.TryParseExact(endtime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outendDate);
                        if (IsValidfromDateFormat)
                        {
                            endtime = outendDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            endtime = $"{walkk.end_at}";
                        }
                        count++;
                        textModel.text = textModel.text + $"الاسم: {walkk.name}<br>" + $"تاريخ البدء: {starttime}<br>" + $"تاريخ الإنتهاء: {endtime}<br>" + $"المكان: {walkk.venue}<br>" + $"المقاعد المتاحه: {walkk.available_seats}<br><br>";
                        if (count == 5)
                        { break; }
                    }
                }
                if (walkRequest.language.ToLower().Trim() == "en")
                {
                    var count = 0;
                    foreach (var walkk in walkthrough.data)
                    {
                        DateTime outDate;
                        string starttime = $"{walkk.start_at}";
                        bool IsValidfromDateFormat = DateTime.TryParseExact(starttime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outDate);
                        if (IsValidfromDateFormat)
                        {
                            starttime = outDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            starttime = $"{walkk.start_at}";
                        }
                        DateTime outendDate;
                        string endtime = $"{walkk.end_at}";
                        bool IsValidfromEndDateFormat = DateTime.TryParseExact(endtime, "yyyy-MM-dd'T'hh:mm:ssZ",
                                 System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out outendDate);
                        if (IsValidfromDateFormat)
                        {
                            endtime = outendDate.ToUniversalTime().ToString("dd-MM-yyyy hh-mm-ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            endtime = $"{walkk.end_at}";
                        }
                        count++;
                        textModel.text = textModel.text + $"Name: {walkk.name}<br>" + $"Start Date: {starttime}<br>" + $"End Date: {endtime}<br>" + $"Venue: {walkk.venue}<br>" + $"Available Seats: {walkk.available_seats}<br><br>";
                        if (count == 5)
                        { break; }
                    }
                }

                stateModel.state = "Success";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(textModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, textModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);

                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }

        }


        [HttpPost]
        [Route("api/Tamkeen/WalkthroughsBook")]
        [LogAction(ActionId = 4635, ClientId = 8708)]
        public async Task<HttpResponseMessage> WalkthroughsBook(WalkthroughsBookAction walkbookRequest)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(walkbookRequest);
            try
            {
                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                WalkthroughsBookRequest request = new WalkthroughsBookRequest();
                logDetails.JSON = JsonConvert.SerializeObject(walkbookRequest);

                if (!string.IsNullOrEmpty(walkbookRequest.id))
                {
                    request.id = walkbookRequest.id;
                }
                if (!string.IsNullOrEmpty(walkbookRequest.mobile))
                {
                    request.mobile = walkbookRequest.mobile;
                }
                if (!string.IsNullOrEmpty(walkbookRequest.otp))
                {
                    request.otp = walkbookRequest.otp;
                }
                var BodyRequest = JsonConvert.SerializeObject(request);

                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/Walkthroughs/Book", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.JSON = JsonConvert.SerializeObject(ClientResponseResult);
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var walkbookInfo = JsonConvert.DeserializeObject<WalkthroughsBookobject>(ClientResponseResult);

                if (walkbookInfo.status == 200)
                {
                    stateModel.state = "Success";
                    stateModel.SlotFillingState = walkbookInfo.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (walkbookInfo.status != 200)
                {
                    stateModel.state = "not_authorized";
                    stateModel.SlotFillingState = walkbookInfo.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }


                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }


        [HttpPost]
        [Route("api/Tamkeen/Complains")]
        [LogAction(ActionId = 4636, ClientId = 8708)]
        public async Task<HttpResponseMessage> Complains(ComplainsAction comRequest)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(comRequest);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                ComplainsRequest CRequest = new ComplainsRequest();
                logDetails.JSON = JsonConvert.SerializeObject(comRequest);

                if (!string.IsNullOrWhiteSpace(comRequest.reference))
                {
                    CRequest.reference = comRequest.reference;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.mobile))
                {
                    CRequest.mobile = comRequest.mobile;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.otp))
                {
                    CRequest.otp = comRequest.otp;
                }
                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                var BodyRequest = JsonConvert.SerializeObject(CRequest);
                logDetails.JSON = BodyRequest;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Complains/Get"),
                    Content = new StringContent(BodyRequest, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var ComplianList = JsonConvert.DeserializeObject<Complainsobject>(responseBody);
                if (ComplianList.status == 200)
                {
                    stateModel.state = "Success";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }
                if (ComplianList.status == 400)
                {

                    stateModel.state = "complains_not_found";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }
                if (ComplianList.status == 403)
                {
                    stateModel.state = "not_authorized ";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);


                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/Applications")]
        [LogAction(ActionId = 4637, ClientId = 8708)]
        public async Task<HttpResponseMessage> Applications(ComplainsAction comRequest)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.RequestToApi = JsonConvert.SerializeObject(comRequest);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

                ComplainsRequest CRequest = new ComplainsRequest();
                logDetails.JSON = JsonConvert.SerializeObject(comRequest);

                if (!string.IsNullOrWhiteSpace(comRequest.reference))
                {
                    CRequest.reference = comRequest.reference;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.mobile))
                {
                    CRequest.mobile = comRequest.mobile;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.otp))
                {
                    CRequest.otp = comRequest.otp;
                }
                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                var BodyRequest = JsonConvert.SerializeObject(CRequest);
                logDetails.JSON = BodyRequest;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURL"]}/dev/api/Applications/Get"),
                    Content = new StringContent(BodyRequest, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var ComplianList = JsonConvert.DeserializeObject<Complainsobject>(responseBody);
                if (ComplianList.status == 200)
                {
                    stateModel.state = "Success";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }
                if (ComplianList.status == 400 || ComplianList.status == 402)
                {
                    stateModel.state = "applications_not_found";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }
                if (ComplianList.status == 403)
                {
                    stateModel.state = "not_authorized ";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }


                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);


                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ApplicationsNew")]
        [LogAction(ActionId = 5212, ClientId = 8708)]
        public async Task<HttpResponseMessage> ApplicationsNew(ComplainsAction comRequest)
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.Forms = "";
            logDetails.Headers = "";
            logDetails.URL = $"{ConfigurationManager.AppSettings["APIURLV2"]}/Applications/Get";
            logDetails.RequestToApi = JsonConvert.SerializeObject(comRequest);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                ComplainsRequest CRequest = new ComplainsRequest();
                logDetails.JSON = JsonConvert.SerializeObject(comRequest);

                if (!string.IsNullOrWhiteSpace(comRequest.reference))
                {
                    CRequest.reference = comRequest.reference;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.mobile))
                {
                    CRequest.mobile = comRequest.mobile;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.otp))
                {
                    CRequest.otp = comRequest.otp;
                }
                if (!string.IsNullOrWhiteSpace(comRequest.lang))
                {
                    CRequest.lang = comRequest.lang;
                }
                else
                {
                    CRequest.lang = "en";
                }

                client.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                var BodyRequest = JsonConvert.SerializeObject(CRequest);
                logDetails.JSON = BodyRequest;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ConfigurationManager.AppSettings["APIURLV2"]}/Applications/Get"),
                    Content = new StringContent(BodyRequest, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;

                var responseBody = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responseBody);


                var ComplianList = JsonConvert.DeserializeObject<Complainsobject>(responseBody);
                if (ComplianList.status == 200)
                {
                    stateModel.state = "Success";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }
                if (ComplianList.status == 400 || ComplianList.status == 402)
                {
                    stateModel.state = "applications_not_found";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }
                if (ComplianList.status == 403)
                {
                    stateModel.state = "not_authorized ";
                    stateModel.SlotFillingState = ComplianList.app_status;
                }

                LogModel model = new LogModel();
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                model.LogTime = DateTime.Now;
                model.ActionName = "Applications";
                model.Parameter = JsonConvert.SerializeObject(comRequest);
                model.LogText = responseBody;
                LiteDBServices liteDB = new LiteDBServices();
                liteDB.InsertLogRow(model);
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                stateModel.SlotFillingState = JsonConvert.SerializeObject(ex);
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);

                LogModel model = new LogModel();
                model.LogTime = DateTime.Now;
                model.ActionName = "Applications";
                model.Parameter = JsonConvert.SerializeObject(comRequest);
                model.LogText = JsonConvert.SerializeObject(ex);
                model.ResponseFromLabiba = JsonConvert.SerializeObject(stateModel);
                LiteDBServices liteDB = new LiteDBServices();
                liteDB.InsertLogRow(model);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }


        [HttpPost]
        [Route("api/Tamkeen/ResetPassword")]
        [LogAction(ActionId = 4638, ClientId = 8708)]
        public async Task<HttpResponseMessage> ResetPassword(ResetPasswordAction passwordreq)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; 

            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                ResetPasswordRequest request = new ResetPasswordRequest();
                logDetails.RequestToApi = JsonConvert.SerializeObject(passwordreq);

                if (!string.IsNullOrWhiteSpace(passwordreq.id))
                {
                    request.id = passwordreq.id;
                }
                if (!string.IsNullOrWhiteSpace(passwordreq.otp))
                {
                    request.otp = passwordreq.otp;
                }

                var BodyRequest = JsonConvert.SerializeObject(request);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/User/ResetPassword", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var passwordinfo = JsonConvert.DeserializeObject<ResetPasswordobject>(ClientResponseResult);

                if (passwordinfo.status == 200)
                {
                    stateModel.state = "Success";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (passwordinfo.status != 200)
                {
                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }


                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/RequestOTP")]
        [LogAction(ActionId = 4639, ClientId = 8708)]
        public async Task<HttpResponseMessage> RequestOTP(ResetPasswordAction passwordreq)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                ResetPasswordRequest request = new ResetPasswordRequest();
                logDetails.RequestToApi = JsonConvert.SerializeObject(passwordreq);

                if (!string.IsNullOrWhiteSpace(passwordreq.mobile))
                {
                    request.mobile = passwordreq.mobile;
                }

                var BodyRequest = JsonConvert.SerializeObject(request);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/User/RequestOtp"
                    , new StringContent(BodyRequest, Encoding.UTF8, "application/json"));
                //HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURL"]}/dev/api/User/RequestOtp", new StringContent(BodyRequest));

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var passwordinfo = JsonConvert.DeserializeObject<ResetPasswordobject>(ClientResponseResult);

                if (passwordinfo.status == 200)
                {


                    stateModel.state = "Success";
                    stateModel.SlotFillingState = passwordinfo.msg + "<br>Code: " + passwordinfo.Code;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (passwordinfo.status == 400)
                {


                    stateModel.state = "user_not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }


                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {


                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }
        [HttpPost]
        [Route("api/Tamkeen/TimeValidation")]
        [LogAction(ActionId = 4855, ClientId = 8708)]
        public HttpResponseMessage TimeValidation(TimeValidationRequest TimeValidatioReq)
        {


            //  var json = string.Empty;
            // var jsonSerialiser = new JavaScriptSerializer();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";

            try
            {
                logDetails.RequestToApi = JsonConvert.SerializeObject(TimeValidatioReq);

                DateTime currtime = TimeZoneInfo.ConvertTime(DateTime.Now,
                TimeZoneInfo.FindSystemTimeZoneById(TimeValidatioReq.TimeZone));

                string currday = currtime.DayOfWeek.ToString().ToLower().Trim();

                DateTime startTime = DateTime.Parse(TimeValidatioReq.StartTime);
                DateTime endTime = DateTime.Parse(TimeValidatioReq.EndTime);

                if (currday.Equals("friday") || currday.Equals("saturday"))
                {
                    stateModel.state = "invalid";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                }
                else
                {
                    if (currtime >= startTime && currtime < endTime)
                    {
                        stateModel.state = "valid";
                        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                        ActionLogger.LogDetails(logDetails);
                    }
                    else
                    {
                        stateModel.state = "invalid";
                        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                        ActionLogger.LogDetails(logDetails);
                    }
                }


                //   json = jsonSerialiser.Serialize(stateResult);
                //return Content(json);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "failure";
                stateModel.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                //  json = jsonSerialiser.Serialize(stateResult);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }

        }

        //[HttpPost]
        //[Route("api/GIG/CountryCoverage")]
        //[LogAction(ActionId = 4528, ClientId = 8709)]
        //public async Task<HttpResponseMessage> CountryCoverage(CooverageRequest req)
        //{
        //    StateModel stateModel = new StateModel();
        //    HttpClient httpClient = new HttpClient();
        //    LogDetails logDetails = new LogDetails();
        //    TextModel textModel = new TextModel();
        //    logDetails.Method = "POST";
        //    logDetails.Forms = "";
        //    logDetails.Headers = "";

        //    try
        //    {
        //        logDetails.RequestToApi = JsonConvert.SerializeObject(req);
        //        if (string.IsNullOrEmpty(req.CountryID) || string.IsNullOrEmpty(req.language))
        //        {
        //            stateModel.state = "Failure";
        //            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
        //            ActionLogger.LogDetails(logDetails);
        //            return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
        //        }
        //        logDetails.JSON = JsonConvert.SerializeObject(req.CountryID);

        //        httpClient.DefaultRequestHeaders.Add("Token", "Pm9cNM4cFV1F/IttcprSlwzqywWimTiQqmEsZgSS91B7m2321dY9S6YXHS9HnR6D");
        //        Stopwatch timer = new Stopwatch();
        //        timer.Start();
        //        HttpResponseMessage response = await httpClient.GetAsync($"https://chatbot.gig.com.jo:93/Travel/GetCountriesCoverageByCode/{req.CountryID}");

        //        timer.Stop();

        //        logDetails.APIExecutionTime = timer.ElapsedMilliseconds;

        //        //LogModelT model = new LogModelT();
        //        //model.LogTime = DateTime.Now;
        //        //model.ActionName = "CountryCoverage";
        //        //model.Parameter = JsonConvert.SerializeObject(req);
        //        //LiteDBServices liteDB = new LiteDBServices();
        //        //liteDB.InsertToLog(model);

        //        var ClientResponseResult = await response.Content.ReadAsStringAsync();
        //        logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

        //        //LogModelT modelx = new LogModelT();
        //        //modelx.LogTime = DateTime.Now;
        //        //modelx.ActionName = "CountryCoverage";
        //        //modelx.Parameter = JsonConvert.SerializeObject(req);
        //        //modelx.Response = ClientResponseResult;
        //        //LiteDBServices liteDBB = new LiteDBServices();
        //        //liteDBB.InsertToLog(modelx);

        //        var countryCoverage = JsonConvert.DeserializeObject<Cooverageobject>(ClientResponseResult);
        //        if (countryCoverage.success == false)
        //        {
        //            stateModel.state = "Failure";
        //            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
        //            ActionLogger.LogDetails(logDetails);
        //            return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
        //        }
        //        if (req.language.ToLower().Trim() == "en")
        //        {
        //            textModel.text = "The available coverages for this country:<br><br>";
        //            var count = 0;
        //            foreach (var country in countryCoverage.countriesCoverageByCode)
        //            {
        //                count++;
        //                textModel.text = textModel.text + $"{count}. {country.directiontypE_EN} <br>";
        //            }
        //            textModel.text = textModel.text + "<br>You can learn more about each coverage program on the following link: https://bit.ly/3hKpoWo";
        //        }
        //        else
        //        {
        //            {
        //                textModel.text = "التغطيات المتاحة لهذا البلد:<br><br>";
        //                var count = 0;
        //                foreach (var country in countryCoverage.countriesCoverageByCode)
        //                {
        //                    count++;
        //                    textModel.text = textModel.text + $"{count}.  {country.directiontypE_AR} <br>";
        //                }
        //                textModel.text = textModel.text + "<br>" + "ممكن تعرف أكثر عن برامج التغطية عبر الرابط التالي: https://bit.ly/37fKVmg";
        //            }
        //        }
        //        textModel.SlotFillingText = JsonConvert.SerializeObject(countryCoverage);
        //        stateModel.state = "Success";
        //        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(textModel);
        //        ActionLogger.LogDetails(logDetails);
        //        return Request.CreateResponse(HttpStatusCode.OK, textModel, Configuration.Formatters.JsonFormatter);

        //    }
        //    catch (Exception ex)
        //    {
        //        ActionLogger.LogException(ex, logDetails);
        //        stateModel.state = "Failure";
        //        //LogModelT modelx = new LogModelT();
        //        //modelx.LogTime = DateTime.Now;
        //        //modelx.ActionName = "CountryCoverage";
        //        //modelx.Parameter = JsonConvert.SerializeObject(req);
        //        //modelx.Response = JsonConvert.SerializeObject(ex);
        //        //LiteDBServices liteDBB = new LiteDBServices();
        //        //liteDBB.InsertToLog(modelx);
        //        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
        //        ActionLogger.LogDetails(logDetails);
        //        return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
        //    }
        //}
        public string startAtDate(string start_at)
        {
            char[] startAtDateArr = start_at.Take(10).ToArray();
            string startAtDate = new string(startAtDateArr);
            return startAtDate;
        }


        [HttpPost]
        [Route("api/Tamkeen/VMENUCards")]
        [LogAction(ActionId = 5726, ClientId = 8708)]
        public HttpResponseMessage VMENUCards(MenuCard menuCard)
        {
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            List<hero_cards> cards = new List<hero_cards>();
            RootObject cardsRootObject = new RootObject();
            logDetails.Method = "POST";
            logDetails.Forms = "";
            logDetails.Headers = "";
            logDetails.JSON = JsonConvert.SerializeObject("");
            logDetails.RequestToApi = JsonConvert.SerializeObject(menuCard);
            var timer = new Stopwatch();
            timer.Start();
            try
            {
                String[] TitleArray = menuCard.title.Split(',');
                String[] IconsArray = menuCard.icons.Split(',');

                if (string.IsNullOrEmpty(menuCard.Language))
                {
                    menuCard.Language = "en";
                }

                if (TitleArray.Length > 0 && IconsArray.Length > 0)
                {
                    var itemsList = TitleArray.ToList();
                    var itemsListForIcons = IconsArray.ToList();
                    int numResult = itemsList.Count();
                    double num = (double)numResult / 6;
                    var numPages = Math.Ceiling(num);
                    if (!string.IsNullOrWhiteSpace(menuCard.PageNumber))
                    {
                        menuCard.PageNumber = menuCard.PageNumber.Replace("nextPage_", "");
                        menuCard.PageNumber = menuCard.PageNumber.Replace("_Last", "");
                    }
                    if (string.IsNullOrWhiteSpace(menuCard.PageNumber) || !System.Text.RegularExpressions.Regex.IsMatch(menuCard.PageNumber, @"^\d+$"))
                    {
                        menuCard.PageNumber = Convert.ToString(1);
                    }
                    var ResultPerPage = itemsList.Skip(((int.Parse(menuCard.PageNumber) - 1) * 6)).Take(6).ToList();
                    var ResultPerPageForIcons = itemsListForIcons.Skip(((int.Parse(menuCard.PageNumber) - 1)) * 6).Take(6).ToList();
                    if (menuCard.Language.Trim().ToLower() == "ar")
                    {
                        int count = 0;
                        foreach (var item in ResultPerPage)
                        {
                            cards.Add(new hero_cards()
                            {
                                Title = $"VMENU:{item.Trim()}",
                                Images = new List<Image> { new Image() { URL = ResultPerPageForIcons[count].Trim() } },
                                Subtitle = $"",
                                Buttons = new List<Button> { new Button() { Title = "اختر", Type = "PostBack", Value = $"{item.Trim()}" } }
                            });
                            count++;
                        }
                        count = 0;
                        if (int.Parse(menuCard.PageNumber) < numPages)
                        {
                            int nextPage = int.Parse(menuCard.PageNumber) + 1;
                            if (nextPage == numPages)
                            {
                                cards.Add(new hero_cards()
                                {
                                    Title = "المزيد",
                                    Buttons = new List<Button>() { new Button() { Title = "المزيد", Type = "PostBack", Value = $"nextPage_{nextPage}_Last" } }
                                });
                            }
                            else
                            {
                                cards.Add(new hero_cards()
                                {
                                    Title = "المزيد",
                                    Buttons = new List<Button>() { new Button() { Title = "المزيد", Type = "PostBack", Value = $"nextPage_{nextPage}" } }
                                });
                            }
                        }
                    }
                    else
                    {
                        int count = 0;
                        foreach (var item in ResultPerPage)//HERE
                        {
                            cards.Add(new hero_cards()
                            {
                                Title = $"VMENU:{item.Trim()}",
                                Images = new List<Image> { new Image() { URL = ResultPerPageForIcons[count].Trim() } },
                                Subtitle = $"",
                                Buttons = new List<Button> { new Button() { Title = "select", Type = "PostBack", Value = $"{item.Trim()}" } }
                            });
                            count++;
                        }
                        count = 0;
                        if (int.Parse(menuCard.PageNumber) < numPages)
                        {
                            int nextPage = int.Parse(menuCard.PageNumber) + 1;
                            if (nextPage == numPages)
                            {
                                cards.Add(new hero_cards()
                                {
                                    Title = "More",
                                    Buttons = new List<Button>() { new Button() { Title = "More", Type = "PostBack", Value = $"nextPage_{nextPage}_Last" } }
                                });
                            }
                            else
                            {
                                cards.Add(new hero_cards()
                                {
                                    Title = "More",
                                    Buttons = new List<Button>() { new Button() { Title = "More", Type = "PostBack", Value = $"nextPage_{nextPage}" } }
                                });
                            }
                        }
                    }
                    cardsRootObject.hero_cards = cards;
                    cardsRootObject.response = "Success";
                    cardsRootObject.success_message = "Here are some results that I found";
                    cardsRootObject.failure_message = "Oops. I couldn't find anything";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(cardsRootObject);
                    timer.Stop();
                    logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, cardsRootObject, Configuration.Formatters.JsonFormatter);
                }
                stateModel.state = "Not_Found";
                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/VerifyAccount")]
        [LogAction(ActionId = 8319, ClientId = 8708)]
        public async Task<HttpResponseMessage> VerifyAccount(VerifyAccountAction verificationreq)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();

            logDetails.Method = "POST";
            logDetails.Forms = "";
            logDetails.Headers = "";
            logDetails.JSON = JsonConvert.SerializeObject("");
            logDetails.RequestToApi = JsonConvert.SerializeObject(verificationreq);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                VerifyAccountRequest request = new VerifyAccountRequest();

                if (!string.IsNullOrWhiteSpace(verificationreq.CPR))
                {
                    request.CPR = verificationreq.CPR;
                }

                var BodyRequest = JsonConvert.SerializeObject(request);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURL"]}/test/api/Portal/Verify", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));
                logDetails.RequestToApi = JsonConvert.SerializeObject(verificationreq);
                var ClientResponseResult = await response.Content.ReadAsStringAsync();

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(ClientResponseResult);

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;




                LogModel modelx = new LogModel();
                modelx.LogTime = DateTime.Now;
                modelx.ActionName = "VerifyAccount";
                modelx.Parameter = JsonConvert.SerializeObject(verificationreq);
                modelx.ResponseFromLabiba = JsonConvert.SerializeObject(ClientResponseResult);
                LiteDBServices liteDBB = new LiteDBServices();
                liteDBB.InsertLogRow(modelx);

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                //stateModel.SlotFillingState = JsonConvert.SerializeObject(ClientResponseResult); 

                var verificationinfo = JsonConvert.DeserializeObject<VerifyAccountobject>(ClientResponseResult);


                if (verificationinfo.msg.Contains("No Data Found"))
                {
                    stateModel.state = "Success";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                if (verificationinfo.msg.Contains("Account is exist in the system with the email id or CPR"))
                {
                    stateModel.state = "Account_Exists";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }


                //logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                //ActionLogger.LogDetails(logDetails);
                //return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogException(ex, logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);

                //ActionLogger.LogException(ex, logDetails);
                //stateModel.state = "Failure";
                //logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                //ActionLogger.LogDetails(logDetails);
                //return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ResgisterUser")]
        [LogAction(ActionId = 8320, ClientId = 8708)]
        public async Task<HttpResponseMessage> ResgisterUser(ResgisterUserAction resgisteruserreq)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";

            logDetails.Forms = "";
            logDetails.Headers = "";
            logDetails.JSON = JsonConvert.SerializeObject("");
            logDetails.RequestToApi = JsonConvert.SerializeObject(resgisteruserreq);


            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                ResgisterUserRequest request = new ResgisterUserRequest();

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.CPR))
                {
                    request.CPR = resgisteruserreq.CPR;
                }
                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Area))
                {
                    request.Area = resgisteruserreq.Area;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Block))
                {
                    request.Block = resgisteruserreq.Block;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Building))
                {
                    request.Building = resgisteruserreq.Building;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.DateOfBirth))
                {
                    request.DateOfBirth = resgisteruserreq.DateOfBirth;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Email))
                {
                    request.Email = resgisteruserreq.Email;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.FirstName))
                {
                    request.FirstName = resgisteruserreq.FirstName;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Flat))
                {
                    request.Flat = resgisteruserreq.Flat;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Gender))
                {
                    request.Gender = resgisteruserreq.Gender;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.IDType))
                {
                    request.IDType = resgisteruserreq.IDType;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.LastName))
                {
                    request.LastName = resgisteruserreq.LastName;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Mobile))
                {
                    request.Mobile = resgisteruserreq.Mobile;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Nationality))
                {
                    request.Nationality = resgisteruserreq.Nationality;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Password))
                {
                    request.Password = resgisteruserreq.Password;

                }

                if (!string.IsNullOrWhiteSpace(resgisteruserreq.Road))
                {
                    request.Road = resgisteruserreq.Road;

                }

                var BodyRequest = JsonConvert.SerializeObject(request);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURL"]}/test/api/Portal/Register", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));

                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                //var ClientResponseResult = "{\"data\": null,\"status\": 400,\"msg\": \"Account is exist in the system with the email id or CPR! Status as below\r\nAccount Verified : True\r\nEmail Verified: True\",\"Code\": 0}";
                //stateModel.SlotFillingState = JsonConvert.SerializeObject(ClientResponseResult);

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(ClientResponseResult);
                LogModel modelx = new LogModel();
                modelx.LogTime = DateTime.Now;
                modelx.ActionName = "ResgisterUser";
                modelx.Parameter = JsonConvert.SerializeObject(resgisteruserreq);

                modelx.ResponseFromLabiba = JsonConvert.SerializeObject(ClientResponseResult);
                LiteDBServices liteDBB = new LiteDBServices();
                liteDBB.InsertLogRow(modelx);

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);


                var resgisteruserinfo = JsonConvert.DeserializeObject<ResgisterUserobject>(ClientResponseResult);
                // stateModel.SlotFillingState = ClientResponseResult;

                if (resgisteruserinfo.msg.Contains("Account is exist in the system"))
                {
                    stateModel.state = "AccountExists";

                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

                if (resgisteruserinfo.msg.Contains("Account is created successfully"))
                {
                    stateModel.state = "Success";


                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

                if (!resgisteruserinfo.msg.Contains("Object reference not set to an instance of an object"))
                {

                    stateModel.state = "Not_Found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }


                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {


                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/UploadInformation")]
        [LogAction(ActionId = 9350, ClientId = 8708)]
        public async Task<HttpResponseMessage> UploadInformation(AddDataRequest Data)
        {
            StateModel stateResult = new StateModel();
            LogDetails logDetails = new LogDetails();
            AddData Data2 = new AddData();

            logDetails.URL = "api/Tamkeen/UploadInformation";
            logDetails.Method = "POST";
            logDetails.Forms = " ";
            logDetails.Headers = " ";
            logDetails.JSON = JsonConvert.SerializeObject(Data);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                var oblist = new List<object>();
                Google.Apis.Sheets.v4.Data.ValueRange requestBody = new Google.Apis.Sheets.v4.Data.ValueRange();
                var service = initializeService();
                //IList<IList<Object>> values = await GetSheet1();
                //var newvalues = values.ToList();
                //string[] CRarray = null;
                //if (!string.IsNullOrWhiteSpace(Data.Information))
                //{
                //    CRarray = Data.Information.Split('!');
                //}
                //else
                if (string.IsNullOrEmpty(Data.PaymentNumber) || string.IsNullOrEmpty(Data.Name) || string.IsNullOrEmpty(Data.ItemNumber))
                {

                    stateResult.state = "Failure";
                    stopwatch.Stop();
                    logDetails.APIExecutionTime = stopwatch.ElapsedMilliseconds;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                var itemNumber = "";
                var SVItem = "";
                var SVNo = "";

                //SVNo = CRarray[0];

                //if (CRarray.Count() > 1)
                //{
                //    SVItem = CRarray[1];
                //}

                //if (CRarray.Count() > 2)
                //{
                //    itemNumber = CRarray[2];
                //}
                SVNo = Data.PaymentNumber;
                // SVItem = CRarray[0].Replace("ItemName:", "").Trim();
                SVItem = Data.ItemNumber;
                itemNumber = Data.Name;

                // itemNumber = CRarray[1].Replace("ItemNumber:", "").Trim();

                oblist = new List<object>() {
                    string.IsNullOrWhiteSpace(SVNo) ? "-" : SVNo,
                    string.IsNullOrWhiteSpace(SVItem) ? "-" : SVItem,
                    string.IsNullOrWhiteSpace(itemNumber) ? "-" : itemNumber,


                        };

                requestBody.Values = new List<IList<object>> { oblist };

                SpreadsheetsResource.ValuesResource.AppendRequest add =
                    service.Spreadsheets.Values.Append(requestBody, spreadsheetId, sheet1);
                // How the input data should be interpreted.
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption
                    = (SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum)1;  // TODO: Update placeholder value.

                // How the input data should be inserted.
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption
                    = (SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum)0;  // TODO: Update placeholder value.

                add.ValueInputOption = valueInputOption;
                add.InsertDataOption = insertDataOption;
                var addResponse = await add.ExecuteAsync();

                stateResult.state = "Success";
                stopwatch.Stop();
                logDetails.APIExecutionTime = stopwatch.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateResult.state = "Failure";
                LogModel model = new LogModel();
                model.Parameter = JsonConvert.SerializeObject(Data);
                model.LogTime = DateTime.Now;
                model.ResponseFromLabiba = JsonConvert.SerializeObject(stateResult);
                //model.RequestToAPI = requestBody;
                model.ResponseFromLabiba = JsonConvert.SerializeObject(ex);
                model.ActionName = "UploadInformation";
                ActionLogger.LogException(ex, logDetails);

                LiteDBServices liteDB = new LiteDBServices();
                liteDB.InsertLogRow(model);
                stopwatch.Stop();
                logDetails.APIExecutionTime = stopwatch.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }



        [HttpPost]
        [Route("api/Tamkeen/UploadInformationtwo")]
        [LogAction(ActionId = 9350, ClientId = 8708)]
        public async Task<HttpResponseMessage> UploadInformationtwo(AddDataRequest Data)
        {
            StateModel stateResult = new StateModel();
            LogDetails logDetails = new LogDetails();
            AddData Data2 = new AddData();

            logDetails.URL = "api/Tamkeen/UploadInformation";
            logDetails.Method = "POST";
            logDetails.Forms = " ";
            logDetails.Headers = " ";
            logDetails.JSON = JsonConvert.SerializeObject(Data);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                var oblist = new List<object>();
                Google.Apis.Sheets.v4.Data.ValueRange requestBody = new Google.Apis.Sheets.v4.Data.ValueRange();
                var service = initializeService();
                //IList<IList<Object>> values = await GetSheet1();
                //var newvalues = values.ToList();
                //string[] CRarray = null;
                //if (!string.IsNullOrWhiteSpace(Data.Information))
                //{
                //    CRarray = Data.Information.Split('!');
                //}
                //else
                if (string.IsNullOrEmpty(Data.PaymentNumber) || string.IsNullOrEmpty(Data.Name) || string.IsNullOrEmpty(Data.ItemNumber))
                {

                    stateResult.state = "Failure";
                    stopwatch.Stop();
                    logDetails.APIExecutionTime = stopwatch.ElapsedMilliseconds;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                var itemNumber = "";
                var SVItem = "";
                var SVNo = "";

                //SVNo = CRarray[0];

                //if (CRarray.Count() > 1)
                //{
                //    SVItem = CRarray[1];
                //}

                //if (CRarray.Count() > 2)
                //{
                //    itemNumber = CRarray[2];
                //}
                SVNo = Data.PaymentNumber;
                // SVItem = CRarray[0].Replace("ItemName:", "").Trim();
                SVItem = Data.ItemNumber;
                itemNumber = Data.Name;

                // itemNumber = CRarray[1].Replace("ItemNumber:", "").Trim();

                oblist = new List<object>() {
                    string.IsNullOrWhiteSpace(SVNo) ? "-" : SVNo,
                    string.IsNullOrWhiteSpace(SVItem) ? "-" : SVItem,
                    string.IsNullOrWhiteSpace(itemNumber) ? "-" : itemNumber,


                        };

                requestBody.Values = new List<IList<object>> { oblist };

                SpreadsheetsResource.ValuesResource.AppendRequest add =
                    service.Spreadsheets.Values.Append(requestBody, spreadsheetId2, sheet2);
                // How the input data should be interpreted.
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption
                    = (SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum)1;  // TODO: Update placeholder value.

                // How the input data should be inserted.
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption
                    = (SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum)0;  // TODO: Update placeholder value.

                add.ValueInputOption = valueInputOption;
                add.InsertDataOption = insertDataOption;
                var addResponse = await add.ExecuteAsync();

                stateResult.state = "Success";
                stopwatch.Stop();
                logDetails.APIExecutionTime = stopwatch.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateResult.state = "Failure";
                LogModel model = new LogModel();
                model.Parameter = JsonConvert.SerializeObject(Data);
                model.LogTime = DateTime.Now;
                model.ResponseFromLabiba = JsonConvert.SerializeObject(stateResult);
                //model.RequestToAPI = requestBody;
                model.ResponseFromLabiba = JsonConvert.SerializeObject(ex);
                model.ActionName = "UploadInformation";
                ActionLogger.LogException(ex, logDetails);

                LiteDBServices liteDB = new LiteDBServices();
                liteDB.InsertLogRow(model);
                stopwatch.Stop();
                logDetails.APIExecutionTime = stopwatch.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/UploadAttachment")]
        [LogAction(ActionId = 15214, ClientId = 8708)]
        public async Task<HttpResponseMessage> UploadAttachment(UploadAttachmentsRequest req)
        {
            HttpClient httpClient = new HttpClient();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "post";
            logDetails.JSON = JsonConvert.SerializeObject(req);

            try
            {
                StateModel stateResult = new StateModel();
                /******************************************************************/


                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");

                //if (string.IsNullOrWhiteSpace(req.PayNO))
                //{
                //    stateResult.state = "Failure";
                //    stateResult.SlotFillingState = "SVNo is required";
                //    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                //    ActionLogger.LogDetails(logDetails);
                //    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                //}

                //var ImageURL = "";
                //if (req.Type.Trim().ToLower() != "location")
                //{
                //    if (!string.IsNullOrWhiteSpace(req.Image))
                //    {
                //        ImageURL = Convert.ToBase64String(ConvertUrlToBytes(req.Image));

                //    }
                //else
                //{
                //    stateResult.state = "Failure";
                //    stateResult.SlotFillingState = "No Image found";
                //    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                //    ActionLogger.LogDetails(logDetails);
                //    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                //}
                //}


                var ImageURL = "";
                if (!string.IsNullOrWhiteSpace(req.Image))
                {
                    ImageURL = Convert.ToBase64String(ConvertUrlToBytes(req.Image));

                }
                req.Image = ImageURL;


                if (!string.IsNullOrEmpty(req.Location))
                {
                    if (!req.Location.Contains(","))
                    {
                        stateResult.state = "Failure";
                        stateResult.SlotFillingState = "Invalid Location";
                        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                        ActionLogger.LogDetails(logDetails);
                        return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                    }
                }


                UploadAttachmentsReq uploadAttachmentsReq = new UploadAttachmentsReq();

                uploadAttachmentsReq.Imgdata = req.Image;
                uploadAttachmentsReq.Location = req.Location;
                uploadAttachmentsReq.PAYNo = req.PayNO;
                uploadAttachmentsReq.Serial = req.Serial;
                uploadAttachmentsReq.SVItem = req.SVItem;
                uploadAttachmentsReq.Type = req.Type;
                uploadAttachmentsReq.SVCofirmedByCustomer = req.CustomerConfirmation;
                uploadAttachmentsReq.SVCustremarks = req.CustomerComment;


                var requestBody = JsonConvert.SerializeObject(uploadAttachmentsReq);

                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/UploadSVDocument/UploadAtt", new StringContent(requestBody, Encoding.UTF8, "application/json"));
                //var ClientResponseResult2 = "{\"data\": {\"SVNo\": null,\"PAYNo\": \"ES/22693-2/00703/PAYREQ-04\",\"Mobile\": null,\"CPR\": null,\"FullName\": null,\"SVItem\": \"ICT-ITEM-01415\",\"Serial\": \"SSA002SSSASD\",\"SVCofirmedByCustomer\": true,\"SVCustremarks\": \"Customer is saying all Items are devlivered by vendor on time and in excellent condition\",\"Location\": \"65.225,25.556\",\"Imgdata\": \"https://storage.googleapis.com/tamkeen_svm/photo1_ICT-ITEM-01415/photo1_ICT-ITEM-01415_512022091405.jpg\",\"Type\": \"cpr\"},\"status\": 200,\"msg\": \"\",\"Code\": 0}";

                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;


                var ClientResponseResult = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<UploadAttachment>(ClientResponseResult);
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                LogModel modelx = new LogModel();
                modelx.LogTime = DateTime.Now;
                modelx.ActionName = "UploadAttachments";
                modelx.Parameter = JsonConvert.SerializeObject(req);

                modelx.ResponseFromLabiba = JsonConvert.SerializeObject(ClientResponseResult);
                LiteDBServices liteDBB = new LiteDBServices();
                liteDBB.InsertLogRow(modelx);
                //stateResult.SlotFillingState = data.status;



                if (data.status == 200)
                {

                    stateResult.state = "Success";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);



                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                else if (data.status == 400)
                {

                    stateResult.state = "not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                else if (!string.IsNullOrWhiteSpace(req.Location) && data.status == 500)
                {
                    stateResult.state = "Success";
                    stateResult.SlotFillingState = data.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    stateResult.state = "Failure";
                    stateResult.SlotFillingState = data.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }


                //logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                //ActionLogger.LogDetails(logDetails);
                //return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                StateModel stateResult = new StateModel();

                ActionLogger.LogException(ex, logDetails);
                stateResult.state = "Failure";
                stateResult.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ListItems")]
        [LogAction(ActionId = 15215, ClientId = 8708)]
        public async Task<HttpResponseMessage> ListItems(ListItemsRequest req)
        {
            HttpClient httpClient = new HttpClient();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "post";
            logDetails.JSON = JsonConvert.SerializeObject(req);
            try
            {
                StateModel stateResult = new StateModel();
                //TextModel textResult = new TextModel();
                /******************************************************************/
                List<List<object>> ValuesbySVNo = new List<List<object>>();
                IList<IList<Object>> values = await GetSheet1();
                if (values != null && values.Count() > 0)
                {
                    foreach (var row in values)
                    {
                        if (row[0].ToString() == req.PAYNO)
                        {
                            ValuesbySVNo.Add((List<Object>)row);
                        }
                    }
                }

                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");


                ListItems Newitem = new ListItems();

                Newitem.PAYNo = req.PAYNO;

               var requestBody = JsonConvert.SerializeObject(Newitem);
             

                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/UploadSVDocument/ListItems"
                    , new StringContent(requestBody, Encoding.UTF8, "application/json"));


                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;

                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                //var ClientResponseResult2 = "{\"data\": {\"SVNo\": \"SV-002099\",\"Mobile\": null,\"CPR\": null,\"FullName\": null,\"SVItem\": \"ICT-ITEM-01415\",\"Serial\": \"SSA002SSSASD\",\"Location\": null,\"Imgdata\": \"https://storage.googleapis.com/tamkeen_svm//_3062021110510.jpg\",\"Type\": \"cpr\"},\"status\": 200,\"msg\": \"\",\"Code\": 0}";
                //var ClientResponseResult = "{\"data\":[{\"ItemNumber\":\"ICT-ITEM-01415\",\"ItemName\":\"FEC MEGAPOS LITE INTEGRATED POS SYSTEM INTEATOM CORE D525, 2GB MEMORY\",\"VendorName\":\"SEEDS SOFT SOLUTIONS\",\"Scheme\":\"ICT\",\"TotalCost\":390.000,\"Quantity\":1},{\"ItemNumber\":\"ICT-ITEM-01416\",\"ItemName\":\"CASH DRAWER\",\"VendorName\":\"SEEDS SOFT SOLUTIONS\",\"Scheme\":\"ICT\",\"TotalCost\":40.000,\"Quantity\":1},{\"ItemNumber\":\"ICT-ITEM-01417\",\"ItemName\":\"EPSON T-20 THERMAL PRINTER\",\"VendorName\":\"SEEDS SOFT SOLUTIONS\",\"Scheme\":\"ICT\",\"TotalCost\":95.000,\"Quantity\":1},{\"ItemNumber\":\"ICT-ITEM-01418\",\"ItemName\":\"INVO POS\",\"VendorName\":\"SEEDS SOFT SOLUTIONS\",\"Scheme\":\"ICT\",\"TotalCost\":200.000,\"Quantity\":1},{\"ItemNumber\":\"ICT-ITEM-01419\",\"ItemName\":\"EPSON TM-U220 WITH NIC\",\"VendorName\":\"SEEDS SOFT SOLUTIONS\",\"Scheme\":\"ICT\",\"TotalCost\":270.000,\"Quantity\":2}],\"status\":200,\"msg\":\"Item details are successfully retrieved!\",\"Code\":0}";

                timer.Stop();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var Result = JsonConvert.DeserializeObject<ListItemsResponse>(ClientResponseResult);

                if (Result.status == 200)
                {
                    if (Result.data.Count() == 0)
                    {
                        stateResult.state = "Not_Found";
                        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                        ActionLogger.LogDetails(logDetails);
                        return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                    }
                    var AllData = Result.data.OrderBy(x => x.ItemNumber).ToList();
                    var filterd = AllData;//from thier*********2 json
                    //var newvalues = values.ToList();
                    if (ValuesbySVNo != null)
                    {
                        if (ValuesbySVNo.Count() != 0)
                        {
                            foreach (var row in ValuesbySVNo)
                            {
                                var i = row[1].ToString();
                                //filterd = filterd.Where(x => x.ItemNumber != i).OrderBy(x => x.ItemNumber).ToList();
                                //filterd = filterd.Where(x => x.ItemNumber != i).ToList();
                                filterd = filterd.Where(x => x.ItemNumber != i).ToList();

                            }
                        }
                    }


                    var dataList = filterd.ToList();
                    // int counter = 0;
                    //int filterdcount = 0;

                    //string Item1 = "";

                    ListItemsStateResponse list = new ListItemsStateResponse();
                    List<ListItems1> listItemsstr = new List<ListItems1>();

                    // dataList = dataList.Skip((page - 1) * pageSize2).Take(pageSize2).ToList();
                    if (filterd.Count > 0)
                    {
                        stateResult.state = "Success";

                        stateResult.SlotFillingState = dataList[0].ItemName;

                        #region Old Code
                        //foreach (var item in AllData)
                        //{
                        //    if (filterd.Count > filterdcount)
                        //    {
                        //        if (filterd[filterdcount].ItemNumber == item.ItemNumber)
                        //        {
                        //            filterdcount++;
                        //            counter++;
                        //            if (req.lang.Trim().ToLower() == "ar")
                        //            {
                        //                Item1 = counter + ". ";
                        //                Item1 += "رقم المنتج: " + item.ItemNumber + $"<br>";
                        //                Item1 += "اسم المنتج: " + item.ItemName + $"<br>";
                        //                Item1 += "الكمية: " + item.Quantity + $"<br>";
                        //                Item1 += "المخطط:  " + item.Scheme + $"<br>";
                        //                Item1 += " اجمالي السعر :  " + item.TotalCost + $"<br>";
                        //                Item1 += "اسم البائع: " + item.VendorName + $"<br>";

                        //                textResult.text += Item1 + $"<br>";

                        //                stateResult.SlotFillingState += ":اسم المنتج " + item.ItemName + "," + "رقم المنتج: " + item.ItemNumber + $"<br>";
                        //                textResult.SlotFillingText += "{\"ItemNumber\":\"" + item.ItemNumber + "\",\"ItemName\":\"" + item.ItemName + "\"},";


                        //                listItemsstr.Add(new ListItems1() { ItemOrder = counter, ItemName = item.ItemName, ItemNumber = item.ItemNumber });


                        //            }
                        //            else
                        //            {
                        //                Item1 = counter + ". ";
                        //                Item1 += "ItemNumber: " + item.ItemNumber + $"<br>";
                        //                Item1 += "ItemName: " + item.ItemName + $"<br>";
                        //                Item1 += "Quantity: " + item.Quantity + $"<br>";
                        //                Item1 += "Scheme: " + item.Scheme + $"<br>";
                        //                Item1 += "TotalCost: " + item.TotalCost + $"<br>";
                        //                Item1 += "VendorName: " + item.VendorName + $"<br>";

                        //                textResult.text += Item1 + $"<br>";


                        //                stateResult.SlotFillingState += "ItemName: " + item.ItemName + "," + "ItemNumber: " + item.ItemNumber + $"<br>";
                        //                textResult.SlotFillingText += "{ItemNumber:" + item.ItemNumber + ",ItemName:" + item.ItemName + "},";
                        //                listItemsstr.Add(new ListItems1() { ItemOrder = counter, ItemName = item.ItemName, ItemNumber = item.ItemNumber });


                        //            }

                        //        }
                        //        else
                        //        {
                        //            counter++;
                        //        }

                        //    }
                        //    else {
                        //        break;
                        //    }



                        //}
                        //list.ListItems1 = listItemsstr.ToArray();

                        ////textResult.SlotFillingText = "{\"ListItems1\":[" + textResult.SlotFillingText + "]}";
                        //textResult.SlotFillingText = JsonConvert.SerializeObject(list); 
                        //logDetails.ResponseToLabiba = JsonConvert.SerializeObject(textResult);
                        //    ActionLogger.LogDetails(logDetails);
                        //    return Request.CreateResponse(HttpStatusCode.OK, textResult, Configuration.Formatters.JsonFormatter);
                        #endregion

                        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                        ActionLogger.LogDetails(logDetails);
                        return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);



                    }
                    else
                    {
                        stateResult.state = "MissingItems";
                        logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                        ActionLogger.LogDetails(logDetails);
                        return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);

                    }
                }
                else
                {
                    stateResult.state = "Failure";
                    stateResult.SlotFillingState = $"Message: {Result.msg} , State: {Result.status}";
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }


            }
            catch (Exception ex)
            {
                StateModel stateResult = new StateModel();


                stateResult.state = "Failure";
                stateResult.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogException(ex, logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ListItemsState")]
        [LogAction(ActionId = 9367, ClientId = 8708)]
        public HttpResponseMessage ListItemsState(ListItemsState ListReq)
        {
            StateModel stateModel = new StateModel();
            HttpClient httpClient = new HttpClient();
            LogDetails logDetails = new LogDetails();
            TextModel textModel = new TextModel();
            logDetails.Method = "POST";
            logDetails.Forms = "";
            logDetails.Headers = "";
            logDetails.RequestToApi = JsonConvert.SerializeObject(ListReq);
            try
            {


                if (string.IsNullOrEmpty(ListReq.UserChoice) || string.IsNullOrEmpty(ListReq.ItemList))
                {
                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                string numlist;
                ListItemsStateResponse ListItemsState = new ListItemsStateResponse();
                ListItems1 ListItems1 = new ListItems1();
                if (ListReq.ItemList.Contains("/"))
                {
                    var splitList = ListReq.ItemList.Split('/');
                    numlist = splitList[1];
                    ListItemsState = JsonConvert.DeserializeObject<ListItemsStateResponse>(numlist);

                }
                else
                {
                    ListItemsState = JsonConvert.DeserializeObject<ListItemsStateResponse>(ListReq.ItemList);
                }


                Stopwatch timer = new Stopwatch();
                timer.Start();
                var datalist = ListItemsState.ListItems1.ToList();

                int n;
                bool isNumeric = int.TryParse(ListReq.UserChoice, out n);
                if (isNumeric)
                {
                    int index = Int32.Parse(ListReq.UserChoice);
                    foreach (var data in datalist)
                    {
                        if (data.ItemOrder == index)
                        {
                            stateModel.state = "Success";
                            stateModel.SlotFillingState = "ItemName: " + data.ItemName + "!ItemNumber: " + data.ItemNumber;
                            timer.Stop();
                            logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                            ActionLogger.LogDetails(logDetails);
                            return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                        }

                    }

                }
                else
                {
                    foreach (var data in datalist)
                    {
                        if (datalist.Count().ToString() == ListReq.UserChoice.ToLower().Trim())
                        {
                            stateModel.state = "Success";
                            stateModel.SlotFillingState = "ItemName: " + data.ItemName + "!ItemNumber: " + data.ItemNumber;
                            timer.Stop();
                            logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                            ActionLogger.LogDetails(logDetails);
                            return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                        }
                    }

                }


                stateModel.state = "Not Found";



                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);



            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ReadBranchesFromExcelFile")]
        [LogAction(ActionId = 0, ClientId = 8708)]
        public async Task<HttpResponseMessage> ReadBranchesFromExcelFile()
        {
            HttpClient httpClient = new HttpClient();
            LogDetails logDetails = new LogDetails();
            StateModel stateResult = new StateModel();
            try
            {


                Google.Apis.Sheets.v4.Data.ValueRange requestBody = new Google.Apis.Sheets.v4.Data.ValueRange();
                var service = initializeService();
                IList<IList<Object>> values = await GetSheet1();



                //Creating and opening a data connection to the Excel sheet 

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {

                ActionLogger.LogException(ex, logDetails);
                stateResult.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);

            }
        }

        [HttpPost]
        [Route("api/Tamkeen/ConfirmPassword")]
        [LogAction(ActionId = 11487, ClientId = 8708)]
        public HttpResponseMessage ConfirmPassword(passwordsConfirm passwords)
        {
            LogDetails logDetails = new LogDetails();
            StateModel stateResult = new StateModel();
            logDetails.Method = "POST";
            logDetails.Headers = "";
            logDetails.URL = "";
            logDetails.Forms = "";
            logDetails.RequestToApi = "";
            logDetails.JSON = JsonConvert.SerializeObject(passwords);

            try
            {

                Stopwatch timer = new Stopwatch();
                timer.Start();

                if (passwords.FirstPassword == passwords.SecondPassword)
                {
                    stateResult.state = "success";
                }
                else
                {
                    stateResult.state = "PasswordError";
                }

                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(stateResult.state);
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                stateResult.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogException(ex, logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/Tamkeen/GetAgentName")]
        [LogAction(ActionId = 12651, ClientId = 8708)]
        public async Task<HttpResponseMessage> GetAgentName(AgentName req)
        {
            //8708
            StateModel StateModel = new StateModel();
            #region ADD LOG DETAILS NEW  
            LogDetails logDetails = new LogDetails();
            logDetails.URL = "api/Tankeen/GetAgentName";
            logDetails.JSON = JsonConvert.SerializeObject(req);
            logDetails.Method = "POST";
            Stopwatch stopwatch = new Stopwatch();
            #endregion
            HttpClient httpClient = new HttpClient();
            try
            {
                stopwatch.Start();

                var GetURL = ConfigurationManager.AppSettings["BotbuilderBaseURL"] + $"/api/LiveChat/v1.0/GetAgentInfo/{req.RecipientID}/{req.senderID}";

                logDetails.RequestToApi = GetURL;

                HttpResponseMessage response = await httpClient.GetAsync(GetURL);
                var ClientResponseResult = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = ClientResponseResult;

                var ResResult = JsonConvert.DeserializeObject<getAgentResponse>(ClientResponseResult);


                if (ResResult.Success == true)
                {
                    StateModel.state = "Success";
                    StateModel.SlotFillingState = ResResult.Name;
                }
                else
                {
                    StateModel.state = "Failure";

                }


            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                StateModel.state = "Failure";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(StateModel);


                return Request.CreateResponse(HttpStatusCode.OK, StateModel, Configuration.Formatters.JsonFormatter);

            }


            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(StateModel);
            ActionLogger.LogDetails(logDetails);

            //json = jsonSerialiser.Serialize(StateModel);
            return Request.CreateResponse(HttpStatusCode.OK, StateModel, Configuration.Formatters.JsonFormatter);
        }

        [HttpPost]
        [Route("api/Tamkeen/SubmitSurveyAnswersBot")]
        [LogAction(ActionId = 11628, ClientId = 8708)]
        public async Task<HttpResponseMessage> SubmitSurveyAnswersBot(SubmitSurvey req)
        {
            StateModel StateModel = new StateModel();
            #region ADD LOG DETAILS NEW  
            LogDetails logDetails = new LogDetails();
            logDetails.URL = "api/Tankeen/SubmitSurveyAnswers";
            logDetails.JSON = JsonConvert.SerializeObject(req);
            logDetails.Method = "POST";
            Stopwatch stopwatch = new Stopwatch();
            #endregion
            HttpClient httpClient = new HttpClient();
            try
            {
                stopwatch.Start();

                var GetURL = ConfigurationManager.AppSettings["BotbuilderBaseURL"] + "/api/MobileAPI/FetchQuestions?bot_id=" + req.RecipientID;

                logDetails.RequestToApi = GetURL;

                HttpResponseMessage response = await httpClient.GetAsync(GetURL);
                var ClientResponseResult = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = ClientResponseResult;

                ClientResponseResult = ClientResponseResult.Replace("\n", "").Replace("\r", "");
                var questions = JsonConvert.DeserializeObject<List<Class1>>(ClientResponseResult);
                SubmitAnswers submitrequest = new SubmitAnswers();
                submitrequest.recepient_id = req.RecipientID;
                submitrequest.sender_id = req.senderID;
                List<Question> submitquestions = new List<Question>();

                foreach (var question in questions.FirstOrDefault().questions)
                {
                    var answerstring = "";
                    int answersint = 0;
                    if (question.QuestionID == "1")
                    {
                        if (req.FirstAnswer.ToLower().Trim() == "yes")
                            answersint = 1;
                        else if (req.FirstAnswer.ToLower().Trim() == "no")
                            answersint = 0;
                    }
                    if (question.QuestionID == "2")
                    {
                        if (req.SecondAnswer.ToLower().Trim() == "yes")
                            answersint = 1;
                        else if (req.SecondAnswer.ToLower().Trim() == "no")
                            answersint = 0;
                    }
                    if (question.QuestionID == "3")
                    {
                        answerstring = req.ThirdAnswer;
                    }
                    if (question.QuestionID == "4")
                    {
                        answerstring = req.FourthAnswer;
                    }
                    if (question.type == "2")
                    {
                        submitquestions.Add(new Question
                        {
                            id = Int32.Parse(question.QuestionID),
                            question = question.question,
                            option = answersint,
                            type = Int32.Parse(question.type),
                            options = new List<string> { "no", "yes" }
                        });
                    }
                    if (question.type == "3")
                    {

                        submitquestions.Add(new Question
                        {
                            id = Int32.Parse(question.QuestionID),
                            question = question.question,
                            text = answerstring,
                            type = Int32.Parse(question.type)
                        });
                    }
                    if (question.type == "4")
                    {
                        submitquestions.Add(new Question
                        {
                            id = Int32.Parse(question.QuestionID),
                            question = question.question,
                            text = answerstring,
                            type = Int32.Parse(question.type)
                        });
                    }
                }

                submitrequest.questions = submitquestions.ToArray();
                var requestBody = JsonConvert.SerializeObject(submitrequest);


                logDetails.RequestToApi = JsonConvert.SerializeObject(requestBody);

                var PostURL = ConfigurationManager.AppSettings["BotbuilderBaseURL"] + "/api/ratingform/submit";
                HttpResponseMessage responsePost = await httpClient.PostAsync(PostURL, new StringContent(requestBody, Encoding.UTF8, "application/json"));
                var ClientResponseResultPost = await responsePost.Content.ReadAsStringAsync();
                if (ClientResponseResultPost.ToLower().Contains("true"))
                    StateModel.state = "Success";
                else
                {
                    StateModel.state = "Failure";
                    ActionLogger.LogDetails(logDetails);

                }

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responsePost);


            }
            catch (Exception ex)
            {
                StateModel.state = "Failure";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(StateModel);
                ActionLogger.LogException(ex, logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, StateModel, Configuration.Formatters.JsonFormatter);

            }


            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(StateModel);
            ActionLogger.LogDetails(logDetails);

            //json = jsonSerialiser.Serialize(StateModel);
            return Request.CreateResponse(HttpStatusCode.OK, StateModel, Configuration.Formatters.JsonFormatter);
        }


        [HttpPost]
        [Route("api/Tamkeen/SubmitSurveyAnswersHuman")]
        [LogAction(ActionId = 11629, ClientId = 8708)]
        public async Task<HttpResponseMessage> SubmitSurveyAnswersHuman(SubmitSurveyHumanAgentSystem req)
        {
            StateModel StateModel = new StateModel();
            #region ADD LOG DETAILS NEW  
            LogDetails logDetails = new LogDetails();
            logDetails.URL = "api/Tickets/SubmitSurveyAnswers";
            logDetails.JSON = JsonConvert.SerializeObject(req);
            logDetails.Method = "POST";
            Stopwatch stopwatch = new Stopwatch();
            #endregion
            HttpClient httpClient = new HttpClient();
            try
            {
                stopwatch.Start();

                var GetURL = ConfigurationManager.AppSettings["BotbuilderBaseURL"] + "/api/HumanAgentRating/FetchAgentQuestionsFromDB?bot_id=" + req.RecipientID;

                logDetails.RequestToApi = GetURL;

                HttpResponseMessage response = await httpClient.GetAsync(GetURL);
                var ClientResponseResult = await response.Content.ReadAsStringAsync();

                logDetails.ResponseFromApi = ClientResponseResult;

                ClientResponseResult = ClientResponseResult.Replace("\n", "").Replace("\r", "");
                var questions = JsonConvert.DeserializeObject<List<Class2>>(ClientResponseResult);
                HumanAgentSubmitRating submitrequest = new HumanAgentSubmitRating();
                submitrequest.recepient_id = req.RecipientID;
                submitrequest.sender_id = req.senderID;
                submitrequest.AgentName = req.AgentName;
                List<QuestionAgent> submitquestions = new List<QuestionAgent>();
                foreach (var question in questions.FirstOrDefault().questions)
                {
                    string answertext = "";
                    if (question.question_id == 1)
                    {
                        answertext = req.FirstAnswer;
                    }
                    if (question.question_id == 2)
                    {
                        answertext = req.SecondAnswer;
                    }
                    if (question.question_id == 3)
                    {
                        answertext = req.ThirdAnswer;
                    }
                    if (question.type == 2)
                    {
                        submitquestions.Add(new QuestionAgent
                        {
                            question_id = question.question_id,
                            question = question.question,
                            text = answertext,
                            type = question.type

                        }); ;
                    }
                }

                submitrequest.questions = submitquestions.ToArray();
                var requestBody = JsonConvert.SerializeObject(submitrequest);


                logDetails.RequestToApi = JsonConvert.SerializeObject(requestBody);

                var PostURL = ConfigurationManager.AppSettings["BotbuilderBaseURL"] + "/api/HumanAgentRating/SubmitUserAgentRating";
                HttpResponseMessage responsePost = await httpClient.PostAsync(PostURL, new StringContent(requestBody, Encoding.UTF8, "application/json"));
                var ClientResponseResultPost = await responsePost.Content.ReadAsStringAsync();
                if (ClientResponseResultPost.ToLower().Contains("success"))
                    StateModel.state = "Success";
                else
                {
                    StateModel.state = "Failure";

                }

                logDetails.ResponseFromApi = JsonConvert.SerializeObject(responsePost);


            }
            catch (Exception ex)
            {
                StateModel.state = "Failure";

                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(StateModel);
                ActionLogger.LogException(ex, logDetails);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, StateModel, Configuration.Formatters.JsonFormatter);
            }


            logDetails.ResponseToLabiba = JsonConvert.SerializeObject(StateModel);
            ActionLogger.LogDetails(logDetails);

            //json = jsonSerialiser.Serialize(StateModel);
            return Request.CreateResponse(HttpStatusCode.OK, StateModel, Configuration.Formatters.JsonFormatter);
        }


        [HttpPost]
        [Route("api/Tamkeen/ListItemsPayNoCheck")]
        [LogAction(ActionId = 12832, ClientId = 8708)]
        public async Task<HttpResponseMessage> ListItemsPayNoCheck(PayNoCheckRequest req)
        {
            HttpClient httpClient = new HttpClient();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "post";
            logDetails.JSON = JsonConvert.SerializeObject(req);

            try
            {
                StateModel stateResult = new StateModel();
                /******************************************************************/


                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");


                if (string.IsNullOrWhiteSpace(req.PAYNO))
                {

                    stateResult.state = "Failure";
                    stateResult.SlotFillingState = "No PAYNo found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }


                PayNoCheckReq payNoCheckReq = new PayNoCheckReq();


                payNoCheckReq.PAYNo = req.PAYNO;

                var requestBody = JsonConvert.SerializeObject(payNoCheckReq);

                Stopwatch timer = new Stopwatch();
                timer.Start();
                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}UploadSVDocument/ListItems", new StringContent(requestBody, Encoding.UTF8, "application/json"));

                timer.Stop();
                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;


                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<ListItemsPayNoCheck>(ClientResponseResult);
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                LogModel modelx = new LogModel();
                modelx.LogTime = DateTime.Now;
                modelx.ActionName = "ListItemsPayNoCheck";
                modelx.Parameter = JsonConvert.SerializeObject(req);

                modelx.ResponseFromLabiba = JsonConvert.SerializeObject(ClientResponseResult);
                LiteDBServices liteDBB = new LiteDBServices();
                liteDBB.InsertLogRow(modelx);
                if (data.status == 200)
                {

                    stateResult.state = "Success";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                else if (data.status == 500)
                {
                    stateResult.state = "Invalid";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    stateResult.state = "Failure";
                    //stateResult.SlotFillingState = data.msg;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }

            }
            catch (Exception ex)
            {
                StateModel stateResult = new StateModel();

                ActionLogger.LogException(ex, logDetails);
                stateResult.state = "Failure";
                stateResult.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }


        [HttpPost]
        [Route("api/Tamkeen/VerifyMobile")]
        [LogAction(ActionId = 14061, ClientId = 8708)]
        public async Task<HttpResponseMessage> VerifyMobile(VerifyMobile verifyMobile)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                logDetails.RequestToApi = JsonConvert.SerializeObject(verifyMobile);

                if (verifyMobile.Mobile.StartsWith("0097"))
                {
                    verifyMobile.Mobile = verifyMobile.Mobile.Substring(4);
                }
                else if (verifyMobile.Mobile.StartsWith("+97"))
                {
                    verifyMobile.Mobile = verifyMobile.Mobile.Substring(3);

                }
                else
                {
                    verifyMobile.Mobile = verifyMobile.Mobile;

                }

                var BodyRequest = JsonConvert.SerializeObject(verifyMobile);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/Portal/VerifyMobile", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var verifyMobileResp = JsonConvert.DeserializeObject<VerifyMobileResp>(ClientResponseResult);

                if (verifyMobileResp.status == 200)
                {


                    stateModel.state = "Success";
                    stateModel.SlotFillingState = verifyMobileResp.data.Fullname;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                else
                {

                    stateModel.state = "not_found";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

            }
            catch (Exception ex)
            {


                ActionLogger.LogException(ex, logDetails);
                stateModel.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
            }
        }



        [HttpPost]
        [Route("api/Tamkeen/UpdateDetails")]
        [LogAction(ActionId = 15216, ClientId = 8708)]
        public async Task<HttpResponseMessage> UpdateDetails(UpdateDetails Model)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                logDetails.RequestToApi = JsonConvert.SerializeObject(Model);
                var BodyRequest = JsonConvert.SerializeObject(Model);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/UploadSVDocument/UpdateItem", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));


                //var response = "{\"PAYNo\":\"ES/22693-2/00703/PAYREQ-04\",\"SVItem\":\"ICT-ITEM-01415\",\"Deliverystatus\":true}";

                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var UpdateDetailsres = JsonConvert.DeserializeObject<UpdateDetailsResp>(ClientResponseResult);

                if (UpdateDetailsres.status == 200)
                {


                    stateModel.state = "Success";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }
                else
                {

                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

            }
            catch (Exception ex)
            {
                StateModel stateResult = new StateModel();

                ActionLogger.LogException(ex, logDetails);
                stateResult.state = "Failure";
                stateResult.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }



        [HttpPost]
        [Route("api/Tamkeen/GetSVNumber")]
        [LogAction(ActionId = 15217, ClientId = 8708)]
        public async Task<HttpResponseMessage> GetSVNumber(GetSVNumber Model)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                logDetails.RequestToApi = JsonConvert.SerializeObject(Model);

                GetSVNumberRequest Newitem = new GetSVNumberRequest();

                Newitem.PAYNo = Model.PAYNO;

                var BodyRequest = JsonConvert.SerializeObject(Newitem);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/UploadSVDocument/ListItems", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));





                //
                List<List<object>> sheetValues = new List<List<object>>();

                IList<IList<Object>> values = await GetSheet1();
                if (values != null && values.Count() > 0)
                {
                    foreach (var row in values)
                    {
                        if (row[0].ToString() == Model.PAYNO)
                        {
                            sheetValues.Add((List<Object>)row);
                        }
                    }
                }

                //






                //var response = "{\"PAYNo\":\"ES/22693-2/00703/PAYREQ-04\"}";
                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                Int32 responseHttpStatusCode = (Int32)response.StatusCode;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var Result = JsonConvert.DeserializeObject<GetSVNumberRespones>(ClientResponseResult);



                if (responseHttpStatusCode == 200)
                {

                   ////var svitem = "";
                   ////foreach (var item in Result.data)
                   ////{
                   ////    //if (Model.ItemName == item.ItemName)
                   ////    if (Model.ItemName == item.ItemName)
                   ////    {
                   ////        svitem = item.ItemNumber;

                   ////    }

                   ////}



                    var ClientData = Result.data;

                    if (sheetValues != null)
                    {
                        if (sheetValues.Count() != 0)
                        {
                            foreach (var row in sheetValues)
                            {
                                var i = row[1].ToString();
                                ClientData = ClientData.Where(x => x.ItemNumber != i).ToList();
                            }
                        }
                    }



                    stateModel.state = "Success";
                    stateModel.SlotFillingState = ClientData[0].ItemNumber;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);

                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);

                }
                else
                {

                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

            }
            catch (Exception ex)
            {
                StateModel stateResult = new StateModel();

                ActionLogger.LogException(ex, logDetails);
                stateResult.state = "Failure";
                stateResult.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }



        [HttpPost]
        [Route("api/Tamkeen/GetScheme")]
        [LogAction(ActionId = 15213, ClientId = 8708)]
        public async Task<HttpResponseMessage> GetScheme(GetScheme Model)
        {
            HttpClient httpClient = new HttpClient();
            StateModel stateModel = new StateModel();
            LogDetails logDetails = new LogDetails();
            logDetails.Method = "POST";
            try
            {
                logDetails.RequestToApi = JsonConvert.SerializeObject(Model);

                GetSchemeRequest scheme = new GetSchemeRequest();

                scheme.PAYNo = Model.PAYNO;

                var BodyRequest = JsonConvert.SerializeObject(scheme);
                logDetails.JSON = BodyRequest;
                httpClient.DefaultRequestHeaders.Add("Token", $"{ConfigurationManager.AppSettings["Token"]}");
                Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpResponseMessage response = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["APIURLV2"]}/UploadSVDocument/ListItems", new StringContent(BodyRequest, Encoding.UTF8, "application/json"));


                // var response = "{\"PAYNo\":\"ES/22693-2/00703/PAYREQ-04\"}";
                timer.Stop();

                logDetails.APIExecutionTime = timer.ElapsedMilliseconds;
                Int32 responseHttpStatusCode = (Int32)response.StatusCode;
                var ClientResponseResult = await response.Content.ReadAsStringAsync();
                logDetails.ResponseFromApi = JsonConvert.SerializeObject(ClientResponseResult);

                var Result = JsonConvert.DeserializeObject<GetSVNumberRespones>(ClientResponseResult);

                //var res = Result.data.Where(x => x.ItemName == Model.ItemName).FirstOrDefault();


                if (responseHttpStatusCode == 200)
                {

                    var svitem = "";
                    foreach (var item in Result.data)
                    {
                        if (Model.ItemName == item.ItemName)
                        {
                            svitem = item.SchemeCode;

                        }

                    }
                    stateModel.state = "Success";
                    stateModel.SlotFillingState = svitem;
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);

                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);

                }
                else
                {

                    stateModel.state = "Failure";
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateModel);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateModel, Configuration.Formatters.JsonFormatter);
                }

            }
            catch (Exception ex)
            {
                StateModel stateResult = new StateModel();

                ActionLogger.LogException(ex, logDetails);
                stateResult.state = "Failure";
                stateResult.SlotFillingState = ex.Message;
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }




        public System.Drawing.Image LoadImage(string path)
        {
            //data:image/gif;base64,
            //this image is a single pixel (black)
            byte[] bytes = Convert.FromBase64String(path);

            System.Drawing.Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = System.Drawing.Image.FromStream(ms);

            }
            return image;
        }

        public static byte[] ConvertUrlToBytes(string imageUrl)
        {
            //string base64 = "";
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebClient webClient = new WebClient();
                byte[] filedate = webClient.DownloadData(imageUrl);


                return filedate;

            }
            catch
            {

                return null;

            }

        }

    }
}