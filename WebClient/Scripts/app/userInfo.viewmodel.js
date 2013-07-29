function UserInfoViewModel(app, name, dataModel) {
    var self = this;

    // Data
    self.name = ko.observable(name);

    // Operations
    self.logOff = function () {
        app.navigateToLoggedOff();
    };

    self.manage = function () {
        app.navigateToManage();
    };
}
