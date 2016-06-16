using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Apple_APNs_PushNotification
{
    public class AppleNotificationPayload
    {
        public NotificationAlert Alert { get; set; }

        public int? ContentAvailable { get; set; }

        public int? Badge { get; set; }

        public string Sound { get; set; }

        public bool HideActionButton { get; set; }

        public Dictionary<string, object[]> CustomItems
        {
            get;
            private set;
        }

        #region Constructor
        public AppleNotificationPayload()
        {
            HideActionButton = false;
            Alert = new NotificationAlert();
            CustomItems = new Dictionary<string, object[]>();
        }

        public AppleNotificationPayload(string alert)
        {
            HideActionButton = false;
            Alert = new NotificationAlert() { Body = alert };
            CustomItems = new Dictionary<string, object[]>();
        }

        public AppleNotificationPayload(string alert, int badge)
            : this(alert, badge, null)
        {

        }

        public AppleNotificationPayload(string alert, int badge, string sound)
		{
			HideActionButton = false;
			Alert = new NotificationAlert() { Body = alert };
			Badge = badge;
			Sound = sound;
			CustomItems = new Dictionary<string, object[]>();
		}
        #endregion

        public void AddCustom(string key, params object[] values)
        {
            if (values != null)
            {
                this.CustomItems.Add(key, values);
            }
        }

        /// <summary>
        /// convert to json format string
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            JObject json = new JObject();

            JObject aps = new JObject();

            if (!this.Alert.IsEmpty)
            {
                if(!string.IsNullOrEmpty(this.Alert.Body) 
                    && string.IsNullOrEmpty(this.Alert.LocalizedKey)
                    && string.IsNullOrEmpty(this.Alert.ActionLocalizedKey)
                    && (this.Alert.LocalizedArgs == null || this.Alert.LocalizedArgs.Count <= 0)
                    && !this.HideActionButton)
                {
                    aps["alert"] = new JValue(this.Alert.Body);
                }
                else
                {
                    JObject jsonAlert = new JObject();

                    if (!string.IsNullOrEmpty(this.Alert.LocalizedKey))
                        jsonAlert["loc-key"] = new JValue(this.Alert.LocalizedKey);

                    if (this.Alert.LocalizedArgs != null && this.Alert.LocalizedArgs.Count > 0)
                        jsonAlert["loc-args"] = new JArray(this.Alert.LocalizedArgs.ToArray());

                    if (!string.IsNullOrEmpty(this.Alert.Body))
                        jsonAlert["body"] = new JValue(this.Alert.Body);

                    if (this.HideActionButton)
                        jsonAlert["action-loc-key"] = new JValue((string)null);
                    else if (!string.IsNullOrEmpty(this.Alert.ActionLocalizedKey))
                        jsonAlert["action-loc-key"] = new JValue(this.Alert.ActionLocalizedKey);

                    aps["alert"] = jsonAlert;
                    
                }
            }

            if (this.Badge.HasValue)
                aps["badge"] = new JValue(this.Badge.Value);

            if (!string.IsNullOrEmpty(this.Sound))
                aps["sound"] = new JValue(this.Sound);

            if (this.ContentAvailable.HasValue)
                aps["content-available"] = new JValue(this.ContentAvailable.Value);

            json["aps"] = aps;

            foreach (string key in this.CustomItems.Keys)
            {
                if (this.CustomItems[key].Length == 1)
                    json[key] = new JValue(this.CustomItems[key][0]);
                else if (this.CustomItems[key].Length > 1)
                    json[key] = new JArray(this.CustomItems[key]);
            }

            string rawString = json.ToString(Newtonsoft.Json.Formatting.None, null);

            StringBuilder encodedString = new StringBuilder();
            foreach (char c in rawString)
            {
                if ((int)c < 32 || (int)c > 127)
                    encodedString.Append("\\u" + string.Format("{0:x4}", Convert.ToUInt32(c)));
                else
                    encodedString.Append(c);
            }

            return rawString;// encodedString.ToString();
        }

        public override string ToString()
        {
            return ToJson();
        }
    }
}
