using System;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.Globalization;
using System.Web;
using Microsoft.SharePoint.Taxonomy;
using Microsoft.SharePoint.Utilities;

// ReSharper disable CheckNamespace
namespace TamTam.SP2010.BatchEdit.Layouts.TamTam.SP2010.BatchEdit
{
    // ReSharper restore CheckNamespace
    public partial class BatchEdit : LayoutsPageBase
    {

        private List<KeyValuePair<Guid, string>> _fieldValues;
        private string _messages = string.Empty;

        /// <summary>
        /// OnInit
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            btnOk.Click += btnOk_Click;
            btnOk.CausesValidation = false;
        }

        /// <summary>
        /// PageLoad
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                lblMessages.Text = string.Empty;
            }
        }

        /// <summary>
        /// Default CreateChild controls
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            LoadForm();
        }

        /// <summary>
        /// Generates the form containing all the fields
        /// </summary>
        private void LoadForm()
        {
            try
            {
                using (new SPMonitoredScope("SP2010 BatchEdit Get Fields (and default values)"))
                {
                    string webId = Request["web"];
                    string listId = Request["source"];

                    using (SPWeb web = SPContext.Current.Site.OpenWeb(new Guid(webId)))
                    {
                        SPList list = web.Lists[new Guid(listId)];

                        foreach (SPField field in list.Fields)
                        {
                            if (field.ShowInNewForm.GetValueOrDefault(true) && field.FieldRenderingControl != null && (field.ShowInEditForm ?? field.CanBeDisplayedInEditForm))
                            {
                                string lblText = field.Title;
                                if (field.Required) { lblText += " *"; }

                                pnlFields.Controls.Add(new System.Web.UI.WebControls.Label { Text = lblText });
                                pnlFields.Controls.Add(new Literal { Text = "<br/>" });

                                try
                                {
                                    if (!field.FieldRenderingControl.GetType().Equals(typeof(TaxonomyFieldControl)))
                                    {
                                        BaseFieldControl editControl = field.FieldRenderingControl;
                                        editControl.ID = "fld_" + field.Id.ToString().Replace("-", "_"); // fix for Lookup picker
                                        Trace.Write(field.Id.ToString());
                                        editControl.ControlMode = SPControlMode.New;
                                        editControl.ListId = list.ID;
                                        editControl.FieldName = field.InternalName;
                                        editControl.RenderContext = SPContext.GetContext(HttpContext.Current, list.DefaultView.ID, list.ID, web);

                                        pnlFields.Controls.Add(editControl);
                                    }

                                    else
                                    {
                                        var session = new TaxonomySession(field.ParentList.ParentWeb.Site);
                                        
                                        var taxonomyControl = new TaxonomyWebTaggingControl
                                            {
                                                IsMulti = true,
                                                IsAddTerms = true,
                                                ID = "fld_" + field.Id,
                                                FieldName = field.Title,
                                                FieldId = field.Id.ToString()
                                            };

                                        taxonomyControl.TermSetId.Add(session.TermStores[0].Id);
                                        taxonomyControl.SSPList = ((TaxonomyField)field).SspId.ToString();
                                        taxonomyControl.AnchorId = ((TaxonomyField)field).AnchorId;
                                        taxonomyControl.TermSetList = ((TaxonomyField)field).TermSetId.ToString();
                                        pnlFields.Controls.Add(taxonomyControl);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SPCriticalTraceCounter.AddDataToScope(66, "SP2010 BatchEdit", 1, ex.Message + ": " + ex.StackTrace);
                                }

                                pnlFields.Controls.Add(new Literal { Text = "<br/><br/>" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SPCriticalTraceCounter.AddDataToScope(66, "SP2010 BatchEdit", 1, ex.Message + ": " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Button ClickEvent will cause a PostBack and will try to save all values to corresponding 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnOk_Click(object sender, EventArgs e)
        {
            using (new SPMonitoredScope("SP2010 BatchEdit Process Field Values"))
            {
                try
                {
                    _fieldValues = new List<KeyValuePair<Guid, string>>();
                    foreach (Control control in pnlFields.Controls)
                    {
                        if (control.GetType().Equals(typeof(DateTimeField)))
                        {
                            BaseFieldControl taxControl = (DateTimeField)control;
                            if (taxControl.Value != null)
                            {
                                _fieldValues.Add(new KeyValuePair<Guid, string>(taxControl.Field.Id, taxControl.Value.ToString()));
                            }
                        }
                        else
                        {
                            var taggingControl = control as TaxonomyWebTaggingControl;
                            if (taggingControl != null)
                            {
                                TaxonomyWebTaggingControl taxControl = taggingControl;
                                if (!string.IsNullOrEmpty(taxControl.Text))
                                {
                                    _fieldValues.Add(new KeyValuePair<Guid, string>(new Guid(taxControl.FieldId), taxControl.Text));
                                }
                            }
                            else if (control.GetType().IsSubclassOf(typeof(BaseFieldControl)))
                            {
                                BaseFieldControl fieldControl = (BaseFieldControl)control;
                                if (!string.IsNullOrEmpty(fieldControl.Value as string))
                                {
                                    _fieldValues.Add(new KeyValuePair<Guid, string>(fieldControl.Field.Id, fieldControl.Value as string));
                                }
                            }
                        }
                    }

                    string webId = Request["web"];
                    string listId = Request["source"];
                    string items = Request["items"];

                    using (SPWeb web = SPContext.Current.Site.OpenWeb(new Guid(webId)))
                    {
                        SPList sourceLibrary = web.Lists[new Guid(listId)];

                        string[] itemids = items.Trim('|').Split('|');

                        foreach (string itemid in itemids)
                        {

                            try
                            {
                                SPListItem item = sourceLibrary.GetItemById(int.Parse(itemid));

                                if (item.Folder != null)
                                {
                                    TraverseListFolder(item.Folder);
                                }
                                else
                                {
                                    UpdateListItem(item);
                                }

                            }
                            catch (Exception ex)
                            {
                                _messages += ex.Message + "<br/>";
                                SPCriticalTraceCounter.AddDataToScope(67, "SP2010 BatchEdit", 1, ex.Message + ": " + ex.StackTrace);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _messages += ex.Message + "<br/>";
                    SPCriticalTraceCounter.AddDataToScope(68, "SP2010 BatchEdit", 1, ex.Message + ": " + ex.StackTrace);
                }
            }

            if (string.IsNullOrEmpty(_messages))
            {
                Page.Response.Clear();
                Page.Response.Write(string.Format(CultureInfo.InvariantCulture,
                            "<script type=\"text/javascript\">window.frameElement.commonModalDialogClose(1, 'Items updated');</script>"));
                Page.Response.End();
            }
            else
            {
                // Log all messages again. (we could also use this 
                SPCriticalTraceCounter.AddDataToScope(67, "SP2010 BatchEdit", 1, _messages);

                lblMessages.Text = "Unfortunately there was an error updating the items with the provided values, a system administrator can use the message below to check upon this error, please provide them with it. <br />" + _messages;
            }
        }

        /// <summary>
        /// Updates a single ListItem, making sure only to update fields in the cType
        /// </summary>
        /// <param name="item"></param>
        private void UpdateListItem(SPListItem item)
        {
            try
            {
                SPContentType ctId = item.ContentType;

                foreach (KeyValuePair<Guid, string> keyValue in _fieldValues.Where(keyValue => ctId.Fields.Contains(keyValue.Key)))
                {
                    if (item.Fields[keyValue.Key].TypeAsString.StartsWith("TaxonomyFieldType", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var field = item.Fields[keyValue.Key] as TaxonomyField;
                        var fieldValue = item[keyValue.Key] != null ? item[keyValue.Key].ToString() : "";
                        var newValues = keyValue.Value;

                        if (field != null)
                        {
                            if (field.AllowMultipleValues)
                            {
                                var values = new TaxonomyFieldValueCollection(field);

                                if (TaxonomyAppending.Checked)
                                {
                                    values.PopulateFromLabelGuidPairs(fieldValue);
                                }

                                values.PopulateFromLabelGuidPairs(newValues);
                                field.SetFieldValue(item, values);
                            }
                            else
                            {
                                var taxValue = new TaxonomyFieldValue(field);
                                taxValue.PopulateFromLabelGuidPair(newValues);
                                field.SetFieldValue(item, taxValue);
                            }
                        }
                    }
                    else if (item.Fields[keyValue.Key].TypeAsString.StartsWith("DateTime", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var field = item.Fields[keyValue.Key] as SPFieldDateTime;
                        if (field != null)
                        {
                            CultureInfo ci = new CultureInfo(Convert.ToInt32(SPContext.Current.Web.CurrencyLocaleID));
                            DateTime fldValue = Convert.ToDateTime(keyValue.Value);
                            field.ParseAndSetValue(item, fldValue.ToString(ci.DateTimeFormat.ShortDatePattern, ci));
                        }
                    }
                    else
                    {
                        item[keyValue.Key] = keyValue.Value;
                    }
                }

                item.Update();
            }
            catch (Exception ex)
            {
                _messages += ex.Message + "<br/>";
                SPCriticalTraceCounter.AddDataToScope(67, "SP2010 BatchEdit", 1, ex.Message + ": " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Loops through folder and updates items in there
        /// </summary>
        /// <param name="folder"></param>
        private void TraverseListFolder(SPFolder folder)
        {
            // Get the collection of items from this folder
            var qry = new SPQuery { Folder = folder };

            try
            {
                using (SPWeb web = folder.ParentWeb)
                {
                    SPListItemCollection ic = web.Lists[folder.ParentListId].GetItems(qry);

                    foreach (SPListItem subitem in ic)
                    {
                        if (subitem.Folder != null)
                        {
                            TraverseListFolder(subitem.Folder);
                        }
                        else
                        {
                            UpdateListItem(subitem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SPCriticalTraceCounter.AddDataToScope(67, "SP2010 BatchEdit", 1, ex.Message + ": " + ex.StackTrace);
            }
        }
    }
}