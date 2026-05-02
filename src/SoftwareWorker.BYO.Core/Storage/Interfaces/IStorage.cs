namespace SoftwareWorker.BYO.CLI.Core.Storage.Interfaces;

public interface IStorage<T>
{
    T Load();

    void Save(T source);
}
