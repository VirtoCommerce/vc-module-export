angular.module('virtoCommerce.exportModule')
.controller('virtoCommerce.exportModule.legacy.mainController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.authService', function ($scope, bladeNavigationService, authService) {

    $scope.export = function () {
        $scope.selectedNodeId = 'export';

        var newBlade = {
            controller: 'virtoCommerce.exportModule.legacy.exportMainController',
            template: 'Modules/$(VirtoCommerce.Export)/Scripts/legacy/blades/export-main.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, $scope.blade);
    };

    $scope.import = function () {
        if (authService.checkPermission('platform:exportImport:import')) {
            $scope.selectedNodeId = 'import';

            var newBlade = {
                controller: 'virtoCommerce.exportModule.legacy.importMainController',
                template: 'Modules/$(VirtoCommerce.Export)/Scripts/legacy/blades/import-main.tpl.html'
            };
            bladeNavigationService.showBlade(newBlade, $scope.blade);
        }
    };

    $scope.blade.headIcon = 'fa fa-database';
    $scope.blade.isLoading = false;
}]);
