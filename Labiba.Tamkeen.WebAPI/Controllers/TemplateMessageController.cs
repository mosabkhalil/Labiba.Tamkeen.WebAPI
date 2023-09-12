using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Labiba.Tamkeen.WebAPI.Models;
using Labiba.Actions.Logger;
using Labiba.Actions.Logger.Manager;
using Newtonsoft.Json;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using static Labiba.Tamkeen.WebAPI.Models.LabibaResponses;

namespace Labiba.Tamkeen.WebAPI.Controllers
{
    public class TemplateMessageController : ApiController
    {

        [HttpPost]
        [Route("api/sitevisit_greeting_template_v2")]
        [LogAction(ActionId = 15104, ClientId = 8708)]
        [Obsolete]
        public async Task<HttpResponseMessage> sitevisit_greeting_template_v2(TemplateReq Req)
        {
            StateModel stateResult = new StateModel();
            var logDetails = new LogDetails();
            logDetails.Method = "POST";
            logDetails.Forms = "";
            logDetails.Headers = "";
            logDetails.URL = $"{ConfigurationManager.AppSettings["FullChannelURL"]}";

            try
            {

                logDetails.ResponseFromApi = JsonConvert.SerializeObject("");

                if (string.IsNullOrWhiteSpace(Req.UserPhoneNumber.ToString()))
                {
                    stateResult.state = "Failure";
                    logDetails.RequestToApi = JsonConvert.SerializeObject(Req);
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
                }
                PassUserScopedParameters passUserScopedParameters = new PassUserScopedParameters();
                List<USParameter> uSParameter = new List<USParameter>();
                uSParameter.Add(new USParameter { key = "UserName", value = Req.CustomerName });
                uSParameter.Add(new USParameter { key = "PayNumber", value = Req.PaymentReferenceNumber });

                var JSONPassReq = new PassUserScopedParameters
                {
                    SenderId = Req.UserPhoneNumber,
                    Parameters = uSParameter.ToArray(),
                    RecipientId = $"{ConfigurationManager.AppSettings["PageId"]}",

                };
                var PassParameterStatusCode = await PassParameters(JSONPassReq);



                List<string> RecipientId = new List<string>();
                TemplatMessage templatMessage = new TemplatMessage();
                List<Message> MessageList = new List<Message>();
                List<Option> options = new List<Option>();
                List<Body> parameters = new List<Body>();
                Templatemesssageparamaters templatemesssageparamaters = new Templatemesssageparamaters();


                //RecipientId
                RecipientId.Add(Req.UserPhoneNumber);

                //Parameters List
                parameters.Add(new Body { text=Req.CustomerName});
                parameters.Add(new Body { text = Req.PaymentReferenceNumber });
                parameters.Add(new Body { text = Req.CustomerName });
                parameters.Add(new Body { text = Req.PaymentReferenceNumber });

                //buttons
                options.Add(new Option { value = "WhatsApp Site Visit" });
                options.Add(new Option { value = "معاينة الموقع عبر تطبيق الوتس اب" });

                //Adding Parameters list to templatemesssageparamaters
                    templatemesssageparamaters.Body = parameters.ToArray();

                templatemesssageparamaters.Options = options.ToArray();

                MessageList.Add(new Message { Type = "template", TemplateName = "sitevisit_greeting_template_v2", Language = "ar", TemplateMesssageParamaters = templatemesssageparamaters });
                
                var JSONbodyReq = new TemplatMessage
                {
                    PageId = $"{ConfigurationManager.AppSettings["PageId"]}",
                    Channel = $"{ConfigurationManager.AppSettings["Channel"]}",
                    RecipientIds = RecipientId.ToArray(),
                    Messages = MessageList.ToArray()
                };

                var StatusCode = await SendMessage(JSONbodyReq);

                if (StatusCode && PassParameterStatusCode)
                {
                    stateResult.state = "Success";
                    logDetails.ResponseFromApi = JsonConvert.SerializeObject(StatusCode);
                    logDetails.RequestToApi = JsonConvert.SerializeObject(JSONbodyReq);
                    logDetails.JSON = JsonConvert.SerializeObject(JSONbodyReq);
                    logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                    ActionLogger.LogDetails(logDetails);
                    return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);

                }



                stateResult.state = "Failure";
                logDetails.ResponseToLabiba = JsonConvert.SerializeObject(stateResult);
                logDetails.JSON = JsonConvert.SerializeObject(Req);

                ActionLogger.LogDetails(logDetails);
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                ActionLogger.LogException(ex, logDetails);
                logDetails.JSON = JsonConvert.SerializeObject(Req);

                stateResult.state = "Failure";
                return Request.CreateResponse(HttpStatusCode.OK, stateResult, Configuration.Formatters.JsonFormatter);
            }
        }

        public async Task<bool> SendMessage(TemplatMessage req)
        {

            var bodyReq = JsonConvert.SerializeObject(req);
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{ConfigurationManager.AppSettings["BaseChannelURL"]}")
            };

           
            var NewAuth = await GetNewAuth();
            var ADNewAuth = JsonConvert.DeserializeObject<CheckExpiryReq>(NewAuth);
           

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ADNewAuth.Token}");

            var responseFromLabiba = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["ChannelURL"]}", new StringContent(bodyReq, Encoding.UTF8, "application/json"));
            var RStatuscode = responseFromLabiba.IsSuccessStatusCode;



            return RStatuscode;
        }


        public async Task<string> CheckExpiry(CheckExpiryReq req)
        {

            var bodyReq = JsonConvert.SerializeObject(req);

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{ConfigurationManager.AppSettings["BaseChannelURL"]}")
            };

            var responseFromLabiba = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["CheckExpURL"]}", new StringContent(bodyReq, Encoding.UTF8, "application/json"));
            var RStatuscode = responseFromLabiba.Content.ReadAsStringAsync();

            return await RStatuscode;
        }


        public async Task<string> GetNewAuth()
        {
            GetNewAuthReq authReq = new GetNewAuthReq();

            authReq.Username = $"{ConfigurationManager.AppSettings["UserName"]}";
            authReq.Password = $"{ConfigurationManager.AppSettings["Password"]}";

            var bodyReq = JsonConvert.SerializeObject(authReq);

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{ConfigurationManager.AppSettings["BaseChannelURL"]}")
            };

            var responseFromLabiba = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["loginURL"]}", new StringContent(bodyReq, Encoding.UTF8, "application/json"));

            var NewAuth = responseFromLabiba.Content.ReadAsStringAsync();

            return NewAuth.Result.ToString();
        }

        public async Task<bool> PassParameters(PassUserScopedParameters req)
        {

            var bodyReq = JsonConvert.SerializeObject(req);
            var httpClient = new HttpClient();
            

            var responseFromLabiba = await httpClient.PostAsync($"{ConfigurationManager.AppSettings["PassUserScopedParametersURL"]}", new StringContent(bodyReq, Encoding.UTF8, "application/json"));
            var RStatuscode = responseFromLabiba.IsSuccessStatusCode;



            return RStatuscode;
        }
    }
}
