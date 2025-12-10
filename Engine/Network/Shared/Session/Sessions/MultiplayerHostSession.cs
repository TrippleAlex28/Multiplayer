using Engine.Network.Client;
using Engine.Network.Server;
using Engine.Network.Shared.Action;
using Engine.Network.Shared.Packet;
using Engine.Network.Shared.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Network.Shared.Session;

public class MultiplayerHostSession : IGameSession
{
    public GameState gs => _clientSession.gs;

    private NetServer _netServer;
    private NetClient _netClient;

    private MultiplayerClientSession _clientSession;

    private readonly Dictionary<int, uint> _lastProcessedActionSeq = new();
    
    private const float _snapshotRate = 30f;
    private float _snapshotTimer = 1f / _snapshotRate;
    
    private bool _initialized = false;
    
    /// ----- SPAWNING CLASSES -----
    private Func<GameObject> _playerConstructor;

    public MultiplayerHostSession(Func<GameObject> playerConstructor)
    {        
        _netServer = new();
        _netClient = new();

        _clientSession = new(_netClient);

        _playerConstructor = playerConstructor;
    }

    public async Task Initialize()
    {
        _netServer.Start();

        _netServer.ClientConnected += OnClientConnected;
        _netServer.ClientDisconnected += OnClientDisconnected;
        _netServer.ActionPacketReceived += OnActionPacketReceived;

        gs.RegisterExistingWorldObjects();

        await _clientSession.Initialize();

        _initialized = true;
    }

    public void HandleInput(InputSnapshot inputSnapshot)
    {
        if (!_initialized) return;
        
        _clientSession.HandleInput(inputSnapshot);
    }

    public void Update(GameTime gameTime)
    {
        if (!_initialized) return;
        
        _clientSession.Update(gameTime);

        _snapshotTimer -= gameTime.DeltaSeconds();
        if (_snapshotTimer <= 0f)
        {
            SendSnapshot();
            _snapshotTimer = 1f / _snapshotRate;
        }
    }

    public void DrawWorld(SpriteBatch spriteBatch)
    {
        if (!_initialized) return;
        
        _clientSession.DrawWorld(spriteBatch);
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        if (!_initialized) return;
        
        _clientSession.DrawUI(spriteBatch);
    }

    public void SwitchScene(string sceneKey)
    {
        if (!_initialized) return;
        
        gs.SwitchScene(sceneKey);
        gs.RegisterExistingWorldObjects();
    }

    #region Event Handlers
    private void OnClientConnected(ClientConnection connection)
    {
        Console.WriteLine("Client Connected");
        gs.AddWorldObject(
            _playerConstructor(),
            owningClientId: connection.ClientId
        );
    }

    private void OnClientDisconnected(ClientConnection connection)
    {
        Console.WriteLine("Client Disconnected");
        GameObject? pawn = gs.GetPawn(connection.ClientId);
        if (pawn == null)
            return;

        gs.RemoveWorldObject(
            pawn.NetworkId
        );
    }
    
    private void OnActionPacketReceived(Udp_ActionPacket packet)
    {
        uint lastSeq = _lastProcessedActionSeq.TryGetValue(packet.ClientId, out uint v) ? v : 0;
        
        foreach (NetAction action in packet.Actions)
        {
            action.Apply(gs, packet.ClientId);

            if (action.SequenceNumber > lastSeq)
                lastSeq = action.SequenceNumber;
        }

        _lastProcessedActionSeq[packet.ClientId] = lastSeq;
    }

    private void SendSnapshot()
    {
        gs.UpdateDirtyFlags();

        // build a snaphsot, for now with all props, even non dirty ones
        Udp_SnapshotPacket packet = new(
            gs.Tick,
            _lastProcessedActionSeq,
            gs.CurrentScene.WorldRoot
        );

        _netServer.BroadcastUdp(packet.CreatePayload());
        
        gs.ClearDirtyFlags();
    }
    #endregion

    public void Dispose()
    {
        _netServer.ClientConnected -= OnClientConnected;
        _netServer.ClientDisconnected -= OnClientDisconnected;
        _netServer.ActionPacketReceived -= OnActionPacketReceived;
        
        _netServer.Dispose();
        _netClient.Dispose();
    }
}