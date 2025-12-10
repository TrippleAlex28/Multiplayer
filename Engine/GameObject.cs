using Engine.Network.Shared.Object;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine;

public enum GameObjectType
{
    None,
    Pickup,
    Enemy,
}

public class GameObject : NetObject
{
    public override NetObjectTypeIds TypeId => NetObjectTypeIds.GameObject;
    
    static GameObject()
    {
        NetObjectFactory.Register<GameObject>(NetObjectTypeIds.GameObject);
    }
    
    public GameObject() : base()
    {
        RegisterProperty(
            nameof(this.IsVisible), 
            () => this.IsVisible,
            (v) => IsVisible = v
        );

        RegisterProperty(
            nameof(this.GlobalPosition), 
            () => this.GlobalPosition,
            (v) => GlobalPosition = v
        );

        RegisterProperty(
            nameof(this.Velocity), 
            () => this.Velocity,
            (v) => Velocity = v
        );

        RegisterProperty(
            nameof(this.Direction), 
            () => this.Direction,
            (v) => Direction = v
        );
    }

    #region Events
    public delegate void GameObjectChangeEvent();
    public event GameObjectChangeEvent? IsVisibleChanged;
    public event GameObjectChangeEvent? Entered;
    public event GameObjectChangeEvent? Exited;
    #endregion

    public GameObjectType Type = GameObjectType.None;

    #region Hierarchy
    public int ChildCount => this.Children.Count;
    public GameObject? Parent { get; private set; } = null;
    public List<GameObject> Children { get; } = new List<GameObject>();
    #endregion
    
    #region Positioning
    public Vector2 GlobalPosition
    {
        get
        {
            if (this.Parent == null)
                return this.LocalPosition;

            return this.LocalPosition + this.Parent.GlobalPosition;
        }
        set
        {
            if (this.Parent == null)
                this.LocalPosition = value;
            else
                this.LocalPosition += value - this.GlobalPosition;
        }
    }
    public Vector2 LocalPosition { get; set; } = Vector2.Zero;
    public Vector2 Direction
    {
        get
        {
            return this._direction;
        }
        set
        {
            this._direction = value.IsApproximatelyZero() ? Vector2.Zero : value.Normalized();
        }
    }
    private Vector2 _direction = Vector2.Zero;
    public float Velocity { get; set; } = 0;
    public Vector2 Displacement => this.Direction * this.Velocity;
    #endregion

    #region Drawing
    /// <summary>
    /// The product of the parallax factor and camera offset.
    /// </summary>
    public Vector2 ScrollOffset => this.ParallaxFactor * this.CameraOffset;
    /// <summary>
    /// Determines how much a drawable game object will scroll with the camera
    /// (e.g. how far it is into the background). For a component "v" of a Vector2:
    /// v == 0 means no scrolling (e.g. user interface elements).
    /// 1 > v > 0 means relatively slow scrolling (e.g. background elements).
    /// v == 1 means scrolling at a regular pace.
    /// v > 1 means relatively fast scrolling (e.g. foreground elements).
    /// </summary>
    public Vector2 ParallaxFactor { get; set; } = Vector2.One;
    /// <summary>
    /// The offset from the object's global position when it is drawn on screen.
    /// This is related to scrolling and should be determined by an external "camera" instance.
    /// </summary>
    public Vector2 CameraOffset { get; set; } = Vector2.Zero;
    /// <summary>
    /// Whether this object and all of its children should be drawn at all.
    /// Objects are visible by default.
    /// </summary>
    public bool IsVisible
    {
        get => this._isVisible;
        set
        {
            bool hasVisibilityChanged = this._isVisible != value;
            this._isVisible = value;

            if (hasVisibilityChanged)
                this.IsVisibleChanged?.Invoke();

            for (int i = 0; i < this.Children.Count; ++i)
                this.Children[i].IsVisible = value;
        }
    }
    private bool _isVisible = true;
    /// <summary>
    /// The actual draw layer of this object in the range [0, 1], which is dictated by MonoGame's
    /// "layerDepth" parameter in SpriteBatch's Draw method. Set BaseDrawLayer and FineDrawLayer
    /// to affect this. If two objects have the same value, then the draw order depends on when
    /// each object was created (later added is on top).
    /// </summary>
    public float DrawLayer => this.BaseDrawLayer + this.FineDrawLayer;
    /// <summary>
    /// A value in the range [0, 1].
    /// Overshooting values are automatically clamped.
    /// Be smart about what values you use (in relation to FineDrawLayer).
    /// Pick relatively large values compard to FineDrawLayer (e.g. 0.1f, 0.2f).
    /// </summary>
    public float BaseDrawLayer
    {
        get => this._baseDrawLayer;
        set => this._baseDrawLayer = value.Clamp(0, 1);
    }
    private float _baseDrawLayer = 0;
    /// <summary>
    /// A value in the range [0, 1] to distinguish between more layers on top of the BaseDrawLayer.
    /// Overshooting values are automatically clamped.
    /// Be smart about what values you use (in relation to BaseDrawLayer).
    /// Pick relatively small values compared to DrawLayer (e.g. 0.001f, 0.002f).
    /// </summary>
    public float FineDrawLayer
    {
        get => this._fineDrawLayer;
        set => this._fineDrawLayer = value.Clamp(0, 1);
    }
    private float _fineDrawLayer = 0;
    #endregion

    #region Hierarchy manipulating
    public virtual void AddChild(GameObject @object)
    {
        if (@object == this)
            throw new Exception("A child cannot be a child of itself.");

        if (@object.Parent != null)
            throw new Exception("Cannot add child with a parent.");

        @object.Parent = this;
        this.Children.Add(@object);
        @object.Enter();
    }

    public void AddChild(GameObject @object, params GameObject[] objects)
    {
        this.AddChild(@object);

        for (int i = 0; i < objects.Length; ++i)
            this.AddChild(objects[i]);
    }

    public GameObject[] GetChildren()
    {
        return this.Children.ToArray();
    }

    public void RemoveChild(GameObject @object)
    {
        @object.Exit();
        @object.Parent = null;
        bool hasChild = this.Children.Remove(@object);

        if (!hasChild)
            throw new Exception("Cannot remove child from non-parent.");
    }

        public void RemoveChild(GameObject @object, params GameObject[] objects)
    {
        this.RemoveChild(@object);

        for (int i = 0; i < objects.Length; ++i)
            this.RemoveChild(objects[i]);
    }

    public void RemoveAllChildren()
    {
        this.Children.Clear();
    }

    public GameObject GetRoot()
    {
        GameObject root = this;
        while (root.Parent != null)
            root = root.Parent;
        return root;
    }
    #endregion

    #region Updating
    /// <summary>
    /// Called just after the object is added to a parent-child hierarchy.
    /// </summary>
    public void Enter()
    {
        this.EnterSelf();
        this.Entered?.Invoke();
    }
    public void RemoveFromParent()
    {
        Parent?.RemoveChild(this);
    }

    public void RemoveSelf()
    {
        Parent?.RemoveChild(this);
    }


    /// <summary>
    /// Called every frame if the object is part of a parent-child hierarchy.
    /// </summary>
    /// <param name="gameTime"></param>
    public virtual void Update(GameTime gameTime)
    {
        this.UpdateSelf(gameTime);
        this.UpdatePosition(gameTime.DeltaSeconds());

        for (int i = 0; i < this.Children.Count; ++i)
            this.Children[i].Update(gameTime);
    }

    /// <summary>
    /// Called after every Update call if the objct is part of a parent-child hierarchy.
    /// </summary>
    /// <param name="spriteBatch">Use for draw commands.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!this.IsVisible)
            return;

        this.DrawSelf(spriteBatch);

        for (int i = 0; i < this.Children.Count; ++i)
            this.Children[i].Draw(spriteBatch);
    }

    /// <summary>
    /// Called just before the object is removed from a parent-child hierarchy.
    /// </summary>
    public void Exit()
    {
        this.ExitSelf();
        this.Exited?.Invoke();
    }

    /// <summary>
    /// Override this in your concrete class to add unique Enter logic.
    /// Examples: initialization code, subscribing to events...
    /// </summary>
    protected virtual void EnterSelf()
    {
    }

    /// <summary>
    /// Override this in your concrete class to add unique Update logic.
    /// </summary>
    protected virtual void UpdateSelf(GameTime gameTime)
    {
    }

    /// <summary>
    /// Override this in your concrete class to add unique Draw logic.
    /// </summary>
    protected virtual void DrawSelf(SpriteBatch spriteBatch)
    {
    }

    /// <summary>
    /// Override this in your concrete class to add unique Exit logic.
    /// Examples: reset code, deinitialization code, unsubscribing from events...
    /// </summary>
    protected virtual void ExitSelf()
    {
    }
    
    /// <summary>
    /// Updates the position according to a generic formula.
    /// </summary>
    /// <param name="deltaSeconds">The time between the previous and current frame in seconds.</param>
    private void UpdatePosition(float deltaSeconds)
    {
        
        this.GlobalPosition += this.Displacement * deltaSeconds;
    }
    #endregion
}
