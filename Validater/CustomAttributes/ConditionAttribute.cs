

namespace Validater.CustomAttributes
{
    using System;
    using Newtonsoft.Json.Linq;

    public class ConditionAttribute : Attribute
    {
        /// <summary>
        /// specified property name
        /// </summary>
        public string CheckName { get; set; }
        /// <summary>
        /// data json type
        /// </summary>
        public JTokenType JsonType { get; set; }
        /// <summary>
        /// string max length
        /// </summary>
        public int StringMaxLength { get; set; }
        /// <summary>
        /// data requirement
        /// </summary>
        public bool Required { get; set; }
    }
}
