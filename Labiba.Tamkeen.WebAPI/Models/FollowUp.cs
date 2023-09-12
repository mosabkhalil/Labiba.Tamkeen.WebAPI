using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labiba.Tamkeen.WebAPI.Models
{
    public class FollowUp

    {
        public class Complainsobject
        {
            public string app_status { get; set; }
            public string sessionid { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
        }

        public class ComplainsAction
        {
            public string reference { get; set; }
            public string mobile { get; set; }
            public string otp { get; set; }
            public string lang { get; set; }

        }

        public class ComplainsRequest
        {
            public string reference { get; set; }
            public string mobile { get; set; }
            public string otp { get; set; }
            public string lang { get; set; }
        }
        public class ResetPasswordobject
        {
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }
        public class ResetPasswordAction
        {
            public string id { get; set; }
            public string mobile { get; set; }

            public string otp { get; set; }

        }
        public class ResetPasswordRequest
        {
            public string id { get; set; }
            public string mobile { get; set; }

            public string otp { get; set; }
        }
        public class TimeValidationRequest
        {
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string TimeZone { get; set; }

        }

        public class VerifyAccountobject
        {
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }

            public DataVerifyAccount Data { get; set; }




        }
        public class DataVerifyAccount
        {

            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string IDType { get; set; }
            public string CPR { get; set; }
            public string Mobile { get; set; }
            public string OtherNumber { get; set; }
            public string FaxNumber { get; set; }
            public string isESPVendor { get; set; }
            public string isTP { get; set; }
            public string Flat { get; set; }
            public string Building { get; set; }
            public string Road { get; set; }
            public string Block { get; set; }
            public string Area { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string DateOfBirth { get; set; }
            public string Gender { get; set; }
            public string Nationality { get; set; }
        }
        public class VerifyAccountAction
        {
            public string CPR { get; set; }

        }
        public class VerifyAccountRequest
        {
            public string CPR { get; set; }

        }

        public class ResgisterUserobject
        {
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }

            public DataResgisterUser data { get; set; }




        }
        public class DataResgisterUser
        {

            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string IDType { get; set; }
            public string CPR { get; set; }
            public string Mobile { get; set; }
            public string OtherNumber { get; set; }
            public string FaxNumber { get; set; }
            public string isESPVendor { get; set; }
            public string isTP { get; set; }
            public string Flat { get; set; }
            public string Building { get; set; }
            public string Road { get; set; }
            public string Block { get; set; }
            public string Area { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string DateOfBirth { get; set; }
            public string Gender { get; set; }
            public string Nationality { get; set; }
        }
        public class ResgisterUserAction
        {
            public string CPR { get; set; }

            public string Mobile { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Flat { get; set; }
            public string Building { get; set; }
            public string Road { get; set; }
            public string Block { get; set; }
            public string Area { get; set; }
            public string IDType { get; set; }
            public string Gender { get; set; }
            public string Nationality { get; set; }
            public string DateOfBirth { get; set; }
            public string Password { get; set; }

        }
        public class ResgisterUserRequest
        {
            public string CPR { get; set; }

            public string Mobile { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Flat { get; set; }
            public string Building { get; set; }
            public string Road { get; set; }
            public string Block { get; set; }
            public string Area { get; set; }
            public string IDType { get; set; }
            public string Gender { get; set; }
            public string Nationality { get; set; }
            public string DateOfBirth { get; set; }
            public string Password { get; set; }

        }


        public class AddDataRequest
        {
            //public string Information { get; set; }
            //public string SVNo { get; set; }
            public string PaymentNumber { get; set; }
            public string Name { get; set; }
            public string ItemNumber { get; set; }



        }
        public class AddData
        {
            public string SVNo { get; set; }
            public string SVItem { get; set; }
            public string itemNumber { get; set; }


        }

        public class UploadAttachmentsRequest
        {

            public string PayNO { get; set; }
            public string Location { get; set; }
            public string SVItem { get; set; }
            public string Type { get; set; }
            public string Serial { get; set; }
            public string Image { get; set; }
            public bool CustomerConfirmation { get; set; }
            public string CustomerComment { get; set; }

            /*public object SVNo { get; set; }
                    public string PAYNo { get; set; }
                    public string Location { get; set; }
                    public string SVItem { get; set; }
                    public bool SVCofirmedByCustomer { get; set; }
                    public string SVCustremarks { get; set; }
                    public string Type { get; set; }
                    public string Serial { get; set; }
                    public string Imgdata { get; set;} */
        }


        public class UploadAttachmentsReq
        {


            public string PAYNo { get; set; }
            public string Location { get; set; }
            public string SVItem { get; set; }
            public bool SVCofirmedByCustomer { get; set; }
            public string SVCustremarks { get; set; }
            public string Type { get; set; }
            public string Serial { get; set; }
            public string Imgdata { get; set; }
            //public string PAYNo { get; set; }
            //    public object Location { get; set; }
            //    public object SVItem { get; set; }
            //    public bool SVCofirmedByCustomer { get; set; }
            //    public string SVCustremarks { get; set; }
            //    public object Type { get; set; }
            //    public object Serial { get; set; }
            //    public object Imgdata { get; set; }



        }
        /*
        public class UploadAttachment
        {
            public UploadAttachmentData data { get; set; }
            public string status { get; set; }
            public string msg { get; set; }
            public string Code { get; set; }
        }
        public class UploadAttachmentData
        {
            public string SVNo { get; set; }
            public string Mobile { get; set; }
            public string CPR { get; set; }
            public string FullName { get; set; }
            public string SVItem { get; set; }
            public string Serial { get; set; }
            public string Location { get; set; }
            public string Imgdata { get; set; }
            public string Type { get; set; }
        }*/


        public class UploadAttachment
        {
            public UploadAttachmentData data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }

        public class UploadAttachmentData
        {

            public string SVNo { get; set; }
            public string SVCustremarks { get; set; }
            public bool SVCofirmedByCustomer { get; set; }
            public object PayNo { get; set; }
            public object Mobile { get; set; }
            public object CPR { get; set; }
            public object FullName { get; set; }
            public string SVItem { get; set; }
            public bool Deliverystatus { get; set; }
            public string Serial { get; set; }
            public string Location { get; set; }
            public string Imgdata { get; set; }
            public string Type { get; set; }
            public object Items { get; set; }
            public object Imagef { get; set; }
            /* public object SVNo { get; set; }
          public string PAYNo { get; set; }
          public string SVCustremarks { get; set; }
          public bool SVCofirmedByCustomer { get; set; }
          public object Mobile { get; set; }
          public object CPR { get; set; }
          public object FullName { get; set; }
          public string SVItem { get; set; }
          public bool Deliverystatus { get; set; }
          public string Serial { get; set; }
          public string Location { get; set; }
          public string Imgdata { get; set; }
          public string Type { get; set; }
          public object Items { get; set; }
          public object Imagef { get; set; }*/
        }


        //public class ListItemsResponse
        //{
        //    public DataListItems[] data { get; set; }
        //    public string status { get; set; }
        //    public string msg { get; set; }
        //    public string Code { get; set; }
        //}
        //public class DataListItems
        //{
        //    public string ItemNumber { get; set; }
        //    public string ItemName { get; set; }
        //    public string VendorName { get; set; }
        //    public string Scheme { get; set; }
        //    public string TotalCost { get; set; }
        //    public string Quantity { get; set; }
        //}
        public class ListItemsResponse
        {
            public DataListItems[] data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }

        public class DataListItems
        {
            public string ItemNumber { get; set; }
            public string ItemName { get; set; }
            public string VendorName { get; set; }
            public string Scheme { get; set; }
            public string SchemeCode { get; set; }
            public float TotalCost { get; set; }
            public int Quantity { get; set; }
        }
        public class ListItemsRequest
        {
            public string PAYNO { get; set; }
            //public string lang { get; set; }
        }
        public class ListItems
        {
            public string PAYNo { get; set; }
        }





        public class ImageModel
        {
            public string ImageURL { get; set; }
            public string Base64 { get; set; }
            public bool Id { get; set; } = false;

        }

        public class ListItemsState
        {
            public string UserChoice { get; set; }
            public string ItemList { get; set; }
        }

        public class ListItemsStateResponse
        {

            public ListItems1[] ListItems1 { get; set; }
        }
        public class ListItems1
        {
            public int ItemOrder { get; set; }

            public string ItemName { get; set; }
            public string ItemNumber { get; set; }
        }

        public class passwordsConfirm
        {
            public string FirstPassword { get; set; }
            public string SecondPassword { get; set; }
        }


        /////////
        ///
        public class PayNoCheckRequest
        {
            public string PAYNO { get; set; }
        }


        public class PayNoCheckReq
        {
            public string PAYNo { get; set; }
        }

        public class ListItemsPayNoCheck
        {
            public PayNoCheckData[] data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }

        public class PayNoCheckData
        {
            public string ItemNumber { get; set; }
            public string ItemName { get; set; }
            public string VendorName { get; set; }
            public string Scheme { get; set; }
            public float TotalCost { get; set; }
            public int Quantity { get; set; }
        }
        public class VerifyMobile
        {
            public string Mobile { get; set; }


        }

        public class VerifyMobileResp
        {
            public VerifyMobileRespData data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }

        public class VerifyMobileRespData
        {
            public string Mobile { get; set; }
            public string Fullname { get; set; }
        }
        public class UpdateDetails
        {
            public string PayNo { get; set; }
            public string SVItem { get; set; }
            public bool DeliveryStatus { get; set; }
        }

        public class UpdateDetailsResp
        {
            public object data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }


        public class GetSVNumber
        {
            public string PAYNO { get; set; }
            public string ItemName { get; set; }
            public string ItemNumber { get; set; }

        }
        public class GetSVNumberRequest
        {
            public string PAYNo { get; set; }
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Datum
        {
            public string ItemNumber { get; set; }
            public string ItemName { get; set; }
            public string VendorName { get; set; }
            public string Scheme { get; set; }
            public string SchemeCode { get; set; }
            public double TotalCost { get; set; }
            public int Quantity { get; set; }
        }

        public class GetSVNumberRespones
        {
            public List<Datum> data { get; set; }
            public int status { get; set; }
            public string msg { get; set; }
            public int Code { get; set; }
        }

        public class GetScheme
        {
            public string PAYNO { get; set; }
            public string ItemName { get; set; }

        }
        public class GetSchemeRequest
        {
            public string PAYNo { get; set; }


        }



    }
}