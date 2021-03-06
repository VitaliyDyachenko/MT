﻿angular.module('mtApp').controller('privateInfoController',
function ($scope, validationService, privateInfoControllerService, datePickerOptions, errorService, modelFromServer) {



    if (modelFromServer != null) {
        $scope.userInfo = modelFromServer;
        $scope.userInfo.IsMan = modelFromServer.IsMan.toString();
    } else 
    {
        $scope.userInfo = {
            Name: "",
            SecondName: "",
            IsMan: false,
            teOfBirth: null,
            UTCZone: '0'
        }
    }
    
    
   

    $("#Name").focus();

    $scope.today = function () {
        $scope.userInfo.DateOfBirth = new Date();
    };
    //if ($scope.userInfo.DateOfBirth==null) $scope.today();


    $scope.open = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();

        $scope.opened = true;
    };

    $scope.dateOptions = datePickerOptions;
    $scope.formats = datePickerOptions.formats;
    $scope.format = $scope.formats[0];

    $scope.showResult = function (responce) {

        if (responce.header == "Error") {
            errorService.showError($scope, responce.errorMessagesList);
        }

        else {
            $scope.isSuccessVisible = true;
            $scope.messageFromServer = responce.data.message;
        }

        
    };

    // formName - form name for validation
    $scope.create = function (formName) {


        if (!validationService.isValid(formName))
            return;
        $scope.messageFromServer = "";

        privateInfoControllerService.saveResource($scope, $scope.userInfo, $scope.showResult);
    };
});

// Fabric method for saving LibraryResource instance using httpService
angular.module('mtApp').factory('privateInfoControllerService', function (httpService) {
    return {
        saveResource: function (scope, resource, showResult) {
            return httpService.post('PersonalCabinet/Create', resource).then(function (responce) {
                showResult(responce);
            });
        }


    };

});
