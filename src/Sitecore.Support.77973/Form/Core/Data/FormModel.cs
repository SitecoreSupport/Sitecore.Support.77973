using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Utility;

namespace Sitecore.Support.Form.Core.Data
{
    internal class FormModel
    {
        public FormModel()
        {
        }

        public FormModel(bool analytics)
        {
            this.Analytics = analytics;
        }

        public Dictionary<string, Dictionary<string, string>> Get(string fieldId)
        {
            Func<Dictionary<string, Dictionary<string, string>>, bool> func2 = null;
            Func<Dictionary<string, Dictionary<string, string>>, bool> predicate = null;
            if (this.Fields == null)
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }
            if (predicate == null)
            {
                if (func2 == null)
                {
                    func2 = f => (f.Keys.Contains<string>("id") && f["id"].ContainsKey("v")) && (f["id"]["v"] == fieldId);
                }
                predicate = func2;
            }
            return this.Fields.FirstOrDefault<Dictionary<string, Dictionary<string, string>>>(predicate);
        }

        public string Get(string fieldId, string property)
        {
            Dictionary<string, Dictionary<string, string>> dictionary = this.Get(fieldId);
            if (((dictionary != null) && dictionary.ContainsKey(property)) && dictionary[property].ContainsKey("v"))
            {
                return dictionary[property]["v"];
            }
            return string.Empty;
        }

        public void Set(string fieldId, string property, string value)
        {
            this.Set(fieldId, property, value, null);
        }

        public void Set(string fieldId, string property, string value, string text)
        {
            Assert.ArgumentNotNullOrEmpty(property, "property");
            Dictionary<string, Dictionary<string, string>> dict = this.Get(fieldId);
            if (dict == null)
            {
                dict = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string> {
                    {
                        "v",
                        fieldId
                    }
                };
                dict.Add("id", dictionary2);
                this.Fields = this.Fields.Union<Dictionary<string, Dictionary<string, string>>>(new Dictionary<string, Dictionary<string, string>>[] { dict }).ToArray<Dictionary<string, Dictionary<string, string>>>();
            }
            Dictionary<string, string> dictionary3 = new Dictionary<string, string> {
                {
                    "v",
                    value
                }
            };
            dict.Set<string, Dictionary<string, string>>(property, dictionary3);
            if (!string.IsNullOrEmpty(text))
            {
                dict[property].Add("t", text);
            }
        }

        [DefaultValue(false), JsonProperty("analytics", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Analytics { get; set; }

        [JsonProperty("fields", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue((string)null)]
        public Dictionary<string, Dictionary<string, string>>[] Fields { get; set; }
    }
}