# SignalR Reconnect Strategy

NotificationService exposes the realtime notification hub at:

```text
/hubs/notification
```

This hub is used to push realtime notification updates to authenticated users.

## Authentication

The notification hub requires a valid JWT access token.

Browser clients should pass the token through the SignalR access token factory. The server accepts the token from the `access_token` query string only for the notification hub path.

Example client setup:

```ts
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:PORT/hubs/notification", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .build();
```

Replace `PORT` with the NotificationService HTTPS port used in local development or deployment.

## Reconnect Policy

Clients should use automatic reconnect with the following retry intervals:

```text
0 ms
2 seconds
10 seconds
30 seconds
```

Recommended behavior:

1. Try to reconnect immediately after the connection drops.
2. Retry after 2 seconds.
3. Retry after 10 seconds.
4. Retry after 30 seconds.
5. If all reconnect attempts fail, show a disconnected state in the UI and allow the user to manually reconnect.

Example:

```ts
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:PORT/hubs/notification", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .build();

connection.onreconnecting(error => {
  console.warn("SignalR reconnecting.", error);
  // Update UI state: realtime connection is reconnecting.
});

connection.onreconnected(connectionId => {
  console.info("SignalR reconnected.", connectionId);
  // Update UI state: realtime connection is connected again.
});

connection.onclose(error => {
  console.error("SignalR connection closed.", error);
  // Update UI state: realtime connection is disconnected.
  // Show manual reconnect action.
});
```

## Server Events

The client should subscribe to these server-to-client events:

```text
NotificationCreated
UnreadCountUpdated
```

### NotificationCreated

Raised when a new notification is created for the authenticated user.

Example:

```ts
connection.on("NotificationCreated", notification => {
  // Add notification to the UI.
  // Example fields:
  // notification.id
  // notification.userId
  // notification.type
  // notification.message
  // notification.isRead
  // notification.createdAt
});
```

### UnreadCountUpdated

Raised when the authenticated user's unread notification count changes.

Example:

```ts
connection.on("UnreadCountUpdated", unreadCount => {
  // Update unread notification badge.
});
```

## Connection Lifecycle

When a client connects, the server:

1. Reads the authenticated user id from the JWT claims.
2. Tracks the SignalR connection id in memory.
3. Adds the connection to a user-specific SignalR group.

The group name format is:

```text
user:{userId}
```

When a client disconnects, the server:

1. Removes the connection id from the in-memory connection manager.
2. Removes the connection from the `user:{userId}` SignalR group.

## Multi-device Behavior

A single user can have multiple active SignalR connections.

Examples:

```text
same user on desktop browser
same user on mobile browser
same user on another tab
```

The server sends realtime events to the user group:

```text
user:{userId}
```

Because all active connections for that user are in the same group, every connected device receives the same realtime notification events.

## Token Expiration

If the access token expires, the SignalR connection can fail or close with an unauthorized state.

Recommended client flow:

1. Detect reconnect failure or unauthorized close.
2. Call the auth refresh endpoint.
3. Get a new access token.
4. Restart the SignalR connection with the new token.
5. If refresh fails, redirect the user to login.

Example flow:

```ts
async function restartNotificationConnection() {
  try {
    await connection.stop();

    const refreshedToken = await refreshAccessToken();

    accessToken = refreshedToken;

    await connection.start();
  } catch (error) {
    console.error("Failed to restart SignalR connection.", error);
    // Redirect to login or show disconnected state.
  }
}
```

## Startup Flow

The frontend should start the SignalR connection after the user is authenticated and an access token is available.

Example:

```ts
async function startNotificationConnection() {
  try {
    await connection.start();
    console.info("SignalR connected.");
  } catch (error) {
    console.error("SignalR connection failed.", error);
    // Retry manually or show a disconnected state.
  }
}
```

## Backend Endpoint

NotificationService maps the hub endpoint at:

```text
/hubs/notification
```

Local development example:

```text
https://localhost:PORT/hubs/notification
```

The client should not call this endpoint with normal HTTP requests. It should connect using the SignalR client.

## Notes

- The hub requires JWT authentication.
- The server accepts `access_token` from query string only for `/hubs/notification`.
- Notifications are pushed with the `NotificationCreated` event.
- Unread count updates are pushed with the `UnreadCountUpdated` event.
- Reconnect behavior is handled by the frontend SignalR client.
- Server-side connection tracking is currently in-memory.
- For multi-instance deployments, connection tracking and SignalR scale-out should use Redis backplane in a later sprint.