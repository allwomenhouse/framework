import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import * as signalR from '@microsoft/signalr'
import * as AuthClient from '../Authorization/AuthClient'
import { HubConnectionState } from '@microsoft/signalr'
import { useForceUpdate } from '../../Signum.React/Scripts/Hooks';

//Orifinally from https://github.com/pguilbert/react-use-signalr/

export function useSignalRConnection(url: string, options?: signalR.IHttpConnectionOptions) {

  const connection = React.useMemo<signalR.HubConnection | undefined>(() => {

    var connection = new signalR.HubConnectionBuilder()
      .withUrl(AppContext.toAbsoluteUrl(url), options ?? {})
      .withAutomaticReconnect()
      .build();
    return connection;
  }, [url]);

  const forceUpdate = useForceUpdate();

  React.useEffect(() => {

    if (!connection) {
      return;
    }

    let isMounted = true;
    const updateState = () => {
      if (isMounted) {
        forceUpdate();
      }
    }
    connection.onclose(updateState);
    connection.onreconnected(updateState);
    connection.onreconnecting(updateState);

    if (connection.state === signalR.HubConnectionState.Disconnected) {
      const promise = connection
        .start()
        .then(() => { forceUpdate() });

      forceUpdate();

      promise.done();

      return () => {
        promise
          .then(() => {
            connection.stop();
          });
        isMounted = false;
      };
    }

    return () => {
      connection.stop();
    };
  }, [connection]);

  return connection;
}

export function useSignalRGroup(connection: signalR.HubConnection | undefined, options: {
  enterGroup: (connection: signalR.HubConnection) => Promise<void>,
  exitGroup: (connection: signalR.HubConnection) => Promise<void>,
  deps: any[]
}) {

  React.useEffect(() => {

    if (connection?.state == signalR.HubConnectionState.Connected) {
      options.enterGroup(connection).done();

      return () => {
        if (connection?.state == signalR.HubConnectionState.Connected) {
          options.exitGroup(connection).done();
        }
      };
    }
  }, [connection, connection?.state, ...options.deps]);
}

export function useSignalRCallback(connection: signalR.HubConnection | undefined, methodName: string, callback: (...args: any[]) => void, deps: any[]) {

  var callback = React.useCallback(callback, deps);

  React.useEffect(() => {
    if (!connection || connection.state != HubConnectionState.Connected) {
      return;
    }

    connection.on(methodName, callback);

    return () => {
      connection.off(methodName, callback);
    }

  }, [connection, connection?.state, callback, methodName]);
}
