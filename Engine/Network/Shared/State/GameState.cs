using Engine.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Network.Shared.State;

public sealed class GameState
{
    public SceneBase CurrentScene { get; private set; }
    
    /// <summary>
    /// Current scene 'couter'
    /// </summary>
    public int SceneEpoch { get; set; } = 0;
    
    /// <summary>
    /// Current tick the game is in
    /// </summary>
    public uint Tick { get; set; } = 1;
    
    private int _nextNetworkId = 1;

    public GameState(string baseSceneKey)
    {
        CurrentScene = SceneRegistry.Create(baseSceneKey);
    }

    #region Tick
    public void Update(GameTime gameTime)
    {
        CurrentScene.Update(gameTime);

        Tick++;
    }

    public void DrawWorld(SpriteBatch spriteBatch)
    {
        CurrentScene.DrawWorld(spriteBatch);
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        CurrentScene.DrawUI(spriteBatch);
    }
    #endregion
    
    #region Scene
    public void SwitchScene(string sceneKey)
    {
        CurrentScene.Close();
        CurrentScene = SceneRegistry.Create(sceneKey);
        CurrentScene.Open();
    }

    public void RegisterExistingWorldObjects()
    {
        RegisterRecursive(CurrentScene.WorldRoot);
    }

    private void RegisterRecursive(GameObject obj)
    {
        if (obj.Replicate)
            RegisterWorldObject(obj);

        foreach (GameObject child in obj.Children)
            RegisterRecursive(child);
    }
    #endregion

    #region Objects
    public GameObject? GetPawn(int clientId)
    {
        return GetPawnRecursive(CurrentScene.WorldRoot, clientId);
    }

    public GameObject? GetWorldObject(int netId)
    {
        return GetWorldObjectRecursive(CurrentScene.WorldRoot, netId);
    }
    
    public void AddWorldObject(GameObject obj, GameObject? parent = null, int owningClientId = -1)
    {
        if (obj.Replicate)
            RegisterWorldObject(obj, owningClientId);
        
        if (parent != null)
            parent.AddChild(obj);
        else
            CurrentScene.WorldRoot.AddChild(obj);
    }

    public void RemoveWorldObject(int netId)
    {
        GameObject? obj = GetWorldObject(netId);

        obj?.RemoveFromParent();
    }

    public void RemoveClientWorldObjects(int owningClientId)
    {
        List<GameObject> list = GetClientWorldObjectsRecursive(CurrentScene.WorldRoot, owningClientId);
        for (int i = list.Count - 1; i >= 0; --i)
        {
            list[i].RemoveFromParent();
        }
    }

    private void RegisterWorldObject(GameObject obj, int owningClientId = -1)
    {
        if (obj.NetworkId == -1)
            obj.NetworkId = _nextNetworkId++;

        obj.OwningClientId = owningClientId;
    }
    #endregion

    #region Dirty Flags
    public void UpdateDirtyFlags()
    {
        UpdateDirtyFlags(CurrentScene.WorldRoot);
    }
    
    private void UpdateDirtyFlags(GameObject obj)
    {
        obj.UpdateDirtyFlags();

        foreach (GameObject child in obj.Children)
            UpdateDirtyFlags(child);
    }

    public void ClearDirtyFlags()
    {
        ClearDirtyFlags(CurrentScene.WorldRoot);
    }

    private void ClearDirtyFlags(GameObject obj)
    {
        obj.ClearDirtyFlags();

        foreach (GameObject child in obj.Children)
            ClearDirtyFlags(child);
    }
    #endregion

    #region Helpers
    private GameObject? GetWorldObjectRecursive(GameObject node, int netId)
    {
        if (node.NetworkId == netId)
            return node;

        foreach (GameObject child in node.Children)
        {
            GameObject? found = GetWorldObjectRecursive(child, netId);
            if (found != null)
                return found;
        }

        return null;
    }

    private List<GameObject> GetClientWorldObjectsRecursive(GameObject root, int owningClientId)
    {
        List<GameObject> list = new();

        if (root.OwningClientId == owningClientId || root.OwningClientId == 0)
            list.Add(root);

        foreach (GameObject child in root.Children)
        {
            List<GameObject> found = GetClientWorldObjectsRecursive(child, owningClientId);
            list.AddRange(found);
        }

        return list;
    }

    private GameObject? GetPawnRecursive(GameObject node, int owningClientId)
    {
        if (node.OwningClientId == owningClientId || node.OwningClientId == 0)
            return node;

        foreach (GameObject child in node.Children)
        {
            GameObject? found = GetPawnRecursive(child, owningClientId);
            if (found != null)
                return found;
        }

        return null;
    }
    #endregion
}

