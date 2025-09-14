using UnityEngine;
using Mirror;
using Unity.Networking.Transport;
using Unity.Collections;
using System;

public class UnityTransportAdapter : Transport
{
    private NetworkDriver driver;
    private NativeList<Unity.Networking.Transport.NetworkConnection> connections;
    private ushort port = 7777;

    public override bool Available() => true;

    public override Uri ServerUri() => new Uri($"unity-transport://localhost:{port}");

    public override void ServerStart()
    {
        driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = port;

        if (driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind Unity Transport.");
            return;
        }

        driver.Listen();
        connections = new NativeList<Unity.Networking.Transport.NetworkConnection>(Allocator.Persistent);
    }

    public override void ServerStop()
    {
        if (driver.IsCreated) driver.Dispose();
        if (connections.IsCreated) connections.Dispose();
    }

    public override bool ServerActive() => driver.IsCreated;

    public override string ServerGetClientAddress(int connectionId) => "127.0.0.1";

    public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
    {
        if (connectionId < 0 || connectionId >= connections.Length) return;
        var connection = connections[connectionId];

        driver.BeginSend(NetworkPipeline.Null, connection, out DataStreamWriter writer);
        writer.WriteBytes(segment);
        driver.EndSend(writer);
    }

    public override void ServerEarlyUpdate()
    {
        driver.ScheduleUpdate().Complete();

        for (int i = connections.Length - 1; i >= 0; i--)
        {
            var connection = connections[i];
            DataStreamReader stream;
            NetworkEvent.Type cmd;

            while ((cmd = driver.PopEventForConnection(connection, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    byte[] data = new byte[stream.Length];
                    stream.ReadBytes(data);
                    OnServerDataReceived?.Invoke(i, new ArraySegment<byte>(data), 0);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    connections.RemoveAtSwapBack(i);
                    Debug.Log($"Client {i} disconnected.");
                }
            }
        }
    }

    public override void ServerDisconnect(int connectionId)
    {
        if (connectionId < 0 || connectionId >= connections.Length) return;
        driver.Disconnect(connections[connectionId]);
        connections[connectionId] = default;
    }

    public override void ClientConnect(string address)
    {
        driver = NetworkDriver.Create();
        connections = new NativeList<Unity.Networking.Transport.NetworkConnection>(Allocator.Persistent);

        NetworkEndpoint endpoint;
        if (NetworkEndpoint.TryParse(address, port, out endpoint))
        {
            var connection = driver.Connect(endpoint);
            Debug.Log("Client attempting to connect to " + address + ":" + port);
            connections.Add(connection);
        }
        else
        {
            Debug.LogError("Failed to parse server address.");
        }
    }

    public override void ClientDisconnect()
    {
        if (connections.Length > 0)
        {
            driver.Disconnect(connections[0]);
            connections.Clear();
        }
    }

    public override bool ClientConnected() => connections.Length > 0 && connections[0].IsCreated;

    public override void ClientSend(ArraySegment<byte> segment, int channelId)
    {
        if (connections.Length == 0) return;

        driver.BeginSend(NetworkPipeline.Null, connections[0], out DataStreamWriter writer);
        writer.WriteBytes(segment);
        driver.EndSend(writer);
    }

    public override void ClientEarlyUpdate()
    {
        driver.ScheduleUpdate().Complete();

        if (connections.Length == 0)
            return;  // Prevent out-of-range error

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = driver.PopEventForConnection(connections[0], out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                // Handle received data
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Disconnected from server.");
            }
        }
    }


    public override int GetMaxPacketSize(int channelId = 0) => 1200;

    public override void Shutdown() => ServerStop();
}
