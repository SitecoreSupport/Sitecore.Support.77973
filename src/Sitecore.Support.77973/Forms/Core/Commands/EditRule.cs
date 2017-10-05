namespace Sitecore.Support.Forms.Core.Commands
{
    using System;
    using System.Collections.Specialized;
    using Sitecore.Form.Core.Web;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web;
    using Sitecore.Web.UI.Sheer;

    [Serializable]
    public class EditRule : Sitecore.Forms.Core.Commands.EditRule
    {
        public override void Execute(CommandContext context)
        {
            string fieldName = context.Parameters["rule"];
            Sitecore.Support.Form.Core.Data.FormModel t = null;
            if (Json.Instance.TryDeserializeObject<Sitecore.Support.Form.Core.Data.FormModel>(WebUtil.GetFormValue(fieldName),
                out t))
            {
                NameValueCollection parameters = new NameValueCollection
                {
                    ["rule"] = fieldName,
                    ["form"] = WebUtil.GetFormValue(fieldName),
                    ["id"] = context.Parameters["id"],
                    ["cid"] = context.Parameters["cid"],
                    ["ruletext"] = t.Get(context.Parameters["id"], "Conditions").Replace("$", "&")
                };
                ClientPipelineArgs args = new ClientPipelineArgs(parameters);
                Context.ClientPage.Start(this, "Run", args);
            }
        }
    }
}