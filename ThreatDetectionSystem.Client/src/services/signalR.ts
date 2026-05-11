import * as signalR from '@microsoft/signalr';
import type { Alert } from '../types';

let connection: signalR.HubConnection | null = null;

export const initSignalR = (onNewAlerts: (alerts: Alert[]) => void) => {
  if (connection?.state === signalR.HubConnectionState.Connected) {
    return connection;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5045/alertHub", {
      withCredentials: true
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.on("ReceiveAlerts", (alerts: Alert[]) => {
    console.log(`🔔 Received ${alerts.length} alerts`);
    onNewAlerts(alerts);
  });

  connection.onreconnecting(() => {
    console.log("⚠️ Reconnecting SignalR...");
  });

  connection.onreconnected(() => {
    console.log("✅ Reconnected");
  });

  connection.onclose(() => {
    console.log("❌ Disconnected");
    connection = null;
  });

  connection.start()
    .then(() => console.log("✅ SignalR Connected"))
    .catch(err => console.error("❌ SignalR Failed:", err));

  return connection;
};