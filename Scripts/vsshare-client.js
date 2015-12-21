(function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);throw new Error("Cannot find module '"+o+"'")}var f=n[o]={exports:{}};t[o][0].call(f.exports,function(e){var n=t[o][1][e];return s(n?n:e)},f,f.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s})({1:[function(require,module,exports){
/// <reference path="../../typings/signalr/signalr.d.ts" />
/// <reference path="../../typings/vsshare/typings/vsshare/vsshare.d.ts" />
'use strict';
var room_1 = require('./room');
$(function () {
    window["VSShareClient"] = VSShareClient;
});
var SignalRConnectionStatus;
(function (SignalRConnectionStatus) {
    SignalRConnectionStatus[SignalRConnectionStatus["Connecting"] = 0] = "Connecting";
    SignalRConnectionStatus[SignalRConnectionStatus["Connected"] = 1] = "Connected";
    SignalRConnectionStatus[SignalRConnectionStatus["Reconnecting"] = 2] = "Reconnecting";
    SignalRConnectionStatus[SignalRConnectionStatus["Disconnected"] = 3] = "Disconnected";
})(SignalRConnectionStatus || (SignalRConnectionStatus = {}));
;
var VSShareStatus;
(function (VSShareStatus) {
    VSShareStatus[VSShareStatus["Disconnected"] = 0] = "Disconnected";
    VSShareStatus[VSShareStatus["Connecting"] = 1] = "Connecting";
    VSShareStatus[VSShareStatus["Connected"] = 2] = "Connected";
    VSShareStatus[VSShareStatus["Authorized"] = 3] = "Authorized";
})(VSShareStatus || (VSShareStatus = {}));
var VSShareClient = (function () {
    function VSShareClient(url, hubName) {
        var _this = this;
        this._status = VSShareStatus.Disconnected;
        this._connectionStatus = SignalRConnectionStatus.Disconnected;
        this._url = url;
        this._hubName = hubName;
        this._room = new room_1["default"]();
        window.addEventListener("resize", function () { _this.refleshView(_this._room); }, false);
    }
    VSShareClient.prototype.startConnection = function (token) {
        var _this = this;
        this.disposeConnection();
        var self = this;
        this._connection = $.hubConnection(this._url);
        this._connection.stateChanged(function (status) {
            self.changeStatus(status.newState);
        });
        this._hub = this._connection.createHubProxy(this._hubName);
        // イベントの登録
        this._hub.on("UpdateRoomStatus", function (item) {
            self._room.updateRoomStatus(item);
        });
        this._hub.on("UpdateSessionInfo", function (item) {
            self._room.updateSessionInfo(item);
        });
        this._hub.on("AppendSession", function (item) {
            self._room.appendSession(item);
            _this._hub.invoke("GetSessionContent", { id: item.id }).done(function (res) {
                self._room.updateSessionContent(res);
            });
        });
        this._hub.on("RemoveSession", function (item) {
            self._room.removeSession(item);
            self.switchOnline(self._room.getSessionCount() > 0);
        });
        this._hub.on("UpdateSessionContent", function (item) {
            self._room.updateSessionContent(item);
        });
        this._hub.on("UpdateSessionCursor", function (item) {
            self._room.updateSessionCursor(item);
        });
        this._connection.start().done(function () {
            // 認証を行う
            console.log("Connected to " + self._hubName + " on " + self._url + ".");
            self._status = VSShareStatus.Connected;
            self.authorize(token);
        }).fail(function (err) {
            // ユーザーへ何らかの手段で通知
            console.error("Failed to connect " + self._hubName + " on " + self._url + ".");
            self.disposeConnection();
        });
    };
    VSShareClient.prototype.authorize = function (token) {
        var _this = this;
        var self = this;
        var item = { "token": token };
        this._hub.invoke("Authorize", item).done(function (res) {
            var response = res;
            if (response && response.success) {
                // 認証成功
                self._status = VSShareStatus.Authorized;
                _this._hub.invoke("GetRoomStatus").done(function (res) {
                    if (res != null) {
                        self._room.updateRoomStatus(res);
                    }
                });
                _this._hub.invoke("GetSessionList").done(function (res) {
                    _this.switchOnline(res.length > 0);
                    res.forEach(function (value, index, array) {
                        self._room.appendSession(value);
                        _this._hub.invoke("GetSessionContent", { id: value.id }).done(function (res) {
                            self._room.updateSessionContent(res);
                        });
                        _this._hub.invoke("GetSessionCursor", { id: value.id }).done(function (res) {
                            self._room.updateSessionCursor(res);
                        });
                    });
                });
            }
            else {
                // エラー
                console.error("Failed to authorize hub with token: " + token);
                self.disposeConnection();
            }
        }).fail(function (err) {
            console.error("Failed to invoke authorize method");
            self.disposeConnection();
        });
    };
    VSShareClient.prototype.changeStatus = function (status) {
        switch (status) {
            case SignalRConnectionStatus.Disconnected:
                this._status = VSShareStatus.Disconnected; // この時だけは反映
                break;
        }
    };
    VSShareClient.prototype.disposeConnection = function () {
        // 終了処理
        if (this._connection) {
            this._connection.stop();
        }
    };
    VSShareClient.prototype.dispose = function () {
    };
    VSShareClient.prototype.refleshView = function (room) {
        room.updateViewSize();
    };
    VSShareClient.prototype.switchOnline = function (isOnline) {
        var status = document.getElementById("broadcast-status");
        if (status == null)
            return;
        if (isOnline) {
            status.innerHTML = "ONLINE";
            status.style.color = "red";
        }
        else {
            status.innerHTML = "OFFLINE";
            status.style.color = "";
        }
    };
    return VSShareClient;
})();
exports.VSShareClient = VSShareClient;

},{"./room":2}],2:[function(require,module,exports){
/// <reference path="../../typings/vsshare/typings/vsshare/vsshare.d.ts" />
/// <reference path="../../typings/goldenlayout/goldenlayout.d.ts" />
'use strict';
var session_1 = require('./session');
var Room = (function () {
    function Room() {
        this._sessions = {};
        this._containers = {};
        this._myLayout = new GoldenLayout({ content: [], settings: { showPopoutIcon: false } }, $('#golden-layout'));
        this._myLayout.registerComponent('file', function (container, state) {
            container.getElement().html("<pre class=\"code\" id=\"code-" + state.id + "\"></pre>");
            container.setTitle(state.self.getShortFileName(state.filename));
            state.self._containers[state.id] = container;
            //container.tab.elements[0].title = state.filename
            state.session.setEditor(container.getElement()[0]);
        });
        this._myLayout.init();
    }
    Room.prototype.appendSession = function (item) {
        var id = item.id;
        var filename = item.filename;
        var type = item.type;
        var owner = item.owner_name;
        if (this._sessions[id]) {
            console.error("Room already contains session id: " + id);
        }
        this._sessions[id] = new session_1["default"](id, filename, type, owner);
        var component = {
            type: 'row',
            content: [{
                    type: 'component',
                    componentName: "file",
                    isClosable: false,
                    componentState: { id: id, filename: filename, session: this._sessions[id], self: this }
                }] };
        if (!this._myLayout.root.contentItems.length) {
            this._myLayout.root.addChild(component);
        }
        else {
            this._myLayout.root.contentItems[0].addChild(component);
        }
    };
    Room.prototype.removeSession = function (item) {
        var id = item.id;
        this._sessions[id].close();
        delete this._sessions[id];
        this._containers[id].close();
    };
    Room.prototype.updateRoomStatus = function (item) {
        this._viewCount = item.view;
        this._visitorCount = item.visitor;
        this.updateRoomStatusView();
    };
    Room.prototype.updateRoomStatusView = function () {
        document.getElementById('viewer-count').innerHTML = this._viewCount.toString();
        document.getElementById('totalview-count').innerHTML = this._visitorCount.toString();
    };
    Room.prototype.updateViewSize = function () {
        var gl = document.getElementById("golden-layout");
        this._myLayout.updateSize(gl.clientWidth, gl.clientHeight);
    };
    Room.prototype.updateSessionInfo = function (item) {
        var session = this._sessions[item.id];
        if (session == null) {
            console.error("Room doesn't contain session id: " + item.id);
            return;
        }
        session.updateSessionInfo(item);
        this._containers[item.id].setTitle(this.getShortFileName(item.filename));
    };
    Room.prototype.updateSessionContent = function (item) {
        this._sessions[item.id].updateContent(item);
    };
    Room.prototype.updateSessionCursor = function (item) {
        this._sessions[item.id].updateCursor(item);
    };
    Room.prototype.getShortFileName = function (filename) {
        return filename ? filename.split(/\/|\\/).pop() : "";
    };
    Room.prototype.getSessionCount = function () {
        return Object.keys(this._sessions).length;
    };
    return Room;
})();
exports.__esModule = true;
exports["default"] = Room;

},{"./session":3}],3:[function(require,module,exports){
/// <reference path="../../typings/vsshare/typings/vsshare/vsshare.d.ts" />
/// <reference path="../../typings/ace/ace.d.ts" />
'use strict';
var StyleClass;
(function (StyleClass) {
    StyleClass.Modified = "modified";
    StyleClass.GutterActiveLine = "ace_gutter-real-active-line";
    StyleClass.Cursor = "ace_real-cursor";
    StyleClass.ActiveLine = "ace_real-active-line";
    StyleClass.Selection = "ace_real-selection";
})(StyleClass || (StyleClass = {}));
var Session = (function () {
    function Session(id, filename, type, owner) {
        this._isModified = [];
        this.Range = ace.require('./range').Range;
        this._id = id;
        this._filename = filename;
        this._type = type;
        this._owner = owner;
        this._noLine = true;
    }
    Session.prototype.setEditor = function (element) {
        this._editor = ace.edit(element.querySelector("#code-" + this._id));
        this.setEditorMode(this._type);
        this._editor.setReadOnly(true);
        this._editor.setOption("maxLines", (element.clientHeight) / this._editor.renderer.layerConfig.lineHeight);
        this._doc = this._editor.session.doc;
        var cursorMarker = {}, lineMarker = {};
        var self = this;
        cursorMarker["update"] = function (html, marker, session, config) { self.updateCursorMarker(html, marker, session, config, self); };
        lineMarker["update"] = function (html, marker, session, config) { self.updateLineMarker(html, marker, session, config, self); };
        this._editor.session.addDynamicMarker(cursorMarker, true);
        this._editor.session.addDynamicMarker(lineMarker, false);
    };
    Session.prototype.replaceLines = function (lines, row, lineLength) {
        var range = new this.Range(row, 0, row + lineLength - 1, this._doc.getLine(row + lineLength - 1).length);
        var linesStr = [];
        this._isModified.splice(row, lineLength);
        var currentRow = row;
        for (var i = 0; i < lines.length; i++) {
            var count = this._doc.$split(lines[i].text).length;
            for (var j = 0; j < count; j++) {
                this._isModified.splice(currentRow + j, 0, lines[i].modified);
            }
            currentRow += count;
            linesStr.push(lines[i].text);
        }
        this.updateModifiedLines();
        this._doc.replace(range, linesStr.join("\n"));
    };
    Session.prototype.insertLines = function (lines, row) {
        var textlines = [];
        var currentRow = row;
        for (var i = 0; i < lines.length; i++) {
            var count = this._doc.$split(lines[i].text).length;
            textlines.concat(this._doc.$split(lines[i].text));
            for (var j = 0; j < count; j++) {
                this._isModified.splice(currentRow + j, 0, lines[i].modified);
            }
            currentRow += count;
        }
        this.updateModifiedLines();
        this._doc.insertFullLines(row, textlines);
    };
    Session.prototype.removeLines = function (startRow, lineLength) {
        this._isModified.splice(startRow, lineLength);
        this.updateModifiedLines();
        this._doc.removeFullLines(startRow, startRow + lineLength - 1);
    };
    Session.prototype.removeAllModifiedMarkers = function () {
        var txtlen = this._doc.getLength();
        this._isModified.splice(txtlen, this._isModified.length - txtlen);
        for (var i = 0; i < this._isModified.length; i++) {
            this._isModified[i] = false;
        }
        this.updateModifiedLines();
    };
    Session.prototype.updateModifiedLines = function () {
        for (var i = 0; i < this._isModified.length; i++) {
            if (this._isModified[i]) {
                if (!this._editor.session.$decorations[i] || !this._editor.session.$decorations[i].match("^" + StyleClass.Modified + "| " + StyleClass.Modified + " |" + StyleClass.Modified + "$")) {
                    this._editor.session.addGutterDecoration(i, StyleClass.Modified);
                }
            }
            else {
                this._editor.session.removeGutterDecoration(i, StyleClass.Modified);
            }
        }
    };
    Session.prototype.dispose = function () {
        // 終了処理
    };
    Session.prototype.updateContent = function (item) {
        var _this = this;
        item.data.sort(function (a, b) {
            return a.order - b.order;
        });
        item.data.forEach(function (d) {
            var pos = d.pos;
            switch (d.type) {
                case 3 /* Append */:
                    pos = _this._doc.getLength();
                case 0 /* Insert */:
                    if (_this._noLine) {
                        _this.replaceLines(d.data, 0, 1);
                        _this._noLine = false;
                    }
                    else {
                        _this.insertLines(d.data, pos);
                    }
                    break;
                case 1 /* Delete */:
                    _this.removeLines(d.pos, d.len);
                    _this._noLine = false;
                    break;
                case 2 /* Replace */:
                    _this.replaceLines(d.data, d.pos, d.len);
                    _this._noLine = false;
                    break;
                case 5 /* ResetAll */:
                    _this.removeLines(0, _this._doc.getLength());
                    _this._noLine = true;
                case 4 /* RemoveMarker */:
                    _this.removeAllModifiedMarkers();
                    break;
            }
        });
    };
    Session.prototype.updateCursor = function (item) {
        if (!this._cursorPos) {
            this._editor.session.addGutterDecoration(item.active.line, StyleClass.GutterActiveLine);
        }
        else if (this._cursorPos.line != item.active.line) {
            this._editor.session.removeGutterDecoration(this._cursorPos.line, StyleClass.GutterActiveLine);
            this._editor.session.addGutterDecoration(item.active.line, StyleClass.GutterActiveLine);
        }
        this._cursorPos = item.active;
        switch (item.type) {
            case 0 /* Point */:
                this._selectionStartPos = undefined;
                break;
            case 1 /* Select */:
                this._selectionStartPos = item.anchor;
                break;
        }
        this._editor.renderer.updateFrontMarkers();
        this._editor.renderer.updateBackMarkers();
    };
    Session.prototype.scrollWithCursor = function (activeCursorPosition) {
        var range = this._editor.getSelectionRange();
        if (range.start.row != range.end.row && range.start.column != range.end.column) {
            return;
        }
        var maxLines = this._editor.getOption("maxLines");
        var currentRow = this._editor.getFirstVisibleRow();
        if (activeCursorPosition.line < currentRow) {
        }
        else if (activeCursorPosition.line > currentRow + maxLines) {
        }
    };
    Session.prototype.updateCursorMarker = function (html, marker, session, config, self) {
        if (!self._cursorPos) {
            return;
        }
        var span = document.createElement("span");
        span.style.visibility = "hidden";
        document.body.appendChild(span);
        span.className = "ace_editor";
        var leftCount = 0;
        for (var i = 0; i < self._cursorPos.pos; i++) {
            span.innerHTML = this._editor.session.getLine(self._cursorPos.line)[i].replace(/ /g, "A").replace(/\t/g, "AAAA".substring(0, 4 - leftCount % 4));
            leftCount += Math.round(span.offsetWidth / config.characterWidth);
        }
        var left = config.padding + leftCount * config.characterWidth;
        document.body.removeChild(span);
        var top = config.lineHeight * self._cursorPos.line;
        var width = config.characterWidth;
        var height = config.lineHeight;
        html.push("<div class=\"" + StyleClass.Cursor + "\" style=\"left: " + left + "px; top: " + top + "px; widht: " + width + "px; height: " + height + "px\"></div>");
    };
    Session.prototype.updateLineMarker = function (html, marker, session, config, self) {
        if (!self._cursorPos) {
            return;
        }
        if (self._selectionStartPos) {
            var pos1, pos2;
            if (self._cursorPos.line == self._selectionStartPos.line) {
                if (self._cursorPos.pos < self._selectionStartPos.pos) {
                    pos1 = self._cursorPos;
                    pos2 = self._selectionStartPos;
                }
                else {
                    pos1 = self._selectionStartPos;
                    pos2 = self._cursorPos;
                }
            }
            else if (self._cursorPos.line < self._selectionStartPos.line) {
                pos1 = self._cursorPos;
                pos2 = self._selectionStartPos;
            }
            else {
                pos1 = self._selectionStartPos;
                pos2 = self._cursorPos;
            }
            var span = document.createElement("span");
            span.style.visibility = "hidden";
            document.body.appendChild(span);
            span.className = "ace_editor";
            var leftCount = 0;
            for (var i = 0; i < pos1.pos; i++) {
                span.innerHTML = this._editor.session.getLine(pos1.line)[i].replace(/ /g, "A").replace(/\t/g, "AAAA".substring(0, 4 - leftCount % 4));
                leftCount += Math.round(span.offsetWidth / config.characterWidth);
            }
            var left = leftCount * config.characterWidth;
            var rightCount = 0;
            for (var i = 0; i < pos2.pos; i++) {
                span.innerHTML = this._editor.session.getLine(pos2.line)[i].replace(/ /g, "A").replace(/\t/g, "AAAA".substring(0, 4 - rightCount % 4));
                rightCount += Math.round(span.offsetWidth / config.characterWidth);
            }
            var right = rightCount * config.characterWidth;
            document.body.removeChild(span);
            var top1 = config.lineHeight * pos1.line;
            var top2 = config.lineHeight * pos2.line;
            if (pos1.line == pos2.line) {
                html.push("<div class=\"" + StyleClass.Selection + "\" style=\"left: " + (config.padding + left) + "px; width: " + (right - left) + "px; top: " + top1 + "px; height: " + config.lineHeight + "px\"></div>");
            }
            else {
                html.push("<div class=\"" + StyleClass.Selection + "\" style=\"left: " + (config.padding + left) + "px; right: " + config.padding + "px; top: " + top1 + "px; height: " + config.lineHeight + "px\"></div>\n\t\t\t\t\t<div class=\"" + StyleClass.Selection + "\" style=\"left: " + config.padding + "px; right: " + config.padding + "px; top: " + (top1 + config.lineHeight) + "px; height: " + config.lineHeight * (pos2.line - pos1.line - 1) + "px\"></div>\n\t\t\t\t\t<div class=\"" + StyleClass.Selection + "\" style=\"left: " + config.padding + "px; width: " + right + "px; top: " + top2 + "px; height: " + config.lineHeight + "px\"></div>");
            }
        }
        else {
            var top = config.lineHeight * self._cursorPos.line;
            var height = config.lineHeight;
            html.push("<div class=\"" + StyleClass.ActiveLine + "\" style=\"left: 0; right: 0; top: " + top + "px; height: " + height + "px\"></div>");
        }
    };
    Session.prototype.updateSessionInfo = function (item) {
        this._filename = item.filename;
        this._type = item.type;
        this.setEditorMode(item.type);
    };
    Session.prototype.setEditorMode = function (type) {
        var mode;
        switch (type) {
            case "code:csharp":
                mode = "ace/mode/csharp";
                break;
            case "code:json":
                mode = "ace/mode/json";
                break;
            default:
                mode = "ace/mode/plain_text";
                break;
        }
        console.log(type);
        this._editor.session.setMode(mode);
    };
    Session.prototype.close = function () {
    };
    Session.prototype.hasNoLine = function () {
        return this._noLine;
    };
    return Session;
})();
exports.__esModule = true;
exports["default"] = Session;

},{}]},{},[1])