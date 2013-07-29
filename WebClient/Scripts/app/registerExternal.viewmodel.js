function RegisterExternalViewModel(app, dataModel) {
    // Private state
    var self = this;

    // Data
    self.loginProvider = ko.observable();
    self.userName = ko.observable(null);
    self.externalAccessToken = ko.observable(null);

    // Other UI state
    self.errors = ko.observableArray();
    self.registering = ko.observable(false);

    // data-bind click
    self.register = function () {
        self.errors.removeAll();
        self.registering(true);
        dataModel.registerExternal(self.externalAccessToken(), {
            userName: self.userName()
        }).done(function (data) {
            self.registering(false);

            if (data.userName && data.access_token) {
                app.navigateToLoggedIn(data.userName, data.access_token, false);
            } else {
                self.errors.push("An unknown error occurred.");
            }
        }).failJSON(function (data) {
            var errors;

            self.registering(false);
            errors = dataModel.toErrorsArray(data);

            if (errors) {
                self.errors(errors);
            } else {
                self.errors.push("An unknown error occurred.");
            }
        });
    };
}
