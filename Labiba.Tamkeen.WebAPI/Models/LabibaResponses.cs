using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labiba.Tamkeen.WebAPI.Models
{
    public class LabibaResponses
    {
        public class HeroCardsModel
        {
            public class Image
            {
                public string URL { get; set; } = string.Empty;
            }

            public class Button
            {
                public string Title { get; set; } = string.Empty;
                public string Type { get; set; } = string.Empty;
                public string Value { get; set; } = string.Empty;

                public static implicit operator List<object>(Button v)
                {
                    throw new NotImplementedException();
                }
            }
            public class hero_cards
            {
                public string Title { get; set; } = string.Empty;
                public string Subtitle { get; set; } = string.Empty;
                public string Text { get; set; } = string.Empty;
                public List<Image> Images { get; set; } = new List<Image>();
                public List<Button> Buttons { get; set; } = new List<Button>();
            }

            public class RootObject
            {

                public string response { get; set; } = string.Empty;

                public string success_message { get; set; } = string.Empty;

                public string failure_message { get; set; } = string.Empty;

                public List<hero_cards> hero_cards { get; set; } = new List<hero_cards>();
            }
        }

        public class StateModel
        {
            public string state { get; set; }
            public string SlotFillingState { get; set; }
        }


        public class TextModel
        {
            public string text { get; set; }
            public string SlotFillingText { get; set; }
        }
    }
}
