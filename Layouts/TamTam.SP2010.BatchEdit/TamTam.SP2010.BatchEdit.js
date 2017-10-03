Type.registerNamespace("TamTam.SP2010.BatchEdit");

TamTam.SP2010.BatchEdit.BatchEdit = function () {
    var items = SP.ListOperation.Selection.getSelectedItems();
    this.selectedItems = '';
    var k;

    for (k in items) {
        this.selectedItems += '|' + items[k].id;
    }
    
    var ctx = SP.ClientContext.get_current();
    
    this.site = ctx.get_site();
    ctx.load(this.site);

    this.web = ctx.get_web();
    ctx.load(this.web);

    ctx.executeQueryAsync(Function.createDelegate(this, TamTam.SP2010.BatchEdit.onQuerySucceeded), Function.createDelegate(this, TamTam.SP2010.BatchEdit.onQueryFailed));
};
TamTam.SP2010.BatchEdit.onQuerySucceeded = function () {
    var webUrl = this.web.get_serverRelativeUrl();
    if (webUrl === "/") {
        webUrl = "";
    }

    var options = {
        url: webUrl + '/_layouts/TamTam.SP2010.BatchEdit/BatchEdit.aspx?items=' + this.selectedItems + "&web=" + this.web.get_id() + '&source=' + SP.ListOperation.Selection.getSelectedList(),
        title: 'Batch Edit Items',
        allowMaximize: false,
        showClose: false,
        width: 640,
        height: 480,
        dialogReturnValueCallback: TamTam.SP2010.BatchEdit.ConvertCallback
    };

    SP.UI.ModalDialog.showModalDialog(options);
};
// We could add notifications here if anything fails
TamTam.SP2010.BatchEdit.onQueryFailed = function () {};
TamTam.SP2010.BatchEdit.ConvertCallback = function (result, target) {
    SP.UI.Notify.addNotification(target, false);

    SP.UI.ModalDialog.RefreshPage(result);
};