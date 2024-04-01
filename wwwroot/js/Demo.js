function createQueryBuilder(instanceId, elemsId) {
    AQB.Web.UI.QueryBuilder(instanceId, $(elemsId[0]), { theme: 'jqueryui', language: 'en' });
    AQB.Web.UI.ObjectTreeView(instanceId, $(elemsId[1]));
    AQB.Web.UI.SubQueryNavigationBar(instanceId, $(elemsId[2]));
    AQB.Web.UI.Canvas(instanceId, $(elemsId[3]), {
        linkColor: '#2191c0',
        linkStyle: AQB.Web.UI.Enums.LinkStyle.MSSQL,
        resizeMode: 's, e, n, w'
    });
    AQB.Web.UI.StatusBar(instanceId, $(elemsId[4]));
    AQB.Web.UI.Grid(instanceId, $(elemsId[5]), {
        orColumnCount: 0,
        useCustomExpressionBuilder: AQB.Web.Enum.AffectedColumns.ExpressionColumn
    });
    AQB.Web.UI.SqlEditor(instanceId, $(elemsId[6]));
}

function createQueryResults() {
    var instanceId = 'QueryResults';

    if (AQB.Web.QueryBuilderContainer.get(instanceId))
        return;

    createQueryBuilder(instanceId, ['#qrqb', '#qrtreeview', '#qrnavbar', '#qrcanvas', '#qrstatusbar', '#qrgrid', '#qreditor']);

    AQB.Web.UI.UserQueries(instanceId, $('#qruserqueries'));
    AQB.Web.UI.SqlEditor(instanceId, $('#qrsqeditor'), { targetQueryPart: 'SubQuery' });
    AQB.Web.UI.CriteriaBuilder(instanceId, $('#qrcb'), { autoSubscribe: false });

    AQB.Web.UI.startApplication(instanceId);
}

function createAlternateNames() {
    var instanceId = 'AlternateNames';

    if (AQB.Web.QueryBuilderContainer.get(instanceId))
        return resetLayout(instanceId);

    createQueryBuilder(instanceId, ['#anqb', '#antreeview', '#annavbar', '#ancanvas', '#anstatusbar', '#angrid', '#aneditor']);
    
    $('.toggler').change(function () { onToggleAlterNames(instanceId); });

    AQB.Web.UI.startApplication('/Home/CreateAlternateNames', instanceId);
}

function createVirtualObjects() {
    var instanceId = 'VirtualObjects';

    if (AQB.Web.QueryBuilderContainer.get(instanceId))
        return resetLayout(instanceId);

    createQueryBuilder(instanceId, ['#voqb', '#votreeview', '#vonavbar', '#vocanvas', '#vostatusbar', '#vogrid', '#voeditor']);

    AQB.Web.UI.startApplication('/Home/CreateVirtualObjects', instanceId);
}

function subscribeToChanges(qb) {
    createCodeMirror(qb);
    hideOverlay(qb.SessionID);

    if (qb.SessionID !== "QueryResults")
        return;

    loadPreview(qb.SessionID);
    createExpressionEditor(qb);

    if (qb.EditorComponent) {
        qb.on(qb.Events.ActiveUnionSubQueryChanged, function() { loadPreview(qb.SessionID); });
        qb.on(qb.Events.SqlChanged, function () { loadPreview(qb.SessionID); });
        return;
    }
}

function subscribeToChangesCb(cb) {
    cb.on(cb.Events.AfterSyncCriteriaBuilder, function () {
        onCriteriaBuilderChanged(cb, function () {
            if (!$('#qr').is(':visible'))
                return;

            var params = getUniqueQueryParams();
            clearParams();

            if (params.length) {
                if (fillJsonEditor.editor) {
                    $("#jqxgrid").jqxGrid('clear');
                    $("#jsgrid").jsGrid('destroy');
                    ReactDOM.unmountComponentAtNode(document.getElementById('reactgrid'));
                    fillJsonEditor.editor.destroy();
                    fillJsonEditor.editor = undefined;
                }
                createParams(params);
            } else {
                createGrids();
            }
        });
    });

    cb.on(cb.Events.CriteriaBuilderChanged, function () {
        onCriteriaBuilderChanged(cb, updateGrids);
    });
}

function loadPreview(name) {
    if (!container.is(':visible'))
        return;

    $('.error-preview').hide();

    if (grid) {
        grid.show();
        grid.jqxGrid('showloadelement');
    }

    $.ajax({
        url: previewUrl,
        data: { name: name },
        success: showPreview,
        error: errorPreview
    });
}

function showPreview(result) {
    if (grid)
        grid.jqxGrid('destroy');

    grid = $('<div class="grid">');
    container.append(grid);

    var source = {
        localdata: result.Data,
        datafields: result.Columns.map(function (c, i) {
            return { name: c, map: i.toString() }
        }),
        datatype: "array"
    };

    var dataAdapter = new $.jqx.dataAdapter(source);

    grid.jqxGrid({
        width: '100%',
        height: 227,
        source: dataAdapter,
        columnsresize: true,
        columns: result.Columns.map(function (c) {
            return { text: c, datafield: c }
        })
    });
}

function errorPreview(xhr) {
    if (grid) {
        grid.hide();
        grid.jqxGrid('hideloadelement');
        grid.jqxGrid('clear');
    }

    $('.error-preview').show().text(xhr.responseText);
}

function createCodeMirror(qb) {
    if (qb.EditorComponent)
        create(qb.EditorComponent);

    if (qb.ActiveSubQueryEditorComponent)
        create(qb.ActiveSubQueryEditorComponent);

    //if (qb.ActiveUnionSubQueryEditorComponent)
        //create(qb.ActiveUnionSubQueryEditorComponent);

    function create(component) {
        var codeMirror = CodeMirror(document.body,
            {
                mode: 'text/x-sql',
                indentWithTabs: true,
                smartIndent: true,
                lineNumbers: true,
                matchBrackets: true
            });

        component.setEditor({
            element: codeMirror.display.wrapper,
            getSql: function () {
                return codeMirror.getValue();
            },
            setSql: function (sql) {
                codeMirror.setValue(sql);
            },
            setCursorPosition: function (pos, col, line) {
                this.focus();
                codeMirror.setCursor(line - 1, col - 1, { scroll: true });
            },
            focus: function () {
                codeMirror.focus();
            },
            onChange: function (callback) {
                this.changeCallback = callback;
                codeMirror.on('change', this.changeCallback);
            },
            remove: function () {
                codeMirror.off('change', this.changeCallback);
                this.element.remove();
            }
        });
    }
}

function hideOverlay(name) {
    document.querySelectorAll('.iframe-overlay.' + name)[0].style.display = 'none';
}

function insertJqueryTheme() {
    setTimeout(function() {
        var t = document.getElementsByTagName('title')[0];
        var h = document.getElementsByTagName('head')[0];
        var l = document.createElement('link');
        l.href = "https://code.jquery.com/ui/1.12.1/themes/start/jquery-ui.css";
        l.rel = "stylesheet";
        l.type = "text/css";
        //h.insertBefore(l, t); 
    }, 1000);
}

function createExpressionEditor(qb) {
    window.codeMirror = CodeMirror(document.body, {
        mode: 'text/x-sql',
        indentWithTabs: true,
        smartIndent: true,
        lineNumbers: true,
        matchBrackets: true,
        width: '500px',
        height: '250px'
    });

    window.codeMirror.display.wrapper.style.display = 'none';

    qb.GridComponent.on(AQB.Web.QueryBuilder.GridComponent.Events.GridBeforeCustomEditCell, BeforeCustomEditCell);
}

function BeforeCustomEditCell(data) {
    var row = data.row;
    var cell = data.cell;

    var error = $('<p class="ui-state-error" style="display: none;"></div>');

    var $dialog = $('<div>').dialog({
        modal: true,
        width: 'auto',
        title: 'Custom expression editor',
        appendTo: '#qrqb',
        buttons: [{
            text: "OK",
            click: function () {
                var newValue = codeMirror.getValue();

                var ifValid = function () {
                    cell.updateValue(newValue);
                    $dialog.dialog("close");
                }

                var ifNotValid = function (message) {
                    error.html(message).show();
                }

                validate(newValue, ifValid, ifNotValid);
            }
        }, {
            text: "Cancel",
            click: function () {
                $dialog.dialog("close");
            }
        }]
    });

    window.codeMirror.display.wrapper.style.display = 'block';

    $dialog.append(error, window.codeMirror.display.wrapper);
    $dialog.parent().css({
        top: '25%',
        left: '30%',
        width: 600
    });

    codeMirror.setValue(row.FormattedExpression || '');
    codeMirror.refresh();
};

function validate(expression, ifValid, ifNotValid) {
    AQB.Web.QueryBuilder.validateExpression(expression, function (isValid, message) {
        if (isValid)
            ifValid();
        else
            ifNotValid(message);
    });
}

function resetLayout(name) {
    setTimeout(function () {
        AQB.Web.QueryBuilderContainer.get(name).resetLayout();
    }, 100);
}

function onOpenQueryResults() {
    var cb = AQB.Web.CriteriaBuilderContainer.first();
    cb.loadColumns();
};

function createGrids() {
    $('.alert-danger').hide();
    var cb = AQB.Web.CriteriaBuilderContainer.first();

    var columns = cb.Columns.map(function (c) {
        return {
            key: c.ResultName,
            name: c.ResultName,
            text: c.ResultName,
            datafield: c.ResultName
        }
    });

    createJqxGrid(columns);
    createJsGrid(columns);
    createReactGrid(columns);
    fillJsonEditor(0);
}

function updateGrids() {
    $('.alert-danger').hide();
    dataAdapter.dataBind();
    jsgrid.jsGrid();
    reactGrid.updateRows();
    fillJsonEditor(0);
}

function onCriteriaBuilderChanged(cb, callback) {
    if (!$('#qr').is(':visible'))
        return;

    cb.transformSql(function (sql) {
        $('.sql').text(sql);
        callback();
    });
}

function createJqxGrid(columns) {
    var source = {
        type: 'POST',
        contentType: 'application/json;',
        datatype: 'json',
        url: dataUrl,
        formatData: function (data) {
            data.params = getParams();
            return JSON.stringify(data);
        },
        loadError: errorCallback,
        sort: function () {
            $("#jqxgrid").jqxGrid('updatebounddata');
        },
        datafields: columns.map(function (c) {
            return { name: c.Name }
        }),
        totalrecords: 9999999
    };

    window.dataAdapter = new $.jqx.dataAdapter(source);

    try {
        $("#jqxgrid").jqxGrid({
            width: '100%',
            source: dataAdapter,
            pageable: true,
            sortable: true,
            virtualmode: true,
            rendergridrows: function () {
                return dataAdapter.loadedData;
            },
            columns: columns
        });
    } catch (err) {
        console.log(err);
    }
}

function createReactGrid(columns) {
    ReactDOM.unmountComponentAtNode(document.getElementById('reactgrid'));

    getData(init, 0);

    function getData(callback, pageNum, sortField, sortOrder) {
        $.ajax({
            url: dataUrl,
            type: 'POST',
            contentType: 'application/json;',
            datatype: 'json',
            data: JSON.stringify({
                pagenum: pageNum,
                pagesize: 10,
                sortdatafield: sortField,
                sortorder: sortOrder,
                params: getParams()
            }),
            success: callback,
            error: errorCallback
        });
    }

    function init(data) {
        var Grid = React.createClass({
            getInitialState: function () {
                this._columns = columns.map(function (c) {
                    c.sortable = true;
                    c.width = 300;
                    return c;
                });

                return { rows: data, page: 0 };
            },

            sort: function (field, order) {
                getData(function (data) {
                    this.setState({ rows: data });
                }.bind(this),
                    this.state.page,
                    field,
                    order !== 'NONE' ? order : undefined);

                this.setState({ field: field, order: order });
            },

            page: function (page) {
                getData(function (data) {
                    this.setState({ rows: data });
                }.bind(this),
                    page,
                    this.state.field,
                    this.state.order !== 'NONE' ? this.state.order : undefined);

                this.setState({ page: page });
            },

            updateRows: function () {
                getData(function (data) {
                    this.setState({ rows: data });
                }.bind(this),
                    this.state.page,
                    this.state.field,
                    this.state.order !== 'NONE' ? this.state.order : undefined);
            },

            prevPage: function () {
                this.page(this.state.page - 1);
            },

            nextPage: function () {
                this.page(this.state.page + 1);
            },

            rowGetter: function (i) {
                return this.state.rows[i];
            },

            render: function () {
                return React.createElement('div',
                    null,
                    [
                        React.createElement(ReactDataGrid,
                            {
                                onGridSort: this.sort,
                                columns: this._columns,
                                rowGetter: this.rowGetter,
                                rowsCount: this.state.rows.length,
                                minHeight: 500
                            }),
                        React.createElement('span', { onClick: this.prevPage }, ['prev ']),
                        React.createElement('span', { onClick: this.nextPage }, [' next'])
                    ]);
            }
        });

        var gridElem = React.createElement(Grid);
        window.reactGrid = ReactDOM.render(gridElem, document.getElementById('reactgrid'));
    }
}

function createJsGrid(columns) {
    window.jsgrid = $("#jsgrid").jsGrid({
        width: "100%",
        height: "400px",
        sorting: true,
        paging: true,
        pageLoading: true,
        pageSize: 10,
        autoload: true,
        fields: columns,
        controller: {
            loadData: function (filter) {
                var d = $.Deferred();

                $.ajax({
                    url: dataUrl,
                    type: 'POST',
                    contentType: 'application/json;',
                    dataType: 'json',
                    data: JSON.stringify({
                        pagenum: filter.pageIndex - 1,
                        pagesize: filter.pageSize,
                        sortdatafield: filter.sortField,
                        sortorder: filter.sortOrder,
                        params: getParams()
                    })
                }).done(function (res) {
                    d.resolve({
                        data: res,
                        itemsCount: 9999999
                    }).fail(errorCallback);
                });

                return d.promise();
            }
        }
    });

    $('jsgrid-header-cell').click(function () {
        var field = this.innerText;
        $("#jsgrid").jsGrid("sort", field);
    });
}

function fillJsonEditor(page) {
    if (page < 0)
        return;

    fillJsonEditor.page = page;

    $('.jsonPage').text(page);

    if (!fillJsonEditor.editor) {
        var container = document.getElementById('jsoneditor');
        fillJsonEditor.editor = new JSONEditor(container, { mode: 'code' });
    }

    $.ajax({
        type: 'POST',
        contentType: 'application/json;',
        dataType: 'json',
        url: dataUrl,
        data: JSON.stringify({
            pagenum: page,
            pagesize: 10,
            params: getParams()
        }),
        success: function (data) {
            fillJsonEditor.editor.set(data);
        },
        error: errorCallback
    });
}

function updateVersion() {
    $.ajax({
        type: 'GET',
        url: 'https://www.activequerybuilder.com/member/version.php?section=3-11',
        success: function (data) {
            $('#version-string').html(data);
        }
    });
}

function refreshCodeMirror() {
    var elem = $(this).find('.CodeMirror').get(0);

    if (elem)
        elem.CodeMirror.refresh();
}

function onToggleAlterNames(name) {
    $.ajax({
        url: '/home/toggle',
        data: { name: name },
        success: function () {
            AQB.Web.QueryBuilderContainer.get(name).fullUpdate();
        }
    });
}

function subscribeToExchangeData() {
    AQB.Web.Core.on(AQB.Web.Core.Events.UserDataReceived,
        function (data) {
            if (data.SessionID === "VirtualObjects")
                $('.virtual-sql textarea').text(data.sql);
            else
                $('.alternate-sql textarea').text(data.sql);
        });
}

function createParams(params) {
    var table = $('.table-params tbody');

    for (var i = 0; i < params.length; i++) {
        var tr = $('<tr>');
        var name = $('<td>' + params[i].FullName + '</td>');
        var value = $('<td><input type="text" class="' + params[i].Name + '" /></td>');
        tr.append(name).append(value);
        table.append(tr);
    }

    $('.table-params').show();
    $('.params-message').show();
}

function clearParams() {
    $('.table-params tbody').empty();
    $('.table-params').hide();
    $('.params-message').hide();
}

function getParams() {
    var result = [];
    var params = getUniqueQueryParams();

    for (var i = 0; i < params.length; i++) {
        result.push({
            Name: params[i].FullName,
            Value: $('input.' + params[i].Name).val()
        });
    }

    return result;
}

function getUniqueQueryParams() {
    var params = AQB.Web.QueryBuilder.queryParams;
    var result = [];

    for (var i = 0, l = params.length; i < l; i++) {
        var param = params[i];

        if (result.find(r => r.FullName === param.FullName) == null)
            result.push(param);
    }

    return result;
}

function Demo() {
    this.syntaxes = [
        ["ANSI SQL-2003", false, []],
        ["ANSI SQL-92", false, []],
        ["ANSI SQL-89", false, []],
        ["Firebird", false, [
            "Firebird 2.0",
            "Firebird 1.5",
            "Firebird 1.0"
        ]],
        ["IBM DB2", false, []],
        ["IBM Informix", false, [
            "Informix 10",
            "Informix 9",
            "Informix 8"
        ]],
        ["Microsoft Access", false, [
            "MS Jet 4",
            "MS Jet 3"
        ]],
        ["Microsoft SQL Server", true, [
            "Auto",
            "SQL Server 2005",
            "SQL Server 2000",
            "SQL Server 7"
        ]],
        ["MySQL", false, [
            "5.0",
            "4.0",
            "3.0"
        ]],
        ["Oracle", false, [
            "Oracle 10",
            "Oracle 9",
            "Oracle 8",
            "Oracle 7"
        ]],
        ["PostgreSQL", false, []],
        ["SQLite", false, []],
        ["Sybase", false, [
            "ASE",
            "SQL Anywhere"
        ]],
        ["VistaDB", false, []],
        ["Universal", false, []]
    ];

    var me = this;

    this.toolbarSytnaxOnChange = function (event) {
        me.toolbarSytnaxVersionUpdate($(event.target).text());
        me.toolbarSytnaxChanged();
    };

    this.getQueryStatistics = function (event) {
        me.QueryStatisticsDialog.html(AQB.Web.QueryBuilder.Localizer.Strings.Loading);
        me.QueryStatisticsDialog.dialog('open');
        me.sendRequest('QueryStatistics', "");
    };


    this.processRequest = function (data) {
        if (data.Reaction === "update") {
            AQB.Web.QueryBuilder.fullUpdate();
        } else if (data.Reaction === "QueryStatistics") {
            me.QueryStatisticsDialog.html(data.QueryStatistics);
            me.QueryStatisticsDialog.dialog('open');
        }
    };

    this.sendRequest = function (action, param) {
        $.ajax({
            url: '/Home/' + action + param,
            type: 'POST',
            success: me.processRequest
        });
    };

    this.toolbarSytnaxChanged = function () {
        var $select = $("#qb-ui-syntax-selector-server");
        var $selectVersion = $("#qb-ui-syntax-selector-version");
        var syntax = $select.val();
        var syntaxVersion = $selectVersion.val();
        var params = "?Syntax=" + syntax + "&SyntaxVersion=" + syntaxVersion;
        me.sendRequest('ChangeSyntax', params);
    };

    this.toolbarSytnaxVersionUpdate = function (val) {
        var $selectVersion = $("#qb-ui-syntax-selector-version");
        var options = [];
        for (var i = 0; i < this.syntaxes.length; i++) {
            var item = this.syntaxes[i];
            if (item[0] == val) {
                for (var j = 0; j < item[2].length; j++) {
                    options.push('<option value="' + item[2][j] + '">' + item[2][j] + '</option>');
                }
                break;
            }
        }
        if (options.length == 0) {
            options.push('<option value="">Auto</option>');
            $selectVersion.attr('disabled', 'disabled');
        } else {
            $selectVersion.removeAttr('disabled');
        }

        if ($selectVersion.data('initialized'))
            $selectVersion.selectmenu("destroy");

        $selectVersion.html(options.join(''));
        $selectVersion.selectmenu();
        $selectVersion.data('initialized', true);
        $('#qb-ui-syntax-selector-version-menu').click(me.toolbarSytnaxChanged);
    };

    this.init = function () {
        var me = this;
        var options = new Array(this.syntaxes.length);
        for (var i = 0; i < this.syntaxes.length; i++) {
            var item = this.syntaxes[i];
            var syntaxName = item[0];
            var syntaxSelected = item[1];
            options[i] = '<option' + (syntaxSelected ? ' selected' : '') + ' value="' + syntaxName + '">' + syntaxName + '</option>';
        }

        var $select = $("#qb-ui-syntax-selector-server");
        $select.html(options.join(''));
        $select.selectmenu();
        $select.change(me.toolbarSytnaxOnChange);

        var $selectVersion = $("#qb-ui-syntax-selector-version");
        $selectVersion.change(me.toolbarSytnaxChanged);

        this.toolbarSytnaxVersionUpdate($select.val());

        $('#qb-ui-query-statistic').bind('click', me.getQueryStatistics);
        this.QueryStatisticsDialog = $('#qb-ui-query-statistic-dialog').dialog({
            autoOpen: false,
            zIndex: 7000,
            width: 600,
            height: 300,
            modal: true
        });

        $('#qb-ui-syntax-selector-server-menu').click(me.toolbarSytnaxOnChange);
    };

    this.init();
};

function errorCallback(xhr, error, statusText) {
    $('.alert-danger').show().text(statusText);
}

function subscribeDomEvents() {
    $('.expander').click(function () {
        $(this).next('div').animate({
            opacity: "toggle",
            height: "toggle"
        });
    });

    $('.execute').click(function () {
        if (fillJsonEditor.editor)
            updateGrids();
        else
            createGrids();
    });

    $('.next').button().click(function() {
        fillJsonEditor(fillJsonEditor.page + 1);
    });

    $('.prev').button().click(function() {
        fillJsonEditor(fillJsonEditor.page - 1);
    });

    $('[href="#qr"]').click(onOpenQueryResults);

    $('.qb-ui-layout__bottom .qb-ui-structure-tabs__tab').click(refreshCodeMirror);

    $('#ld-demo-altnames').click(createAlternateNames);

    $('#ld-demo-virtual').click(createVirtualObjects);

    $('#qr-tab').click(function() {
        loadPreview("QueryResults");
    });

    $('#nav-tabs > li > a').click(function (e) {
        e.preventDefault();

        $('.tab-pane').hide();
        $('#nav-tabs > li').removeClass('active');

        $(this.parentNode).addClass('active');
        $(this.getAttribute('href')).show();
    });


    $('#main-tabs').tabs();
    $('#second-tabs').tabs();

    setTimeout(function() {
        $("#switcher").themeswitcher({
            imgpath: 'https://www.activequerybuilder.com/cdn/img/themeroller/',
            loadtheme: 'start',
            jqueryuiversion: "1.12.1"
        }, 1000); 
    });
}

function addScript() {
    
}

var container = $('#preview'),
    grid,
    previewUrl = "Home/GetPreviewData",
    dataUrl = "Home/GetData",
    demo = new Demo();

subscribeDomEvents();
updateVersion();
subscribeToExchangeData();

AQB.Web.onQueryBuilderReady(subscribeToChanges);
AQB.Web.onCriteriaBuilderReady(subscribeToChangesCb);

insertJqueryTheme();
createQueryResults();

//polyfill

// https://tc39.github.io/ecma262/#sec-array.prototype.find
if (!Array.prototype.find) {
    Object.defineProperty(Array.prototype, 'find', {
        value: function (predicate) {
            // 1. Let O be ? ToObject(this value).
            if (this == null) {
                throw new TypeError('"this" is null or not defined');
            }

            var o = Object(this);

            // 2. Let len be ? ToLength(? Get(O, "length")).
            var len = o.length >>> 0;

            // 3. If IsCallable(predicate) is false, throw a TypeError exception.
            if (typeof predicate !== 'function') {
                throw new TypeError('predicate must be a function');
            }

            // 4. If thisArg was supplied, let T be thisArg; else let T be undefined.
            var thisArg = arguments[1];

            // 5. Let k be 0.
            var k = 0;

            // 6. Repeat, while k < len
            while (k < len) {
                // a. Let Pk be ! ToString(k).
                // b. Let kValue be ? Get(O, Pk).
                // c. Let testResult be ToBoolean(? Call(predicate, T, « kValue, k, O »)).
                // d. If testResult is true, return kValue.
                var kValue = o[k];
                if (predicate.call(thisArg, kValue, k, o)) {
                    return kValue;
                }
                // e. Increase k by 1.
                k++;
            }

            // 7. Return undefined.
            return undefined;
        },
        configurable: true,
        writable: true
    });
}