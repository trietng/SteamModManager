// ==UserScript==
// @name         Steam Mod Manager Extension
// @namespace    http://tampermonkey.net/
// @version      0.3
// @description  Extension
// @author       trietng
// @match        https://steamcommunity.com/sharedfiles/filedetails/?id=*
// @match        https://steamcommunity.com/workshop/filedetails/?id=*
// @icon         https://steamcommunity.com/favicon.ico
// @grant        none
// ==/UserScript==

(function() {
    'use strict';
    // Consts
    const url = 'http://127.0.0.1:27060';
    const idGlobal = document.getElementById('PublishedFileSubscribe').elements['id'].value;
    const titleGlobal = document.getElementsByClassName('workshopItemTitle')[0].innerText;
    // Check if item is a collection
    const publishedFileCollectionSubscribe = document.getElementById('PublishedFileCollectionSubscribe');
    if (publishedFileCollectionSubscribe === null) {
        // Query selectors for item
        var subscribeItemButton = document.getElementById("SubscribeItemBtn");
        var subscribeItemOptionAdd = document.getElementById('SubscribeItemOptionAdd');
        var subscribeItemOptionSubscribed = document.getElementById('SubscribeItemOptionSubscribed');
        // Disable button mouse event
        subscribeItemButton.setAttribute('style', 'pointer-events: none !important');
        // HTML injections
        subscribeItemOptionAdd.innerHTML = 'Add';
        subscribeItemOptionSubscribed.innerHTML = 'In database';
        subscribeItemOptionSubscribed.nextSibling.nextSibling.innerHTML = 'Remove';
        // Check if item is already in database
        // need to be done synchronously
        var xhrItem = new XMLHttpRequest();
        xhrItem.open('POST', url, false);
        xhrItem.setRequestHeader("Content-Type", "application/json");
        try {
            xhrItem.send(JSON.stringify({ 'action': 'query', 'id': idGlobal, 'title': titleGlobal, 'collection': false }));
        }
        catch (error) {

        }
        if (xhrItem.status === 200) {
            if (xhrItem.responseText === 'true') {
                subscribeItemButton.classList.add('toggled');
                subscribeItemOptionAdd.className = 'subscribeOption add';
                subscribeItemOptionSubscribed.className = 'subscribeOption subscribed selected';
            }
        }
        else {
            window.SubscribeItem = function() {};
            subscribeItemButton.classList.remove('btn_green_white_innerfade');
            subscribeItemButton.classList.add('btnv6_white_transparent');
            subscribeItemOptionAdd.innerHTML = 'Disabled';
            document.getElementsByClassName('subscribeIcon')[0].setAttribute('style', 'background-image: none !important;');
            subscribeItemButton.removeAttribute('style');
            return;
        }
        // Button click event
        window.SubscribeItem = function () {
            var actionWait = document.getElementById('action_wait');
            if (!subscribeItemButton.classList.contains('toggled')) {
                actionWait.style.display = 'block';
                fetch(url, {
                    method: 'POST',
                    header: {
                        "Content-Type": "application/json",
                        Accept: "application/json",
                    },
                    body: JSON.stringify({ 'action': 'add', 'id': idGlobal, 'title': titleGlobal, 'collection': false }),
                })
                .then((response) => {
                    subscribeItemButton.classList.add('toggled');
                    subscribeItemOptionAdd.className = 'subscribeOption add';
                    subscribeItemOptionSubscribed.className = 'subscribeOption subscribed selected';
                    actionWait.style.display = 'none';
                }).catch(error => console.log(error));
            }
            else {
                actionWait.style.display = 'block';
                fetch(url, {
                    method: 'POST',
                    header: {
                        "Content-Type": "application/json",
                        Accept: "application/json",
                    },
                    body: JSON.stringify({ 'action': 'remove', 'id': idGlobal, 'title': titleGlobal, 'collection': false }),
                })
                .then((response) => {
                    subscribeItemButton.classList.remove('toggled');
                    subscribeItemOptionAdd.className = 'subscribeOption add selected';
                    subscribeItemOptionSubscribed.className = 'subscribeOption subcribed';
                    actionWait.style.display = 'none';
                }).catch(error => console.log(error));
            }
        }
        // Re-enable button mouse event
        subscribeItemButton.removeAttribute('style');
    }
    else {
        var detailBox = document.getElementsByClassName('detailBox')[1];
        detailBox.setAttribute('style', 'pointer-events: none !important');
        // Check if collection is already in database
        // need to be done synchronously
        var xhrCollection = new XMLHttpRequest();
        xhrCollection.open('POST', url, false);
        xhrCollection.setRequestHeader("Content-Type", "application/json");
        try {
            xhrCollection.send(JSON.stringify({ 'action': 'query', 'id': idGlobal, 'title': titleGlobal, 'collection': true }));
        }
        catch (error) {

        }
        if (xhrCollection.status === 200) {
            if (xhrCollection.responseText.length > 0) {
                var array = JSON.parse(xhrCollection.responseText);
                for (let item of array) {
                    document.getElementById('SubscribeItemBtn' + item).className = 'general_btn subscribe toggled';
                }
            }
        }
        else {
            var buttons = document.getElementsByClassName('general_btn subscribe');
            for (let button of buttons) {
                button.removeAttribute('onclick');
                button.setAttribute('style', 'background-image: none !important;');
                button.children[0].setAttribute('style', 'background-image: none !important;');
            }
            buttons[0].children[1].innerHTML = 'Disabled';
            buttons[1].children[1].innerHTML = 'Disabled';
            detailBox.removeAttribute('style');
            return;
        }
        // Script injection
        window.SubscribeCollection = function () {
            let actionWait = document.getElementById('action_wait');
            actionWait.style.display = 'block';
            fetch(url, {
                method: 'POST',
                header: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                },
                body: JSON.stringify({ 'action': 'add', 'id': idGlobal, 'title': titleGlobal, 'collection': true }),
            })
            .then(response => response.text())
            .then((response) => {
                var array = JSON.parse(response);
                for (var item of array) {
                    document.getElementById('SubscribeItemBtn' + item).className = 'general_btn subscribe toggled';
                }
                actionWait.style.display = 'none';
            }).catch(error => console.log(error));
        }
        window.UnsubscribeCollection = function () {
            let actionWait = document.getElementById('action_wait');
            actionWait.style.display = 'block';
            fetch(url, {
                method: 'POST',
                header: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                },
                body: JSON.stringify({ 'action': 'remove', 'id': idGlobal, 'title': titleGlobal, 'collection': true }),
            })
            .then(response => response.text())
            .then((response) => {
                var array = JSON.parse(response);
                for (var item of array) {
                    document.getElementById('SubscribeItemBtn' + item).className = 'general_btn subscribe';
                }
                actionWait.style.display = 'none';
            }).catch(error => console.log(error));
        }
        window.SubscribeCollectionItem = function (id, appID) {
            // Query selectors
            let actionWaitId = document.getElementById('action_wait_' + id);
            let sharedfileId = document.getElementById('sharedfile_' + id);
            let subscribeItemButtonId = sharedfileId.children[2].children[1];
            // Get title of item
            const title = sharedfileId.children[1].children[0].children[0].innerHTML;
            // Toggle
            if (!subscribeItemButtonId.classList.contains('toggled')) {
                actionWaitId.style.display = 'block';
                fetch(url, {
                    method: 'POST',
                    header: {
                        "Content-Type": "application/json",
                        Accept: "application/json",
                    },
                    body: JSON.stringify({ 'action': 'add', 'id': id, 'title': title, 'collection': false}),
                })
                .then((response) => {
                    subscribeItemButtonId.className = 'general_btn subscribe toggled';
                    actionWaitId.style.display = 'none';
                }).catch(error => console.log(error));
            }
            else {
                actionWaitId.style.display = 'block';
                fetch(url, {
                    method: 'POST',
                    header: {
                        "Content-Type": "application/json",
                        Accept: "application/json",
                    },
                    body: JSON.stringify({ 'action': 'remove', 'id': id, 'title': title, 'collection': false }),
                })
                .then((response) => {
                    subscribeItemButtonId.className = 'general_btn subscribe';
                    actionWaitId.style.display = 'none';
                }).catch(error => console.log(error));
            }
        }
        detailBox.removeAttribute('style');
    }
})();
