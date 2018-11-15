﻿Dropzone.autoDiscover = false;

$(document).ready(function () {
    var pvVue = new Vue({
        el: '#content-project-view',
        data: {
            projectId: $('#projectId').val(),
            isAvgLoadTimesLoaded: false,
            activeTab: null,
            reloadSeconds: 30,
            unprocessedCount: $('#unprocessedCount').val(),
            countdownTimer: null
        },
        methods: {
            deleteProject() {
                var pid = this.projectId;
                $.ajax({
                    url: "/project/delete/" + pid,
                    method: 'POST',
                    dataType: "json"
                }).done(function (response) {
                    window.location.href = '/';
                    })
                .fail(function (jqXHR, textStatus) {
                    alert("Request failed: " + textStatus);
                });
            },
            initaliseAvgLoadTimesGrid: function (projectId) {
                $("#grid-project-load-times").jsGrid({
                    width: "100%",
                    height: "440px",
                    sorting: true,
                    paging: true,
                    autoload: false,

                    controller: {
                        loadData: function () {
                            var d = $.Deferred();
                            $.ajax({
                                url: "/project/" + projectId + "/avgloadtimes",
                                method: 'POST',
                                dataType: "json"
                            }).done(function (response) {
                                d.resolve(response);
                            });

                            return d.promise();
                        }
                    },
                    loadIndicator: function (config) {
                        var container = config.container[0];
                        var spinner = new Spinner();

                        return {
                            show: function () {
                                spinner.spin(container);
                            },
                            hide: function () {
                                spinner.stop();
                            }
                        };
                    },
                    fields: [
                        { name: "uriStemAggregate", title: "URI Stem", type: "text" },
                        { name: "requestCount", title: "Request Count", type: "number", width: 50 },
                        { name: "avgTimeTakenMilliseconds", title: "Avg Time Taken (ms)", type: "number", width: 50 }
                    ]
                });
            },
            initialiseDropzone: function () {
                var that = this;
                Dropzone.options.dropzoneFileUpload = {
                    error: function (file, response) {
                        $(file.previewElement).addClass("dz-error").find('.dz-error-message').text(response);
                    },
                    queuecomplete: function () {
                        that.reloadAll();
                    }
                };
                $('#dropzoneFileUpload').dropzone();

            },
            initaliseProjectFileGrid: function (projectId) {
                var that = this;
                $("#grid-project-files").jsGrid({
                    width: "100%",
                    height: "440px",
                    sorting: true,
                    paging: true,
                    autoload: false,

                    controller: {
                        loadData: function () {
                            var d = $.Deferred();
                            $.ajax({
                                url: "/project/" + projectId + "/files",
                                method: 'POST',
                                dataType: "json"
                            }).done(function (response) {
                                var upc = 0;
                                for (var i = 0; i < response.length; i++) {
                                    if (!response[i].isProcessed) {
                                        upc++;
                                    }
                                }
                                that.unprocessedCount = upc;
                                that.initReloadCountdown();
                                d.resolve(response);
                            });

                            return d.promise();
                        }
                    },
                    loadIndicator: function (config) {
                        var container = config.container[0];
                        var spinner = new Spinner();

                        return {
                            show: function () {
                                spinner.spin(container);
                            },
                            hide: function () {
                                spinner.stop();
                            }
                        };
                    },
                    fields: [
                        { name: "fileName", title: "File Name", type: "text", width: 150, validate: "required" },
                        { name: "fileLength", title: "Size", type: "number", width: 50 },
                        { name: "recordCount", title: "Records", type: "number", width: 200 }
                    ]
                });
            },
            initReloadCountdown: function () {
                if (this.unprocessedCount > 0) {
                    this.reloadSeconds = 30;
                    this.countdownTimer = setInterval(() => {
                        this.reloadSeconds--;
                        if (this.reloadSeconds <= 0) {
                            clearInterval(this.countdownTimer);
                            this.countdownTimer = null;
                            this.reloadAll();
                        }
                    }, 1000);
                }
            },
            onAddProjectFilesClick: function () {
                $('#dlg-project-files').modal('show');
            },
            onDeleteProjectClick: function () {
                var that = this;
                bootbox.confirm({
                    message: "Are you sure you want to delete this project?<br /><br />All files and related data will be deleted.",
                    buttons: {
                        cancel: {
                            label: 'Cancel',
                            className: 'btn-success'
                        },
                        confirm: {
                            label: 'Yes',
                            className: 'btn-danger'
                        }
                    },
                    callback: function (result) {
                        if (result) {
                            that.deleteProject();
                        }
                    }
                });
            },
            onLoadTimesTabShown: function () {
                if (!this.isAvgLoadTimesLoaded) {
                    $("#grid-project-load-times").jsGrid("loadData");
                    this.isAvgLoadTimesLoaded = true;
                }
                $("#grid-project-load-times").jsGrid("refresh");
            },
            reloadAll: function () {
                if (this.countdownTimer != null) {
                    clearInterval(this.countdownTimer);
                    this.countdownTimer = null;
                }
                $("#grid-project-files").jsGrid("loadData");
                $("#grid-project-load-times").jsGrid("loadData");
            }
        },
        mounted: function () {
            var that = this;
            this.initialiseDropzone();
            this.initaliseProjectFileGrid(this.projectId);
            this.initaliseAvgLoadTimesGrid(this.projectId);
            $(document).on('shown.bs.tab', 'a[data-toggle="tab"]', function (e) {
                that.activeTab = e.target;
                if (that.activeTab.hash == '#tab-loadtimes') {
                    that.onLoadTimesTabShown();
                }
            });
            this.reloadAll();
        },
    });
});


