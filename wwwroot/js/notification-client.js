// wwwroot/js/notification-client.js
"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveNotification", function (notification) {
    // notification object: { title, message, type, severity }
    displayNotification(notification);
});

connection.start().then(function () {
    console.log("SignalR connected.");
    // Join role-based group. The role must be set in a global JS variable.
    if (typeof userRole !== "undefined" && userRole) {
        connection.invoke("JoinGroup", userRole).catch(err => console.error(err));
    }
    // Also join user-specific group if userId exists
    if (typeof userId !== "undefined" && userId) {
        connection.invoke("JoinGroup", userId).catch(err => console.error(err));
    }
}).catch(function (err) {
    console.error(err.toString());
});

function displayNotification(notif) {
    // Use existing toast system (if available) for non-critical messages.
    if (typeof showToast === "function" && notif.severity !== "critical") {
        showToast(notif.message, "success");
    } else {
        // For critical alerts, show a modal or persistent popup.
        // Simple example: create a fixed alert box.
        let alertBox = document.createElement("div");
        alertBox.className = "critical-alert";
        alertBox.innerHTML = `
            <div class="alert-content">
                <strong>${notif.title}</strong>
                <p>${notif.message}</p>
                <button onclick="this.parentElement.parentElement.remove()">Dismiss</button>
            </div>`;
        document.body.appendChild(alertBox);
    }
}