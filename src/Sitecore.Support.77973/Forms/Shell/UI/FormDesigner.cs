namespace Sitecore.Support.Forms.Shell.UI
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Form.Core.Analytics;
    using Sitecore.Form.Core.Configuration;
    using Sitecore.Form.Core.ContentEditor.Data;
    using Sitecore.Form.Core.Utility;
    using Sitecore.Form.Core.Visual;
    using Sitecore.Forms.Core.Data;
    using Sitecore.Forms.Shell.UI;
    using Sitecore.Globalization;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.WebControls.Ribbons;
    using System;
    using System.Collections.Specialized;
    using System.Text;
    using System.Web;

    public class FormDesigner : Sitecore.Forms.Shell.UI.FormDesigner
    {
        private void AddNewField()
        {
            base.builder.AddToSetNewField();
            SheerResponse.Eval("Sitecore.FormBuilder.updateStructure(true);");
            SheerResponse.Eval("$j('#f1 input:first').trigger('focus'); $j('.v-splitter').trigger('change')");
        }

        private void AddNewField(string parent, string id, string index)
        {
            base.builder.AddToSetNewField(parent, id, int.Parse(index));
        }

        private void AddNewSection(string id, string index)
        {
            base.builder.AddToSetNewSection(id, int.Parse(index));
        }

        [HandleMessage("item:load", true)]
        private void ChangeLanguage(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!string.IsNullOrEmpty(args.Parameters["language"]) && this.CheckModified(true))
            {
                UrlString str = new UrlString(HttpUtility.UrlDecode(HttpContext.Current.Request.RawUrl.Replace("&amp;", "&")))
                {
                    ["la"] = args.Parameters["language"]
                };
                Context.ClientPage.ClientResponse.SetLocation(str.ToString());
            }
        }

        private bool CheckModified(bool checkIfActionsModified)
        {
            if (checkIfActionsModified && base.SettingsEditor.IsModifiedActions)
            {
                Context.ClientPage.Modified = true;
                base.SettingsEditor.IsModifiedActions = false;
            }
            return SheerResponse.CheckModified();
        }

        [HandleMessage("forms:editsuccess", true)]
        private void EditSuccess(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.CheckModified(false))
            {
                if (args.IsPostBack)
                {
                    if (args.HasResult)
                    {
                        NameValueCollection values = ParametersUtil.XmlToNameValueCollection(args.Result);
                        FormItem item = new FormItem(base.GetCurrentItem());
                        LinkField successPage = item.SuccessPage;
                        Item item2 = item.Database.GetItem(values["page"]);
                        if (!string.IsNullOrEmpty(values["page"]))
                        {
                            successPage.TargetID = MainUtil.GetID(values["page"], null);
                            if (item2 != null)
                            {
                                Language language;
                                if (!Language.TryParse(Sitecore.Web.WebUtil.GetQueryString("la"), out language))
                                {
                                    language = Context.Language;
                                }
                                successPage.Url = Sitecore.Form.Core.Utility.ItemUtil.GetItemUrl(item2, Sitecore.Configuration.Settings.Rendering.SiteResolving, language);
                            }
                        }
                        base.SettingsEditor.UpdateSuccess(values["message"], values["page"], successPage.Url, values["choice"] == "1");
                    }
                }
                else
                {
                    UrlString urlString = new UrlString(UIUtil.GetUri("control:SuccessForm.Editor"));
                    UrlHandle handle = new UrlHandle
                    {
                        ["message"] = base.SettingsEditor.SubmitMessage
                    };
                    if (!string.IsNullOrEmpty(base.SettingsEditor.SubmitPageID))
                    {
                        handle["page"] = base.SettingsEditor.SubmitPageID;
                    }
                    handle["choice"] = base.SettingsEditor.SuccessRedirect ? "1" : "0";
                    handle.Add(urlString);
                    Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
                    args.WaitForPostBack();
                }
            }
        }

        private void ExportToAscx()
        {
            Run.ExportToAscx(this, base.GetCurrentItem().Uri);
        }

        private static string GetUpdateTypeScript(string res, string id, string oldTypeID, string newTypeID)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Sitecore.PropertiesBuilder.changeType('");
            builder.Append(res);
            builder.Append("','");
            builder.Append(id);
            builder.Append("','");
            builder.Append(newTypeID);
            builder.Append("','");
            builder.Append(oldTypeID);
            builder.Append("')");
            return builder.ToString();
        }

        private void LoadControls()
        {
            FormItem item = new FormItem(base.GetCurrentItem());
            base.builder = new Sitecore.Support.Forms.Shell.UI.Controls.FormBuilder();
            base.builder.ID = Sitecore.Forms.Shell.UI.FormDesigner.FormBuilderID;
            base.builder.UriItem = item.Uri.ToString();
            base.FormTablePanel.Controls.Add(base.builder);
            base.FormTitle.Text = item.FormName;
            if (string.IsNullOrEmpty(base.FormTitle.Text))
            {
                base.FormTitle.Text = ResourceManager.Localize("UNTITLED_FORM");
            }
            base.TitleBorder.Controls.Add(new Literal("<input ID=\"ShowTitle\" Type=\"hidden\"/>"));
            if (!item.ShowTitle)
            {
                base.TitleBorder.Style.Add("display", "none");
            }
            base.SettingsEditor.TitleName = base.FormTitle.Text;
            base.Intro.Controls.Add(new Literal("<input ID=\"ShowIntro\" Type=\"hidden\"/>"));
            base.IntroGrid.Value = item.Introduction;
            if (string.IsNullOrEmpty(base.IntroGrid.Value))
            {
                base.IntroGrid.Value = ResourceManager.Localize("FORM_INTRO_EMPTY");
            }
            if (!item.ShowIntroduction)
            {
                base.Intro.Style.Add("display", "none");
            }
            base.IntroGrid.FieldName = item.IntroductionFieldName;
            base.SettingsEditor.FormID = base.CurrentItemID;
            base.SettingsEditor.Introduce = base.IntroGrid.Value;
            base.SettingsEditor.SaveActionsValue = item.SaveActions;
            base.SettingsEditor.CheckActionsValue = item.CheckActions;
            base.SettingsEditor.TrackingXml = item.Tracking.ToString();
            base.SettingsEditor.SuccessRedirect = item.SuccessRedirect;
            if (item.SuccessPage.TargetItem != null)
            {
                Language language;
                if (!Language.TryParse(Sitecore.Web.WebUtil.GetQueryString("la"), out language))
                {
                    language = Context.Language;
                }
                base.SettingsEditor.SubmitPage = Sitecore.Form.Core.Utility.ItemUtil.GetItemUrl(item.SuccessPage.TargetItem, Sitecore.Configuration.Settings.Rendering.SiteResolving, language);
            }
            else
            {
                base.SettingsEditor.SubmitPage = item.SuccessPage.Url;
            }
            if (!ID.IsNullOrEmpty(item.SuccessPageID))
            {
                base.SettingsEditor.SubmitPageID = item.SuccessPageID.ToString();
            }
            base.Footer.Controls.Add(new Literal("<input ID=\"ShowFooter\" Type=\"hidden\"/>"));
            base.FooterGrid.Value = item.Footer;
            if (string.IsNullOrEmpty(base.FooterGrid.Value))
            {
                base.FooterGrid.Value = ResourceManager.Localize("FORM_FOOTER_EMPTY");
            }
            if (!item.ShowFooter)
            {
                base.Footer.Style.Add("display", "none");
            }
            base.FooterGrid.FieldName = item.FooterFieldName;
            base.SettingsEditor.Footer = base.FooterGrid.Value;
            base.SettingsEditor.SubmitMessage = item.SuccessMessage;
            string submitName = item.SubmitName;
            if (string.IsNullOrEmpty(submitName))
            {
                submitName = ResourceManager.Localize("NO_BUTTON_NAME");
                base.FormSubmit.Attributes["value"] = submitName;
            }
            else
            {
                base.FormSubmit.Attributes["value"] = submitName;
            }
            base.SettingsEditor.SubmitName = submitName;
            this.UpdateRibbon();
        }

        private void LoadPropertyEditor(string typeID, string id)
        {
            Item currentItem = base.GetCurrentItem();
            Item item = currentItem.Database.GetItem(typeID);
            if (!string.IsNullOrEmpty(typeID))
            {
                try
                {
                    string str = PropertiesFactory.RenderPropertiesSection(item, Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeAssemblyID, Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeClassID);
                    Tracking tracking = new Tracking(base.SettingsEditor.TrackingXml, currentItem.Database);
                    if ((!Sitecore.Form.Core.Configuration.Settings.Analytics.IsAnalyticsAvailable || tracking.Ignore) || (item["Deny Tag"] == "1"))
                    {
                        str = str + "<input id='denytag' type='hidden'/>";
                    }
                    if (!string.IsNullOrEmpty(str))
                    {
                        base.SettingsEditor.PropertyEditor = str;
                    }
                }
                catch
                {
                }
            }
            else if (id == "Welcome")
            {
                base.SettingsEditor.ShowEmptyForm();
            }
        }

        private void Localize()
        {
            base.FormTitle.Text = ResourceManager.Localize("TITLE_CAPTION");
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
                this.Localize();
                this.BuildUpClientDictionary();
                if (string.IsNullOrEmpty(Registry.GetString("/Current_User/VSplitters/FormsSpliter")))
                {
                    Registry.SetString("/Current_User/VSplitters/FormsSpliter", "412,");
                }
                this.LoadControls();
                if (base.builder.IsEmpty)
                {
                    base.SettingsEditor.ShowEmptyForm();
                }
            }
            else
            {
                base.builder = base.FormTablePanel.FindControl(Sitecore.Forms.Shell.UI.FormDesigner.FormBuilderID) as Sitecore.Support.Forms.Shell.UI.Controls.FormBuilder;
                base.builder.UriItem = base.GetCurrentItem().Uri.ToString();
            }
        }

        [HandleMessage("forms:addaction", true)]
        private void OpenSetSubmitActions(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.CheckModified(false))
            {
                if (args.IsPostBack)
                {
                    UrlString urlString = new UrlString(args.Parameters["url"]);
                    UrlHandle handle = UrlHandle.Get(urlString);
                    base.SettingsEditor.TrackingXml = handle["tracking"];
                    base.SettingsEditor.FormID = base.CurrentItemID;
                    if (args.HasResult)
                    {
                        ListDefinition definition = ListDefinition.Parse((args.Result == "-") ? string.Empty : args.Result);
                        base.SettingsEditor.UpdateCommands(definition, base.builder.FormStucture.ToXml(), args.Parameters["mode"] == "save");
                    }
                }
                else
                {
                    string name = ID.NewID.ToString();
                    HttpContext.Current.Session.Add(name, (args.Parameters["mode"] == "save") ? base.SettingsEditor.SaveActions : base.SettingsEditor.CheckActions);
                    UrlString str3 = new UrlString(UIUtil.GetUri("control:SubmitCommands.Editor"));
                    str3.Append("definition", name);
                    str3.Append("db", base.GetCurrentItem().Database.Name);
                    str3.Append("id", base.CurrentItemID);
                    str3.Append("la", base.CurrentLanguage.Name);
                    str3.Append("root", args.Parameters["root"]);
                    str3.Append("system", args.Parameters["system"] ?? string.Empty);
                    args.Parameters.Add("params", name);
                    new UrlHandle
                    {
                        ["title"] = ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SELECT_SAVE_TITLE" : "SELECT_CHECK_TITLE"),
                        ["desc"] = ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SELECT_SAVE_DESC" : "SELECT_CHECK_DESC"),
                        ["actions"] = ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SAVE_ACTIONS" : "CHECK_ACTIONS"),
                        ["addedactions"] = ResourceManager.Localize((args.Parameters["mode"] == "save") ? "ADDED_SAVE_ACTIONS" : "ADDED_CHECK_ACTIONS"),
                        ["tracking"] = base.SettingsEditor.TrackingXml,
                        ["structure"] = base.builder.FormStucture.ToXml()
                    }.Add(str3);
                    args.Parameters["url"] = str3.ToString();
                    Context.ClientPage.ClientResponse.ShowModalDialog(str3.ToString(), true);
                    args.WaitForPostBack();
                }
            }
        }

        private void Save(bool refresh)
        {
            FormItem.UpdateFormItem(base.GetCurrentItem().Database, base.CurrentLanguage, base.builder.FormStucture);
            this.SaveFormsText();
            Context.ClientPage.Modified = false;
            if (refresh)
            {
                base.Refresh(string.Empty);
            }
        }

        private void SaveFormsText()
        {
            Item currentItem = base.GetCurrentItem();
            FormItem item2 = new FormItem(currentItem);
            currentItem.Editing.BeginEdit();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID].Value = base.SettingsEditor.TitleName;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value = Context.ClientPage.ClientRequest.Form["ShowTitle"];
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormIntroductionID].Value = base.SettingsEditor.Introduce;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormIntroID].Value = Context.ClientPage.ClientRequest.Form["ShowIntro"];
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormFooterID].Value = base.SettingsEditor.Footer;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormFooterID].Value = Context.ClientPage.ClientRequest.Form["ShowFooter"];
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormSubmitID].Value = (base.SettingsEditor.SubmitName == string.Empty) ? ResourceManager.Localize("NO_BUTTON_NAME") : base.SettingsEditor.SubmitName;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value = base.SettingsEditor.SaveActions.ToXml();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.CheckActionsID].Value = base.SettingsEditor.CheckActions.ToXml();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessMessageID].Value = base.SettingsEditor.SubmitMessage;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessModeID].Value = base.SettingsEditor.SuccessRedirect ? "{F4D50806-6B89-4F2D-89FE-F77FC0A07D48}" : "{3B8369A0-CC1A-4E9A-A3DB-7B086379C53B}";
            LinkField successPage = item2.SuccessPage;
            successPage.TargetID = MainUtil.GetID(base.SettingsEditor.SubmitPageID, ID.Null);
            if (successPage.TargetItem != null)
            {
                successPage.Url = successPage.TargetItem.Paths.Path;
            }
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessPageID].Value = successPage.Xml.OuterXml;
            if (currentItem.Fields["__Tracking"] != null)
            {
                currentItem.Fields["__Tracking"].Value = base.SettingsEditor.TrackingXml;
            }
            currentItem.Editing.EndEdit();
        }

        [HandleMessage("item:save", true)]
        private void SaveMessage(ClientPipelineArgs args)
        {
            this.SaveFormStructure(true, null);
            SheerResponse.Eval("Sitecore.FormBuilder.updateStructure(true);");
        }

        [HandleMessage("item:selectlanguage", true)]
        private void SelectLanguage(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Run.SetLanguage(this, base.GetCurrentItem().Uri);
        }

        private void UpdateRibbon()
        {
            Item currentItem = base.GetCurrentItem();
            Ribbon ctl = new Ribbon
            {
                ID = "FormDesigneRibbon",
                CommandContext = new CommandContext(currentItem)
            };
            Item item = Context.Database.GetItem(Sitecore.Forms.Shell.UI.FormDesigner.RibbonPath);
            Error.AssertItemFound(item, Sitecore.Forms.Shell.UI.FormDesigner.RibbonPath);
            bool flag = !string.IsNullOrEmpty(base.SettingsEditor.TitleName);
            ctl.CommandContext.Parameters.Add("title", flag.ToString());
            bool flag2 = !string.IsNullOrEmpty(base.SettingsEditor.Introduce);
            ctl.CommandContext.Parameters.Add("intro", flag2.ToString());
            bool flag3 = !string.IsNullOrEmpty(base.SettingsEditor.Footer);
            ctl.CommandContext.Parameters.Add("footer", flag3.ToString());
            ctl.CommandContext.Parameters.Add("id", currentItem.ID.ToString());
            ctl.CommandContext.Parameters.Add("la", currentItem.Language.Name);
            ctl.CommandContext.Parameters.Add("vs", currentItem.Version.Number.ToString());
            ctl.CommandContext.Parameters.Add("db", currentItem.Database.Name);
            ctl.CommandContext.RibbonSourceUri = item.Uri;
            ctl.ShowContextualTabs = false;
            base.RibbonPanel.InnerHtml = Sitecore.Web.HtmlUtil.RenderControl(ctl);
        }

        private void UpdateSubmit()
        {
            base.SettingsEditor.FormID = base.CurrentItemID;
            base.SettingsEditor.UpdateCommands(base.SettingsEditor.SaveActions, base.builder.FormStucture.ToXml(), true);
        }

        private void UpgradeToSection(string parent, string id)
        {
            base.builder.UpgradeToSection(id);
        }

        [HandleMessage("forms:validatetext", true)]
        private void ValidateText(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                base.SettingsEditor.Validate(args.Parameters["ctrl"]);
            }
        }

        private void WarningEmptyForm()
        {
            base.builder.ShowEmptyForm();
            Control control = base.SettingsEditor.ShowEmptyForm();
            Context.ClientPage.ClientResponse.SetOuterHtml(control.ID, control);
        }
    }
}