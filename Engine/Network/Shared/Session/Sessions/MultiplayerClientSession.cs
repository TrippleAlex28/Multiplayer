using System.Threading.Tasks;
using System.Xml.XPath;
using Engine.Network.Client;
using Engine.Network.Shared.Action;
using Engine.Network.Shared.Object;
using Engine.Network.Shared.Packet;
using Engine.Network.Shared.State;
using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Network.Shared.Session;

public class MultiplayerClientSession : IGameSession
{
    public GameState gs => _predictedState;
    
    private GameState _predictedState = new("LoadingScene");
    private NetClient _netClient;
    public bool Connected => _netClient.Connected;

    private List<NetAction> _frameActions = new();
    private List<NetAction> _pendingActions = new();

    private uint _nextSequenceNumber = 1;

    private Udp_SnapshotPacket? _latestFutureSnapshot;

    private bool _initialized = false;
    private bool _connected = false;
    
    public MultiplayerClientSession()
    {
        _netClient = new();
    }

    public MultiplayerClientSession(NetClient client)
    {
        _netClient = client;
    }

    public async Task Initialize()
    {
        _netClient.ConnectedResult += OnConnectedResult;
        _netClient.ChatMessageReceived += OnChatMessageReceived;
        _netClient.SnapshotPacketReceived += OnSnapshotPacketReceived;
        _netClient.SceneChangePacketReceived += OnSceneChangePacketReceived;
        _netClient.Disconnected += OnDisconnected;
            
        _initialized = true;
    }

    #region Connection
    public async Task ConnectAsync(string host, int hostTcpPort = 7777, int localUdpPort = 0)
    {
        if (!_initialized || _connected) 
            return;
        
        bool success = await _netClient.ConnectAsync(host, hostTcpPort, localUdpPort);
        _connected = success;
    }

    public async Task Disconnect(string reason)
    {
        await _netClient.DisconnectAsync(reason);
        _connected = false;
    }
    #endregion

    #region Tick
    public void HandleInput(List<NetAction> actions)
    {
        if (!_initialized || !_connected) return;
        
        // CONVERT INPUT INTO NETACTIONS
        _frameActions = actions;

        foreach (NetAction action in _frameActions)
        {
            action.SequenceNumber = _nextSequenceNumber++;
            _pendingActions.Add(action);
                
            // Predict immediately
            action.Apply(gs, _netClient.ClientId);
        }

        if (_frameActions.Count > 0)
        {
            Udp_ActionPacket packet = new(
                _netClient.ClientId,
                _frameActions
            );

            _netClient.SendActionPacket(packet);
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!_initialized || !_connected) return;
        
        gs.Update(gameTime);
    }

    public void DrawWorld(SpriteBatch spriteBatch)
    {
        if (!_initialized || !_connected) return;
        
        gs.DrawWorld(spriteBatch);
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        if (!_initialized || !_connected) return;
        
        gs.DrawUI(spriteBatch);
    }
    #endregion

    #region Actions
    public async void Stop()
    {
        await Disconnect("Client Stopped");
    }

    public void SwitchScene(string sceneKey)
    {
        if (!_initialized || !_connected) return;
        
        // Client is not allowed to switch scenes
        // gs.SwitchScene(sceneKey);
    }
    #endregion

    #region Event Handlers
    public void OnConnectedResult(string currentScene, int currentEpoch)
    {
        gs.SwitchScene(currentScene);
        gs.SceneEpoch = currentEpoch;
    }
    
    public void OnChatMessageReceived(string sender, string message)
    {
        Console.WriteLine($"{sender}: {message}");
    }
    
    public void OnSnapshotPacketReceived(Udp_SnapshotPacket packet)
    {
        // snapshot packet from old scene
        if (packet.SceneEpoch < gs.SceneEpoch)
            return;
        
        // we are behind, queue this packet
        if (packet.SceneEpoch > gs.SceneEpoch)
        {
            _latestFutureSnapshot = packet;
            return;
        }
        
        gs.Tick = packet.Tick;

        SyncSceneRoot(packet);

        // Drop all pending actions that the server has already processed
        uint lastProcessedActionSeq = packet.LastProcessedSequencePerClient.TryGetValue(_netClient.ClientId, out uint v) ? v : 0;
        _pendingActions.RemoveAll(a => a.SequenceNumber <= lastProcessedActionSeq);

        // Reapply remaining pending actions (client-side prediction reconciliation)
        foreach (NetAction action in _pendingActions)
            action.Apply(gs, _netClient.ClientId);
    }

    private void OnSceneChangePacketReceived(Tcp_SceneChangePacket packet)
    {
        // outdated packet
        if (packet.SceneEpoch <= gs.SceneEpoch)
            return;
        
        // stop prediction + clear buffers
        _frameActions.Clear();
        _pendingActions.Clear();

        gs.SwitchScene(packet.NewSceneKey);
        gs.SceneEpoch = packet.SceneEpoch;

        // Apply stored future snapshot
        if (_latestFutureSnapshot != null)
        {
            OnSnapshotPacketReceived(_latestFutureSnapshot);
            _latestFutureSnapshot = null;
        }
    }

    private void OnDisconnected(string reason)
    {
        Console.WriteLine($"MPClient Disconnected: {reason}");
    }
    #endregion
    
    #region Helpers
    private void SyncSceneRoot(Udp_SnapshotPacket packet)
    {
        SceneBase scene = gs.CurrentScene;
        if (gs.CurrentScene == null || packet.WorldRoot == null)
            return;
        
        SceneRoot liveRoot = scene.WorldRoot;
        SceneRoot snapshotRoot = packet.WorldRoot;
        
        // Build lookup of existing replicated objects
        Dictionary<int, GameObject> replicatedMap = [];
        BuildReplicatedMap(liveRoot, replicatedMap);
        
        // Track which replicated IDs are still alive in this snapshot
        HashSet<int> aliveIds = new();

        // Sync the replicated subtree from snapshot into the live tree
        SyncSnapshotNode(
            snapshotRoot,
            liveRoot,
            replicatedMap,
            aliveIds,
            false
        );

        // Remove replicated objects that disappeared from the snapshot
        RemoveDespawnedReplicated(liveRoot, aliveIds);
    }

    private void BuildReplicatedMap(GameObject node, Dictionary<int, GameObject> map)
    {
        if (node.Replicate)
            map[node.NetworkId] = node;

        foreach (GameObject child in node.Children)
            BuildReplicatedMap(child, map);
    }

    private void SyncSnapshotNode(
        GameObject snapshotNode,
        GameObject liveParent,
        Dictionary<int, GameObject> replciatedMap,
        HashSet<int> aliveIds,
        bool skipSelf = false
    )
    {
        GameObject liveNode = liveParent;

        // Allows for the options to skip the root itself and only sync the children
        if (!skipSelf)
        {
            liveNode = GetOrCreateLiveNode(snapshotNode, liveParent, replciatedMap);

            if (liveNode.Replicate)
                aliveIds.Add(liveNode.NetworkId);

            CopyNetProperties(liveNode, snapshotNode);
        }

        foreach (GameObject snapshotChild in snapshotNode.Children)
        {
            SyncSnapshotNode(
                snapshotChild,
                liveNode,
                replciatedMap,
                aliveIds,
                false
            );
        }
    }

    private GameObject GetOrCreateLiveNode(GameObject snapshotNode, GameObject liveParent, Dictionary<int, GameObject> replicatedMap)
    {
        GameObject liveNode;

        // If snapshot has an ID and we already have that replicated object, reuse it
        if (replicatedMap.TryGetValue(snapshotNode.NetworkId, out GameObject existing))
        {
            liveNode = existing;

            // Ensure the parent matches the snapshot parent
            if (liveNode.Parent != null)
            {
                if (!ReferenceEquals(liveNode.Parent, liveParent))
                {
                    liveNode.RemoveFromParent();
                    liveParent.AddChild(liveNode);
                }
            }
        }
        // Otherwise create a new instance and attach it
        else
        {
            liveNode = (GameObject)NetObjectFactory.Create(snapshotNode.TypeId);

            liveNode.NetworkId = snapshotNode.NetworkId;
            liveNode.OwningClientId = snapshotNode.OwningClientId;

            liveParent.AddChild(liveNode);

            replicatedMap[liveNode.NetworkId] = liveNode;
        }

        return liveNode;
    }

    private void CopyNetProperties(NetObject target, NetObject source)
    {
        // Uses existing (De)Serializing system so I don't have to copy fields manually (idk if this is a big hit on performance)
        using MemoryStream ms = new();
        using BinaryWriter w = new(ms);
        
        source.SerializeProperties(w, false);

        ms.Position = 0;

        using BinaryReader r = new(ms);
        target.DeserializeProperties(r);
    }

    private void RemoveDespawnedReplicated(GameObject node, HashSet<int> aliveIds)
    {
        for (int i = node.Children.Count - 1; i >= 0; --i)
        {
            GameObject child = node.Children[i];

            bool isReplicated = child.Replicate;
            bool stillAlive =  isReplicated && aliveIds.Contains(child.NetworkId);

            if (isReplicated && !stillAlive)
            {
                child.RemoveFromParent();
                continue;
            }

            RemoveDespawnedReplicated(child, aliveIds);
        }
    }
    #endregion
    
    public void Dispose()
    {
        _netClient.ConnectedResult -= OnConnectedResult;
        _netClient.ChatMessageReceived -= OnChatMessageReceived;
        _netClient.SnapshotPacketReceived -= OnSnapshotPacketReceived;
        _netClient.SceneChangePacketReceived -= OnSceneChangePacketReceived;
        _netClient.Disconnected -= OnDisconnected;

        _netClient.Dispose();
    }
}