angular.module('virtoCommerce.exportModule')
    .factory('virtoCommerce.exportModule.webApi', ['$resource', function ($resource) {
        return $resource('api/VirtoCommerceExportModule');
}]);
