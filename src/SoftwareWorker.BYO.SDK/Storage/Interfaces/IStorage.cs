namespace SoftwareWorker.BYO.SDK.Storage.Interfaces;

public interface IStorage<T>
{
    T Load();

    void Save(T source);
}
