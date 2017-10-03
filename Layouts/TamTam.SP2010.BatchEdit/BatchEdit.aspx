<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls"
    Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BatchEdit.aspx.cs" Inherits="TamTam.SP2010.BatchEdit.Layouts.TamTam.SP2010.BatchEdit.BatchEdit"
    DynamicMasterPageFile="~masterurl/default.master" %>

<%@ Register TagPrefix="wssuc" TagName="InputFormSection" Src="/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" Src="/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" Src="/_controltemplates/ButtonSection.ascx" %>

<asp:content id="PageHead" contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">
<script type="text/javascript">
    function BtnBatchEditCancel_Click() {
        SP.UI.ModalDialog.commonModalDialogClose(SP.UI.DialogResult.cancel, 'Operation Cancelled, no values saved.');
    } 
</script>
</asp:content>

<asp:content id="Main" contentplaceholderid="PlaceHolderMain" runat="server">
    <wssuc:InputFormSection Title="Field values" runat="server"> 
        <Template_Description> 
          <SharePoint:EncodedLiteral ID="EncodedLiteral1" runat="server" 
                    text="Set field values, all provided values will be used to overwrite existing ones. " EncodeMethod='HtmlEncode'/> <br />
          <asp:CheckBox ID="TaxonomyAppending" runat="server" Text="Allow appending enterprise keywords for multi-value fields" Checked="True"/>
        </Template_Description> 
        
        <Template_InputFormControls> 
          <wssuc:InputFormControl runat="server"> 
            <Template_Control>
                <asp:Panel ID="pnlFields" runat="server"></asp:Panel>
            </Template_Control> 
          </wssuc:InputFormControl> 
        </Template_InputFormControls> 
      </wssuc:InputFormSection> 
    <wssuc:ButtonSection runat="server" ShowStandardCancelButton="False"> 
        <Template_Buttons> 
          <asp:placeholder ID="Placeholder1" runat="server"> 
              <asp:Button CssClass="ms-ButtonHeightWidth" ID="btnOk" runat="server" text="Ok" />
                <SeparatorHtml> 
                    <span id="idSpace" class="ms-SpaceBetButtons" /> 
                </SeparatorHtml> 
              <input class="ms-ButtonHeightWidth" type="button" name="BtnCancel" id="Button2" 
                                value="Cancel" onclick="BtnBatchEditCancel_Click()" />            
          </asp:PlaceHolder> 
        </Template_Buttons> 
      </wssuc:ButtonSection> 

       <asp:Label ID="lblMessages" runat="server" style="color: #ff0000; display: block;"></asp:Label>
</asp:content>

<asp:content id="PageTitle" contentplaceholderid="PlaceHolderPageTitle" runat="server">
Tam Tam Batch Edit items
</asp:content>
<asp:content id="PageTitleInTitleArea" contentplaceholderid="PlaceHolderPageTitleInTitleArea"
    runat="server">
Tam Tam Batch Edit items
</asp:content>
