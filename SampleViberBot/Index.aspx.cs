//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.UI;
//using System.Web.UI.WebControls;

//namespace SampleViberBot
//{
//    public partial class Index : System.Web.UI.Page
//    {
//        protected void Page_Load(object sender, EventArgs e)
//        {

//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;


namespace SampleViberBot
{
    public partial class Index : System.Web.UI.Page
    {
        const string paAuthenticationToken = "4c3638d0c0000a7e-8d924219dcd38f5-cb2385bcec122156";
        const string viberUrlForSendingMessagesTo = "https://chatapi.viber.com/pa/send_message";


        //---- Input message structure start: ----
        public class SenderJsonNoId
        {//Json mapping :
            public string name;
            public string avatar;
        };
        public class SenderJson
        {//Json mapping :
            public string id;
            public string name;
            public string avatar;
            public string country;
            public string language;
            public string api_version;
        };

        public class locationJson
        {
            public float lat;
            public float lon;
        };
        public class contactJson
        {
            public string name;
            public string phone_number;
        };
        public class receivedMessageJson
        {//Json mapping :
            public string type;
            public string text;
            public string media;
            public locationJson location;
            public string tracking_data;
            public contactJson contact;
        };

        //---- Input message structure end ----


        //---- outgoing message structure start: ----

        public class KeyboardButtonsJson
        {
            public string ActionType;
            public string ActionBody;
            public string BgColor;
            public int Columns;
            public int Rows;
            public string Text;
            public string TextHAlign;
            public string Image;
            public string TextSize;
            public int TextOpacity;

            public KeyboardButtonsJson(string actionType, string actionBody, string backgroundColor, string text, string textAlign,
                int buttonColumns = 6, int buttonsRows = 1, string ImageUrl = null, bool isLargeTextSize = false, int textOpacity = 100)
            {
                const int maxNumColums = 6;
                const int maxNumRows = 2;
                buttonColumns = ((buttonColumns > 0) && (buttonColumns < maxNumColums)) ? buttonColumns : maxNumColums;
                buttonsRows = ((buttonsRows > 0) && (buttonsRows < maxNumColums)) ? buttonsRows : maxNumRows;

                this.Image = ImageUrl;
                this.ActionType = actionType;
                this.ActionBody = actionBody;
                this.BgColor = backgroundColor;
                this.Text = text;
                this.TextHAlign = textAlign;//possible values: "left", "center", "right"
                this.Columns = buttonColumns;
                this.Rows = buttonsRows;
                this.TextSize = isLargeTextSize ? "large" : "regular";// (can also be "small")
                this.TextOpacity = ((textOpacity >= 0) && (textOpacity <= 100)) ? textOpacity : 100;
            }
        };


        public class KeyboardJson
        {
            public string Type;
            public List<KeyboardButtonsJson> Buttons;

            public KeyboardJson(List<KeyboardButtonsJson> buttons, string queryForMedicalSearch)
            {
                this.Type = "keyboard";
                this.Buttons = buttons;
            }
        };


        public class SentMessageJson
        {
            public string auth_token;
            public string receiver;
            public string tracking_data;
            public KeyboardJson keyboard;
            public string text;
            public string type;
            public string media;
            public SenderJsonNoId sender;

            public SentMessageJson(string paAuthenticationToken, string receiver, string tracking_data, string senderName, string textToSend)
            {
                this.auth_token = paAuthenticationToken;
                this.receiver = receiver;
                this.tracking_data = (null == tracking_data) ? "" : tracking_data;

                this.sender = new SenderJsonNoId();
                this.sender.name = senderName;
                this.sender.avatar = "https://avatars0.githubusercontent.com/u/9072931?v=3&s=40";

                this.media = "";
                this.type = "text";
                this.text = textToSend;
            }
        };

        //---- outgoing message structure end.   ----

        private string getJsonString(System.IO.Stream postedData)
        {
            byte[] readBytes = new byte[postedData.Length];
            postedData.Read(readBytes, 0, (int)postedData.Length);
            string readString = System.Text.Encoding.UTF8.GetString(readBytes).TrimEnd('\0');
            return readString;
        }

        private void replyEmptyResponse()
        {
            Response.Write("{}");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string readString = getJsonString(Request.GetBufferlessInputStream());

            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            Dictionary<string, object> dict = serializer.Deserialize<Dictionary<string, object>>(readString);
            if (null == dict)
            {
                replyEmptyResponse();
                return;
            }

            object eventTypeOject;
            if (!dict.TryGetValue("event", out eventTypeOject))
            {
                replyEmptyResponse();
                return;//ERROR
            }

            string eventType = eventTypeOject.ToString().ToLower();
            if (eventType.Equals("message"))
            {
                object senderValue;
                if (!dict.TryGetValue("sender", out senderValue))
                {
                    replyEmptyResponse();
                    return;//ERROR
                }
                SenderJson SenderObject = serializer.ConvertToType<SenderJson>(senderValue);
                if (null == SenderObject)
                {
                    replyEmptyResponse();
                    return;//TODO: ERROR
                }

                object sentMessage;
                if (dict.TryGetValue("message", out sentMessage))
                {//Send reply message to user:
                    receivedMessageJson messageObject = serializer.ConvertToType<receivedMessageJson>(sentMessage);

                    // Get received message text and send it back to the sender:
                    String arrivingText = "Sending you back " + messageObject.text;
                    SentMessageJson autoSentReplyToUser = new SentMessageJson(paAuthenticationToken, SenderObject.id, messageObject.tracking_data, SenderObject.name, arrivingText);
                    string jsonStr = serializer.Serialize(autoSentReplyToUser);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(viberUrlForSendingMessagesTo);
                    var data = Encoding.UTF8.GetBytes(jsonStr);
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    httpWebRequest.ContentLength = data.Length;
                    using (var stream = httpWebRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);

                    }
                }
            }

            replyEmptyResponse();
        }
    }
}