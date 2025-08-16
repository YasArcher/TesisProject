namespace tesisproject.frontend.SharedUI.Modal
{
    public enum ModalSize { Sm, Md, Lg, Xl }

    /// <summary>
    /// Simple pub/sub service for opening and closing <Modal> instances by Id.
    /// Register in DI: services.AddScoped<IModalService, ModalService>();
    /// </summary>
    public interface IModalService
    {
        event Action<string>? OpenRequested;
        event Action<string>? CloseRequested;

        void Open(string id);
        void Close(string id);
    }

    /// <summary>
    /// Lightweight implementation with in-memory events.
    /// </summary>
    public sealed class ModalService : IModalService
    {
        public event Action<string>? OpenRequested;
        public event Action<string>? CloseRequested;

        public void Open(string id) => OpenRequested?.Invoke(id);
        public void Close(string id) => CloseRequested?.Invoke(id);
    }
}
