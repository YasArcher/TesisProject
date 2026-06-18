using tesisproject.frontend.Services.Interfaces;
using Blazored.LocalStorage;

namespace tesisproject.frontend.Services.Implementations
{
    public class LocalTokenStore : ITokenStore
    {
        private readonly string _key;
        private readonly ILocalStorageService _storage;

        public LocalTokenStore(ILocalStorageService storage, IConfiguration config)
        {
            _storage = storage;
            _key = config["TokenStore:Key"] ?? "authToken";
        }

        public ValueTask SetAsync(string token) => _storage.SetItemAsStringAsync(_key, token);
        public ValueTask<string?> GetAsync() => _storage.GetItemAsStringAsync(_key);
        public ValueTask ClearAsync() => _storage.RemoveItemAsync(_key);
    }
}