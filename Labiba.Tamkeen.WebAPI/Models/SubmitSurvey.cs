using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labiba.Tamkeen.WebAPI.Models
{


    public class AgentName
    {
        public string RecipientID { get; set; }
        public string senderID { get; set; }
    }

    public class getAgentResponse
    {
        public bool Success { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }


    public class SubmitSurvey
    {
        public string language { get; set; }
        public string FirstAnswer { get; set; }
        public string SecondAnswer { get; set; }
        public string ThirdAnswer { get; set; }
        public string FourthAnswer { get; set; }
        public string senderID { get; set; }
        public string RecipientID { get; set; }

    }
    public class SubmitSurveyHumanAgentSystem
    {
        public string language { get; set; }
        public string FirstAnswer { get; set; }
        public string SecondAnswer { get; set; }
        public string ThirdAnswer { get; set; }
        public string AgentName { get; set; }
        public string senderID { get; set; }
        public string RecipientID { get; set; }
    }
    public class SubmitAnswers
    {
        public Question[] questions { get; set; }
        public string recepient_id { get; set; }
        public string sender_id { get; set; }
    }

    public class Question
    {
        public int id { get; set; }
        public string question { get; set; }
        public string rating { get; set; }
        public int type { get; set; }
        public string text { get; set; }
        public int option { get; set; }
        public List<string> options { get; set; }


    }


    public class HumanAgentSubmitRating
    {
        public QuestionAgent[] questions { get; set; }
        public string recepient_id { get; set; }
        public string sender_id { get; set; }
        public string AgentName { get; set; }
        public Timestamp Timestamp { get; set; }
    }

    public class Timestamp
    {
        public DateTime date { get; set; }
    }

    public class QuestionAgent
    {
        public int question_id { get; set; }
        public string question { get; set; }
        public int rating { get; set; }
        public string text { get; set; }
        public int type { get; set; }
    }





    public class FetchQuestions
    {
        public Class1[] Property1 { get; set; }
    }

    public class Class1
    {
        public string _id { get; set; }
        public string recepient_id { get; set; }
        public Question2[] questions { get; set; }
    }

    public class Question2
    {
        public string QuestionID { get; set; }
        public string question { get; set; }
        public string type { get; set; }
        public double IncreaseRatingBy { get; set; }
    }




    public class FetchQuestionAgent
    {
        public Class2[] Property1 { get; set; }
    }

    public class Class2
    {
        public string _id { get; set; }
        public QuestionsAgent[] questions { get; set; }
        public DateTime TimeStamp { get; set; }
        public string recepient_id { get; set; }
    }

    public class QuestionsAgent
    {
        public int question_id { get; set; }
        public string question { get; set; }
        public float MaxStarsCount { get; set; }
        public float RatingStart { get; set; }
        public float IncreaseRatingBy { get; set; }
        public int type { get; set; }
    }

}