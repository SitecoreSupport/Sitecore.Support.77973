namespace Sitecore.Support.Forms.Shell.UI.Controls
{
    using Sitecore;
    using Sitecore.Diagnostics;
    using Sitecore.Form.Core.Configuration;
    using Sitecore.Form.Core.Data;
    using Sitecore.Form.Core.Web;
    using Sitecore.Form.Web.UI.Controls;
    using Sitecore.Forms.Core.Rules;
    using Sitecore.Forms.Shell.UI.Controls;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using System;
    using System.Collections.Generic;
    using System.Web.UI;

    public class FormBuilder : Sitecore.Forms.Shell.UI.Controls.FormBuilder
    {
        internal Sitecore.Support.Form.Core.Data.FormModel GetFormModel(bool saved)
        {
            Sitecore.Support.Form.Core.Data.FormModel model;
            if (!saved && Sitecore.Context.ClientPage.IsEvent)
            {
                Json.Instance.TryDeserializeObject<Sitecore.Support.Form.Core.Data.FormModel>(WebUtil.GetFormValue("form"), out model);
                return model;
            }
            model = new Sitecore.Support.Form.Core.Data.FormModel(Settings.Analytics.IsAnalyticsAvailable);
            List<Dictionary<string, Dictionary<string, string>>> list = new List<Dictionary<string, Dictionary<string, string>>>();
            foreach (SectionDefinition definition in base.Form.Sections)
            {
                Dictionary<string, Dictionary<string, string>> properties = this.GetProperties(definition.ClientControlID, definition.Conditions);
                if (properties != null)
                {
                    list.Add(properties);
                }
                foreach (FieldDefinition definition2 in definition.Fields)
                {
                    properties = this.GetProperties(definition2.ClientControlID, definition2.Conditions);
                    if (properties != null)
                    {
                        list.Add(properties);
                    }
                }
            }
            model.Fields = list.ToArray();
            return model;
        }

        private Dictionary<string, Dictionary<string, string>> GetProperties(string id, string conditions)
        {
            if (!string.IsNullOrEmpty(conditions) && (conditions != "<ruleset />"))
            {
                conditions = conditions.Replace("&", "$");
                Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string> {
                    {
                        "v",
                        id
                    }
                };
                dictionary.Add("id", dictionary2);
                Dictionary<string, string> dictionary3 = new Dictionary<string, string> {
                    {
                        "v",
                        conditions
                    },
                    {
                        "t",
                        RuleRenderer.Render(conditions)
                    }
                };
                dictionary.Add("Conditions", dictionary3);
                return dictionary;
            }
            return null;
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            (this.Controls.FirstOrDefault(x => (x is Border)).Controls[0] as Literal).Text = "<input ID=\"form\" Type=\"hidden\" value='" + ObjectExtensions.ToJson(this.GetFormModel(true)) + "'/>";
        }
    }
}