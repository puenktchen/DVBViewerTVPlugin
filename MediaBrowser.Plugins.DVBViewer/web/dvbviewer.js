﻿define(['globalize', 'loading', 'appRouter', 'formHelper', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-select'], function (globalize, loading, appRouter, formHelper) {
    'use strict';

    function onBackClick() {

        appRouter.back();
    }

    function getTunerHostConfiguration(id) {

        if (id) {
            return ApiClient.getTunerHostConfiguration(id);
        } else {
            return ApiClient.getDefaultTunerHostConfiguration('dvbviewer');
        }
    }

    function reload(view, providerInfo) {

        getTunerHostConfiguration(providerInfo.Id).then(function (info) {
            fillTunerHostInfo(view, info);
        });
    }

    function fillTunerHostInfo(view, info) {

        var providerOptions = JSON.parse(info.ProviderOptions || '{}');

        view.querySelector('.txtStreamingPort').value = providerOptions.StreamingPort || '';
        view.querySelector('.txtUsername').value = providerOptions.Username || '';
        view.querySelector('.txtPassword').value = providerOptions.Password || '';

        selectRootChannelGroups(view, providerOptions);

        view.querySelector('#chkImportRadioChannels').checked = providerOptions.ImportRadioChannels || false;
        view.querySelector('#chkImportFavoritesOnly').checked = providerOptions.ImportFavoritesOnly || false;
        view.querySelector('#chkRemapProgramEvents').checked = providerOptions.RemapProgramEvents || false;

        view.querySelector('.txtDevicePath').value = info.Url || '';
        view.querySelector('.txtFriendlyName').value = info.FriendlyName || '';
    }

    function selectRootChannelGroups(view, providerOptions) {

        fetch(ApiClient.getUrl("DVBViewer/RootChannelGroups"), {
            method: "GET",
        }).then((resp) => resp.json())
            .then(function (groups) {
                view.querySelector('#selectRootChannelGroup', view).innerHTML = groups.map(function (group) {
                    var selectedText = group == providerOptions.RootChannelGroup ? " selected" : "";
                    return '<option value="' + group + '"' + selectedText + '>' + group + '</option>';
                });
            });
    }

    function alertText(options) {

        require(['alert']).then(function (responses) {
            responses[0](options);
        });
    }

    return function (view, params) {

        view.addEventListener('viewshow', function () {

            reload(view, {
                Id: params.id
            });
        });

        view.querySelector('.btnCancel').addEventListener("click", onBackClick);

        function submitForm(page) {

            loading.show();

            getTunerHostConfiguration(params.id).then(function (info) {

                var providerOptions = JSON.parse(info.ProviderOptions || '{}');

                providerOptions.Username = view.querySelector('.txtUsername').value;
                providerOptions.Password = view.querySelector('.txtPassword').value;
                providerOptions.StreamingPort = view.querySelector('.txtStreamingPort').value;

                providerOptions.RootChannelGroup = view.querySelector('#selectRootChannelGroup').value;

                providerOptions.ImportRadioChannels = view.querySelector('#chkImportRadioChannels').checked;
                providerOptions.ImportFavoritesOnly = view.querySelector('#chkImportFavoritesOnly').checked;
                providerOptions.RemapProgramEvents = view.querySelector('#chkRemapProgramEvents').checked;

                info.FriendlyName = page.querySelector('.txtFriendlyName').value || null;
                info.Url = page.querySelector('.txtDevicePath').value || null;

                info.ProviderOptions = JSON.stringify(providerOptions);

                ApiClient.saveTunerHostConfiguration(info).then(function (result) {

                    formHelper.handleConfigurationSavedResponse();

                    appRouter.show(appRouter.getRouteUrl('LiveTVSetup', {
                        SavedTunerHostId: (result || {}).Id || info.Id,
                        IsNew: params.id == null
                    }));

                }, function () {
                    loading.hide();

                    alertText({
                        text: globalize.translate('ErrorSavingTvProvider')
                    });
                });
            });
        }

        view.querySelector('form').addEventListener('submit', function (e) {
            e.preventDefault();
            e.stopPropagation();
            submitForm(view);
            return false;
        });
    };
});