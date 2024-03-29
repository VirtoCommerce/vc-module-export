//Call this to register our module to main application
var moduleName = "virtoCommerce.exportModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, ['ui.grid.cellNav', 'ui.grid.edit', 'ui.grid.validate', 'angular-toArrayFilter'])
    .config(['$stateProvider', function ($stateProvider) {
        $stateProvider.state('workspace.exportModule', {
            url: '/generic-export',
            templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
            controller: ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                var newBlade = {
                    id: 'exportSettings',
                    title: 'export.blades.export-settings.title',
                    subtitle: 'export.blades.export-settings.subtitle',
                    controller: 'virtoCommerce.exportModule.exportSettingsController',
                    template: 'Modules/$(VirtoCommerce.Export)/Scripts/blades/export-settings.tpl.html',
                    exportDataRequest: {},
                    isClosingDisabled: true
                };
                bladeNavigationService.showBlade(newBlade);
            }]
        });
    }])
    .run(
        ['$http', '$compile', 'platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state', 'platformWebApp.authService'
            , function ($http, $compile, mainMenuService, widgetService, $state, authService) {
                //Register module in main menu
                var menuItem = {
                    path: 'configuration/generic-export',
                    icon: 'fa fa-database',
                    title: 'export.main-menu-title',
                    priority: 30,
                    action: function () { $state.go('workspace.exportModule'); },
                    permission: 'export:access'
                };
                mainMenuService.addMenuItem(menuItem);
            }]);
