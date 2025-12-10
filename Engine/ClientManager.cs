namespace Engine;

public sealed class ClientManager
{
    private static ClientManager? _instance;
    public static ClientManager Instance
    {
        get
        {
            _instance ??= new ClientManager();
            return _instance;
        }
    }
    
    public string Name { get; private set; }

    private ClientManager()
    {
        // TODO: Retrieve from config file
        Name = "ClientName";
    }
}