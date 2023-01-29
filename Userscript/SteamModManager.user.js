// ==UserScript==
// @name         Steam Mod Manager
// @namespace    http://tampermonkey.net/
// @version      0.1
// @description  Mod Downloader Extension
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
    const id = document.getElementById('PublishedFileSubscribe').elements['id'].value;
    // Query selectors
    var subscribeItemButton = document.getElementById("SubscribeItemBtn");
    var subscribeItemOptionAdd = document.getElementById('SubscribeItemOptionAdd');
    var subscribeItemOptionSubscribed = document.getElementById('SubscribeItemOptionSubscribed');
    var actionWait = document.getElementById('action_wait');
    // HTML injections
    subscribeItemButton.removeAttribute('onclick');
    subscribeItemOptionAdd.innerHTML = 'Add';
    subscribeItemOptionSubscribed.innerHTML = 'In database';
    subscribeItemOptionSubscribed.nextSibling.nextSibling.innerHTML = 'Remove';
    // Check if item is already in database
    fetch(url, {
        method: 'POST',
        header: {
            "Content-Type": "application/json",
            Accept: "application/json",
        },
        body: JSON.stringify({'action':'query','id':id}),
    })
    .then(response => response.text())
    .then((response) => {
        console.log(response);
        if (response === 'true') {
            subscribeItemButton.classList.add('toggled');
            subscribeItemOptionAdd.className = 'subscribeOption add';
            subscribeItemOptionSubscribed.className = 'subscribeOption subscribed selected';
        }
    }).catch(error => console.log(error));
    // Button click event
    subscribeItemButton.addEventListener('click', function() {
        if (!subscribeItemButton.classList.contains('toggled')) {
            actionWait.style.display = 'block';
            fetch(url, {
                method: 'POST',
                header: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                },
                body: JSON.stringify({'action':'add','id':id}),
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
                body: JSON.stringify({'action':'remove','id':id}),
            })
            .then((response) => {
                subscribeItemButton.classList.remove('toggled');
                subscribeItemOptionAdd.className = 'subscribeOption add selected';
                subscribeItemOptionSubscribed.className = 'subscribeOption subcribed';
                actionWait.style.display = 'none';
            }).catch(error => console.log(error));
        }
    });
})();