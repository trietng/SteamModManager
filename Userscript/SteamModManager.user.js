// ==UserScript==
// @name         Steam Mod Manager
// @namespace    http://tampermonkey.net/
// @version      0.2
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
    const title = document.getElementsByClassName('workshopItemTitle')[0].innerText;
    // Query selectors
    var subscribeItemButton = document.getElementById("SubscribeItemBtn");
    var subscribeItemOptionAdd = document.getElementById('SubscribeItemOptionAdd');
    var subscribeItemOptionSubscribed = document.getElementById('SubscribeItemOptionSubscribed');
    var subscribeIcon = document.getElementsByClassName('subscribeIcon')[0];
    var actionWait = document.getElementById('action_wait');
    // HTML injections
    subscribeItemButton.removeAttribute('onclick');
    subscribeItemOptionAdd.innerHTML = 'Add';
    subscribeItemOptionSubscribed.innerHTML = 'In database';
    subscribeItemOptionSubscribed.nextSibling.nextSibling.innerHTML = 'Remove';
    // Check if item is already in database
    // need to be done synchronously
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url, false);
    xhr.setRequestHeader("Content-Type", "application/json");
    try {
        xhr.send(JSON.stringify({'action':'query','id':id,'title':title}));
    }
    catch (error) {

    }
    if (xhr.status === 200) {
        subscribeItemButton.classList.add('toggled');
        subscribeItemOptionAdd.className = 'subscribeOption add';
        subscribeItemOptionSubscribed.className = 'subscribeOption subscribed selected';
    }
    else {
        subscribeItemButton.classList.remove('btn_green_white_innerfade');
        subscribeItemButton.classList.add('btnv6_white_transparent');
        subscribeIcon.setAttribute('style', 'background-image: none !important;');
        subscribeItemOptionAdd.innerHTML = 'Disabled';
        return;
    }
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
                body: JSON.stringify({'action':'add','id':id,'title':title}),
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
                body: JSON.stringify({'action':'remove','id':id,'title':title}),
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