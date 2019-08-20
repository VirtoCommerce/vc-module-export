// Call this to register your module to main application
var moduleName = "virtoCommerce.exportModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider', '$urlRouterProvider',
        function ($stateProvider, $urlRouterProvider) {
            $stateProvider
                .state('workspace.virtoCommerceExportModuleState', {
                    url: '/virtoCommerce.exportModule',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        '$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                            var newBlade = {
                                id: 'blade1',
                                controller: 'virtoCommerce.exportModule.helloWorldController',
                                template: 'Modules/$(virtoCommerce.exportModule)/Scripts/blades/hello-world.html',
                                isClosingDisabled: true
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['$rootScope', 'platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state',
        function ($rootScope, mainMenuService, widgetService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/virtoCommerce.exportModule',
                icon: 'fa fa-cube',
                title: 'VirtoCommerce.ExportModule',
                priority: 100,
                action: function () { $state.go('workspace.virtoCommerceExportModuleState'); },
                permission: 'virtoCommerce.exportModule.WebPermission'
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
