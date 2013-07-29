function RegisterViewModel(app, dataModel) {
    var self = this;

    // Data
    self.userName = ko.observable("");
    self.password = ko.observable("");
    self.confirmPassword = ko.observable("");

    // Other UI state
    self.errors = ko.observableArray();
    self.registering = ko.observable(false);

    // Operations
    self.register = function () {
        self.errors.removeAll();
        self.registering(true);
        dataModel.register({
            userName: self.userName(),
            password: self.password(),
            confirmPassword: self.confirmPassword()
        }).done(function (data) {
            self.registering(false);

            if (data.errors) {
                self.errors(data.errors);
            } else if (data.userName && data.access_token) {
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

    self.login = function () {
        app.navigateToLogin();
    };
}
