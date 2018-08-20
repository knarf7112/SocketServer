using System;
using System.Collections.Generic;
using System.Linq;

namespace Validater.Converters
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Reflection;
    using Validater.CustomAttributes;

    public class ValidationConverter : JsonConverter
    {
        public List<string> ErrorList { get; private set; }
        public override bool CanConvert(Type objectType)
        {
            //Console.WriteLine("Can Convert: true"); //1.
            return true;
        }
        //委派檢驗方法
        public Action<JProperty, ConditionAttribute> DeleValidation { private get; set; }

        public Action<List<string>> ShowError { private get; set; }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            ErrorList = new List<string>();
            if (reader.TokenType == JsonToken.Null)
                return null;


            JObject root = JToken.Load(reader) as JObject;

            if (DeleValidation == null)
            {
                DeleValidation = DefaultValidateAction;
            }

            WalkNode(root, objectType, DefaultValidateAction);

            if (ShowError != null)
            {
                ShowError.Invoke(ErrorList);
            }

            return root.ToObject(objectType);
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject obj = JToken.FromObject(value) as JObject;
            obj.WriteTo(writer);
        }

        // Recursive All JsonNode and Models properties
        private void WalkNode(JToken token, Type obejctTyoe, Action<JProperty, ConditionAttribute> action)
        {
            if (token.Type == JTokenType.Object)
            {
                List<PropertyInfo> props = null;
                // 1.取得此層要映射的物件Prperties,若為泛型List<T>,則取得T的Prperties
                if (obejctTyoe.IsGenericType && obejctTyoe.GetGenericTypeDefinition() == typeof(List<>))
                {
                    props = obejctTyoe.GetGenericArguments()[0].GetProperties().ToList();
                }
                else
                {
                    props = obejctTyoe.GetProperties().ToList();
                }

                // 2.遍歷此層的json node並搜尋propertInfo內的自定屬性 MyAttribute 並確認條件
                foreach (JProperty child in token.Children<JProperty>())
                {
                    ConditionAttribute myAttr = null;
                    Type genericType = null;
                    PropertyInfo hitProp = null;
                    Predicate<PropertyInfo> task = (prop) =>
                    {
                        var attr = prop.GetCustomAttribute<ConditionAttribute>(false);
                        if (attr != null && attr.CheckName.Equals(child.Name, StringComparison.CurrentCultureIgnoreCase))
                        {
                            hitProp = prop;
                            genericType = prop.PropertyType;
                            myAttr = attr;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    };

                    if (props.Exists(task))
                    {
                        props.Remove(hitProp);
                        action(child, myAttr);//委派執行錯誤檢驗
                    }
                    // T have MyAttribute(未設置自定Attribute就不再繼續往其內層node檢驗)
                    if (myAttr != null && (child.Value.Type == JTokenType.Object || child.Value.Type == JTokenType.Array))
                    {
                        WalkNode(child.Value, genericType, action);
                    }
                }


                // 3.取出剩餘properties且為Required的並設定錯誤訊息
                props.ForEach(prop =>
                {
                    var attr = prop.GetCustomAttribute<ConditionAttribute>(false);
                    if (attr != null && attr.Required)
                    {
                        ErrorList.Add(String.Format("property:({0}) is required,but not find", attr.CheckName));
                    }
                });
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken child in token.Children())
                {
                    WalkNode(child, obejctTyoe, action);
                }
            }
        }

        // default validation
        private void DefaultValidateAction(JProperty jprop, ConditionAttribute attr)
        {
            string propertyName = attr.CheckName;
            // 檢查設定的名稱與Json的是否一致
            if (!propertyName.Equals(jprop.Name))
            {
                ErrorList.Add(String.Format("property:({0}) [Name] expected: [{0}], actual is [{1}]", propertyName, jprop.Name));
            }
            // 檢查(非預設值且必要)設定的資料型別與json的值型別是否一致
            if (attr.JsonType != JTokenType.None && attr.Required && jprop.Value.Type != attr.JsonType)
            {
                ErrorList.Add(String.Format("property:({0}) [Type] expected: {1}, actual is {2}", propertyName, attr.JsonType.ToString(), jprop.Value.Type.ToString()));
            }
            // (若為字串型別)檢查內容字串的長度與設定是否一致
            if (attr.JsonType == JTokenType.String && attr.StringMaxLength != 0)
            {
                var val = jprop.Value.Value<string>();
                if (val != null && val.Length > attr.StringMaxLength)
                {
                    ErrorList.Add(String.Format("property:({0}) [StringMaxLength] expected: {1}, actual is {2}", propertyName, attr.StringMaxLength, val.Length));
                }
            }
        }
    }
}
