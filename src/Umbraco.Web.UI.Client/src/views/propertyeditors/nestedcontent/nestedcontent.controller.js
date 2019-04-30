﻿angular.module("umbraco").controller("Umbraco.PropertyEditors.NestedContent.DocTypePickerController", [

    "$scope",
    "Umbraco.PropertyEditors.NestedContent.Resources",

    function ($scope, ncResources) {

        $scope.add = function () {
            $scope.model.value.push({
                // As per PR #4, all stored content type aliases must be prefixed "nc" for easier recognition.
                // For good measure we'll also prefix the tab alias "nc"
                ncAlias: "",
                ncTabAlias: "",
                nameTemplate: ""
            }
            );
        }

        $scope.remove = function (index) {
            $scope.model.value.splice(index, 1);
        }

        $scope.sortableOptions = {
            axis: "y",
            cursor: "move",
            handle: ".icon-navigation"
        };

        $scope.docTypeTabs = {};

        ncResources.getContentTypes().then(function (docTypes) {
            $scope.model.docTypes = docTypes;
            
            // Populate document type tab dictionary
            docTypes.forEach(function (value) {
                $scope.docTypeTabs[value.alias] = value.variants[0].tabs;
            });
        });

        $scope.selectableDocTypesFor = function (config) {
            // return all doctypes that are:
            // 1. either already selected for this config, or
            // 2. not selected in any other config
            return _.filter($scope.model.docTypes, function (docType) {
                return docType.alias === config.ncAlias || !_.find($scope.model.value, function(c) {
                    return docType.alias === c.ncAlias;
                });
            });
        }

        if (!$scope.model.value) {
            $scope.model.value = [];
            $scope.add();
        }
    }
]);

angular.module("umbraco").controller("Umbraco.PropertyEditors.NestedContent.PropertyEditorController", [

    "$scope",
    "$interpolate",
    "$filter",
    "$timeout",
    "contentResource",
    "localizationService",
    "iconHelper",
    "copyService",
    "eventsService",
    
    function ($scope, $interpolate, $filter, $timeout, contentResource, localizationService, iconHelper, copyService, eventsService) {

        var inited = false;
        
        var contentTypeAliases = [];
        _.each($scope.model.config.contentTypes, function (contentType) {
            contentTypeAliases.push(contentType.ncAlias);
        });

        _.each($scope.model.config.contentTypes, function (contentType) {
            contentType.nameExp = !!contentType.nameTemplate
                ? $interpolate(contentType.nameTemplate)
                : undefined;
        });

        $scope.nodes = [];
        $scope.currentNode = undefined;
        $scope.realCurrentNode = undefined;
        $scope.scaffolds = undefined;
        $scope.sorting = false;

        $scope.minItems = $scope.model.config.minItems || 0;
        $scope.maxItems = $scope.model.config.maxItems || 0;

        if ($scope.maxItems === 0)
            $scope.maxItems = 1000;

        $scope.singleMode = $scope.minItems === 1 && $scope.maxItems === 1;
        $scope.showIcons = Object.toBoolean($scope.model.config.showIcons);
        $scope.wideMode = Object.toBoolean($scope.model.config.hideLabel);

        $scope.labels = {};
        localizationService.localizeMany(["grid_addElement"]).then(function(data) {
            $scope.labels.docTypePickerTitle = data[0];
        });

        // helper to force the current form into the dirty state
        $scope.setDirty = function () {
            if ($scope.propertyForm) {
                $scope.propertyForm.$setDirty();
            }
        };

        $scope.addNode = function (alias) {
            var scaffold = $scope.getScaffold(alias);

            var newNode = createNode(scaffold, null);

            $scope.currentNode = newNode;
            $scope.setDirty();
        };

        $scope.openNodeTypePicker = function ($event) {
            if ($scope.nodes.length >= $scope.maxItems) {
                return;
            }

            $scope.overlayMenu = {
                title: $scope.labels.docTypePickerTitle,
                show: false,
                style: {},
                filter: $scope.scaffolds.length > 15 ? true : false,
                orderBy: "$index",
                view: "itempicker",
                event: $event,
                submit: function(model) {                    
                    if(model && model.selectedItem) {
                        $scope.addNode(model.selectedItem.alias);
                    }
                    $scope.overlayMenu.show = false;
                    $scope.overlayMenu = null;
                },
                close: function() {
                    $scope.overlayMenu.show = false;
                    $scope.overlayMenu = null;
                }
            };

            // this could be used for future limiting on node types
            $scope.overlayMenu.availableItems = [];
            _.each($scope.scaffolds, function (scaffold) {
                $scope.overlayMenu.availableItems.push({
                    alias: scaffold.contentTypeAlias,
                    name: scaffold.contentTypeName,
                    icon: iconHelper.convertFromLegacyIcon(scaffold.icon)
                });
            });

            if ($scope.overlayMenu.availableItems.length === 0) {
                return;
            }
            
            $scope.overlayMenu.pasteItems = [];
            var availableNodesForPaste = copyService.retriveDataOfType("elementType", contentTypeAliases);
            _.each(availableNodesForPaste, function (node) {
                $scope.overlayMenu.pasteItems.push({
                    alias: node.contentTypeAlias,
                    name: node.name, //contentTypeName
                    icon: iconHelper.convertFromLegacyIcon(node.icon)
                });
            });
            $scope.overlayMenu.clickClearPaste = function($event) {
                $event.stopPropagation();
                $event.preventDefault();
                copyService.clearEntriesOfType("elementType", contentTypeAliases);
                $scope.overlayMenu.pasteItems = [];// This dialog is not connected via the copyService events, so we need to update manually.
            };
            
            if ($scope.overlayMenu.availableItems.length === 1 && $scope.overlayMenu.pasteItems.length === 0) {
                // only one scaffold type - no need to display the picker
                $scope.addNode($scope.scaffolds[0].contentTypeAlias);
                return;
            }
            
            $scope.overlayMenu.show = true;
        };

        $scope.editNode = function (idx) {
            if ($scope.currentNode && $scope.currentNode.key === $scope.nodes[idx].key) {
                $scope.currentNode = undefined;
            } else {
                $scope.currentNode = $scope.nodes[idx];
            }
        };

        $scope.deleteNode = function (idx) {
            if ($scope.nodes.length > $scope.model.config.minItems) {
                if ($scope.model.config.confirmDeletes && $scope.model.config.confirmDeletes === 1) {
                    localizationService.localize("content_nestedContentDeleteItem").then(function (value) {
                        if (confirm(value)) {
                            $scope.nodes.splice(idx, 1);
                            $scope.setDirty();
                            updateModel();
                        }
                    });
                } else {
                    $scope.nodes.splice(idx, 1);
                    $scope.setDirty();
                    updateModel();
                }
            }
        };

        $scope.getName = function (idx) {

            var name = "Item " + (idx + 1);

            if ($scope.model.value[idx]) {

                var contentType = $scope.getContentTypeConfig($scope.model.value[idx].ncContentTypeAlias);

                if (contentType != null && contentType.nameExp) {
                    // Run the expression against the stored dictionary value, NOT the node object
                    var item = $scope.model.value[idx];

                    // Add a temporary index property
                    item["$index"] = (idx + 1);

                    var newName = contentType.nameExp(item);
                    if (newName && (newName = $.trim(newName))) {
                        name = newName;
                    }

                    // Delete the index property as we don't want to persist it
                    delete item["$index"];
                }

            }

            // Update the nodes actual name value
            if ($scope.nodes[idx].name !== name) {
                $scope.nodes[idx].name = name;
            }
            
            return name;
        };
        
        $scope.getIcon = function (idx) {
            var scaffold = $scope.getScaffold($scope.model.value[idx].ncContentTypeAlias);
            return scaffold && scaffold.icon ? iconHelper.convertFromLegacyIcon(scaffold.icon) : "icon-folder";
        }

        $scope.sortableOptions = {
            axis: "y",
            cursor: "move",
            handle: ".umb-nested-content__icon--move",
            start: function (ev, ui) {
                updateModel();
                // Yea, yea, we shouldn't modify the dom, sue me
                $("#umb-nested-content--" + $scope.model.id + " .umb-rte textarea").each(function () {
                    tinymce.execCommand("mceRemoveEditor", false, $(this).attr("id"));
                    $(this).css("visibility", "hidden");
                });
                $scope.$apply(function () {
                    $scope.sorting = true;
                });
            },
            update: function (ev, ui) {
                $scope.setDirty();
            },
            stop: function (ev, ui) {
                $("#umb-nested-content--" + $scope.model.id + " .umb-rte textarea").each(function () {
                    tinymce.execCommand("mceAddEditor", true, $(this).attr("id"));
                    $(this).css("visibility", "visible");
                });
                $scope.$apply(function () {
                    $scope.sorting = false;
                    updateModel();
                });
            }
        };

        $scope.getScaffold = function (alias) {
            return _.find($scope.scaffolds, function (scaffold) {
                return scaffold.contentTypeAlias === alias;
            });
        }

        $scope.getContentTypeConfig = function (alias) {
            return _.find($scope.model.config.contentTypes, function (contentType) {
                return contentType.ncAlias === alias;
            });
        }
        
        $scope.showCopy = copyService.supportsCopy();
        
        $scope.showPaste = false;
        
        $scope.clickCopy = function($event, node) {
            
            syncCurrentNode();
            
            copyService.copy("elementType", node);
            $event.stopPropagation();
        }
        
        $scope.clickPaste = function($event) {
            
            var newNode = copyService.retriveDataOfType("elementType", contentTypeAliases).pop();
            $scope.pasteFromClipboard(newNode);
            
        }
        
        $scope.pasteFromClipboard = function(newNode) {
            
            if (newNode === undefined) {
                return;
            }
            
            // generate a new key.
            newNode.key = String.CreateGuid();
            
            $scope.nodes.push(newNode);
            //updateModel();// done by setting current node...
            
            $scope.currentNode = newNode;
        }
        
        function checkAbilityToPasteContent() {
            $scope.showPaste = copyService.hasEntriesOfType("elementType", contentTypeAliases);
        }
        
        eventsService.on("copyService.storageUpdate", checkAbilityToPasteContent);
        
        var notSupported = [
          "Umbraco.Tags",
          "Umbraco.UploadField",
          "Umbraco.ImageCropper"
        ];

        // Initialize
        var scaffoldsLoaded = 0;
        $scope.scaffolds = [];
        _.each($scope.model.config.contentTypes, function (contentType) {
            contentResource.getScaffold(-20, contentType.ncAlias).then(function (scaffold) {
                // make sure it's an element type before allowing the user to create new ones
                if (scaffold.isElement) {
                    // remove all tabs except the specified tab
                    var tabs = scaffold.variants[0].tabs;
                    var tab = _.find(tabs, function (tab) {
                        return tab.id !== 0 && (tab.alias.toLowerCase() === contentType.ncTabAlias.toLowerCase() || contentType.ncTabAlias === "");
                    });
                    scaffold.variants[0].tabs = [];
                    if (tab) {
                        scaffold.variants[0].tabs.push(tab);

                        angular.forEach(tab.properties,
                            function (property) {
                                if (_.find(notSupported, function (x) { return x === property.editor; })) {
                                    property.notSupported = true;
                                    // TODO: Not supported message to be replaced with 'content_nestedContentEditorNotSupported' dictionary key. Currently not possible due to async/timing quirk.
                                    property.notSupportedMessage = "Property " + property.label + " uses editor " + property.editor + " which is not supported by Nested Content.";
                                }
                            });
                    }

                    // Store the scaffold object
                    $scope.scaffolds.push(scaffold);
                }

                scaffoldsLoaded++;
                initIfAllScaffoldsHaveLoaded();
            }, function (error) {
                scaffoldsLoaded++;
                initIfAllScaffoldsHaveLoaded();
            });
        });

        var initIfAllScaffoldsHaveLoaded = function () {
            // Initialize when all scaffolds have loaded
            if ($scope.model.config.contentTypes.length === scaffoldsLoaded) {
                // Because we're loading the scaffolds async one at a time, we need to
                // sort them explicitly according to the sort order defined by the data type.
                contentTypeAliases = [];
                _.each($scope.model.config.contentTypes, function (contentType) {
                    contentTypeAliases.push(contentType.ncAlias);
                });
                $scope.scaffolds = $filter("orderBy")($scope.scaffolds, function (s) {
                    return contentTypeAliases.indexOf(s.contentTypeAlias);
                });

                // Convert stored nodes
                if ($scope.model.value) {
                    for (var i = 0; i < $scope.model.value.length; i++) {
                        var item = $scope.model.value[i];
                        var scaffold = $scope.getScaffold(item.ncContentTypeAlias);
                        if (scaffold == null) {
                            // No such scaffold - the content type might have been deleted. We need to skip it.
                            continue;
                        }
                        createNode(scaffold, item);
                    }
                }

                // Enforce min items
                if ($scope.nodes.length < $scope.model.config.minItems) {
                    for (var i = $scope.nodes.length; i < $scope.model.config.minItems; i++) {
                        $scope.addNode($scope.scaffolds[0].contentTypeAlias);
                    }
                }

                // If there is only one item, set it as current node
                if ($scope.singleMode || ($scope.nodes.length === 1 && $scope.maxItems === 1)) {
                    $scope.currentNode = $scope.nodes[0];
                }

                inited = true;
                
                checkAbilityToPasteContent();
            }
        }
        
        function createNode(scaffold, fromNcEntry) {
            var node = angular.copy(scaffold);
            
            node.key = fromNcEntry && fromNcEntry.key ? fromNcEntry.key : String.CreateGuid();
            
            for (var v = 0; v < node.variants.length; v++) {
                var variant = node.variants[v];
                
                for (var t = 0; t < variant.tabs.length; t++) {
                    var tab = variant.tabs[t];
                    
                    for (var p = 0; p < tab.properties.length; p++) {
                        var prop = tab.properties[p];
                        
                        prop.propertyAlias = prop.alias;
                        prop.alias = $scope.model.alias + "___" + prop.alias;
                        // Force validation to occur server side as this is the
                        // only way we can have consistency between mandatory and
                        // regex validation messages. Not ideal, but it works.
                        prop.validation = {
                            mandatory: false,
                            pattern: ""
                        };
                        
                        if (fromNcEntry && fromNcEntry[prop.propertyAlias]) {
                            prop.value = fromNcEntry[prop.propertyAlias];
                        }
                    }
                }
            }
            
            $scope.nodes.push(node);

            return node;
        }
        
        function convertNodeIntoNCEntry(node) {
            var obj = {
                key: node.key,
                name: node.name,
                ncContentTypeAlias: node.contentTypeAlias
            };
            for (var t = 0; t < node.variants[0].tabs.length; t++) {
                var tab = node.variants[0].tabs[t];
                for (var p = 0; p < tab.properties.length; p++) {
                    var prop = tab.properties[p];
                    if (typeof prop.value !== "function") {
                        obj[prop.propertyAlias] = prop.value;
                    }
                }
            }
            return obj;
        }
        
        function syncCurrentNode() {
            if ($scope.realCurrentNode) {
                $scope.$broadcast("ncSyncVal", { key: $scope.realCurrentNode.key });
            }
        }
        
        function updateModel() {
            syncCurrentNode();
            
            if (inited) {
                var newValues = [];
                for (var i = 0; i < $scope.nodes.length; i++) {
                    newValues.push(convertNodeIntoNCEntry($scope.nodes[i]));
                }
                $scope.model.value = newValues;
            }
        }

        $scope.$watch("currentNode", function (newVal) {
            updateModel();
            $scope.realCurrentNode = newVal;
        });

        var unsubscribe = $scope.$on("formSubmitting", function (ev, args) {
            updateModel();
        });

        $scope.$on("$destroy", function () {
            unsubscribe();
        });
        
    }

]);
