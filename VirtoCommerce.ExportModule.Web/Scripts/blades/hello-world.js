angular.module('virtoCommerce.exportModule')
    .controller('virtoCommerce.exportModule.helloWorldController', ['$scope', 'virtoCommerce.exportModule.webApi', function ($scope, api) {
        var blade = $scope.blade;
        blade.title = 'VirtoCommerce.ExportModule';

        blade.refresh = function () {
            api.get(function (data) {
                blade.data = data.result;
                blade.isLoading = false;
            });
        };

        blade.refresh();
    }]);