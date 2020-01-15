
/**
 * jQuery script for creating lookup box
 * @version 1.2
 * @requires jQuery UI
 *
 * Copyright (c) 2016 Lucky
 * Licensed under the GPL license:
 *   http://www.gnu.org/licenses/gpl.html
 */

(function ($) {
    function lookupbox(options) {
        var settings = {
            id: "lookup-dialog-box",
            title: "Lookup",
            url: "",
            tipo: 0,
            loadingDivId: "loading1",
            imgLoader: '<img src="../Images/ajax-loader.gif">',
            notFoundMessage: "&iexcl;Datos no encontrados!",
            requestErrorMessage: "Error de repuestas!",
            tableHeader: null,
            onSearch: null,
            searchButtonId: "lookupbox-search-button",
            searchTextId: "lookupbox-search-key",
            searchResultId: "lookupbox-search-result",
            width: 800,
            htmlForm: '<form id="lookupbox-search-form" onsubmit="return false"><div class="input-group"><input class="form-control" id="lookupbox-search-key" type="text" name="key" placeholder="Ingrese su b&uacute;squeda." /><span class="input-group-btn"><button id="lookupbox-search-button" type="button" class="btn btn-primary"><i class="fa fa-search"></i></button></span></div><span id="loading1"></span></form><div id="lookupbox-search-result"></div>',
            modal: true,
            draggable: false,
            onItemSelected: null,
            item: null,
            hiddenFields: []
        };
        $.extend(settings, options);

        $(document).ready(function () {
            if ($("#" + settings.id).length == 0) {
                var $dialog = $('<div id="' + settings.id + '"></div>');
            }
            else {
                var $dialog = $('#' + settings.id);
            }

            $dialog = $dialog.html(settings.htmlForm)
              .dialog({
                  autoOpen: false,
                  title: settings.title,
                  modal: settings.modal,
                  draggable: settings.draggable,
                  width: settings.width,
                  autoReposition: true
              });

            $(window).resize(function () { $dialog.dialog("option", "position", "center"); });

            $("#" + settings.searchButtonId).click(function () {
                if (settings.onSearch == null) {
                    $.ajax({
                        beforeSend: function () {
                            //$('#' + settings.loadingDivId).html(settings.imgLoader);
                        },
                        url: settings.url + $("#" + settings.searchTextId).val(),

                        success: function (result) {
                           // try {
                                var data = null;

                                if (typeof result == 'string')
                                    data = $.parseJSON(result);
                                else if (typeof result == 'object')
                                    data = result;

                                settings.item = data;
                                //var table = "<table cellspacing='0' cellpadding='3' id='lookupbox-result' class='lookupbox-result'>";
                                var table = "<table cellspacing='0' cellpadding='3' id='lookupbox-result' data-page-size='200' class='footable table table-condensed hover-table toggle-arrow-tiny'>";

                                if (settings.tableHeader != null) {
                                    //table = table + "<tr id='lookupbox-result-header' class='lookupbox-result-header'>";
                                    table = table + "<tr id='lookupbox-result-header'>";
                                    for (var i = 0; i < settings.tableHeader.length; i++) {
                                        table = table + "<th>" + settings.tableHeader[i] + "</th>";
                                    }
                                    table = table + "</tr>";
                                }

                                for (i = 0; i < data.length; i++) {
                                    var rowClass = 'lookupbox-result-row odd';
                                    if (i % 2 == 0) {
                                        rowClass = 'lookupbox-result-row even';
                                    }
                                    table = table + "<tr id='lookupbox-result-row-" + i + "' class='" + rowClass + "'>";
                                    //table = table + "<tr id='lookupbox-result-row-" + i + "'>";
                                    for (var key in data[i]) {
                                        var u = 0;
                                        if ($.inArray(key, settings.hiddenFields) === -1) {
                                            table = table + "<td id='t" + [u] + "'  style='cursor:pointer'>" + data[i][key] + "</td>";
                                            u = u + 1;

                                        }
                                    }
                                    table = table + "</tr>";
                                }
                                table = table + "</table>";

                                $("#" + settings.searchResultId).html(table);

                                if (settings.onItemSelected != null) {
                                    $("#lookupbox-result tr").click(function () {
                                        var arr = $(this).attr("id").split("-");
                                        if (typeof settings.onItemSelected === "function") {
                                            settings.onItemSelected.call(this, settings.item[arr[arr.length - 1]]);
                                        }
                                        $dialog.dialog("close");
                                    });
                                }
                            //}
                            /*catch (e) {
                                $("#" + settings.searchResultId).html(settings.notFoundMessage);
                            }*/
                            $('#' + settings.loadingDivId).html('');
                        },
                        error: function (xhr, status, ex) {
                            $("#" + settings.searchResultId).html(settings.requestErrorMessage);
                        }
                    });
                }
                else {
                    if (typeof settings.onSearch === "function") {
                        settings.onSearch.call();
                    }
                }
            });

            $("#" + settings.searchTextId).keyup(function (e) {
                $("#" + settings.searchButtonId).trigger('click');
            });

            $dialog.dialog('open');
            $dialog.dialog({ closeText: "Cerrar" });
            $("#" + settings.searchButtonId).trigger('click');

        });
    }

    $.fn.lookupbox = function (options) {
        var settings = options;
        return this.each(function () {
            $(this).click(function () {
                lookupbox(settings);
            });
        });
    }
})(jQuery);
