namespace NetworkFrameworkX.Interface
{
    public enum LoadMode
    {
        LoadOnly,
        CreateAndSaveWhenNull,
        CreateWhenNull
    }

    public interface ISerialzation<TOut>
    {
        bool Save<T>(T value, string path);

        T Load<T>(string path, LoadMode mode = LoadMode.LoadOnly) where T : new();

        T Deserialize<T>(TOut input);

        TOut Serialize<T>(T value);
    }
}