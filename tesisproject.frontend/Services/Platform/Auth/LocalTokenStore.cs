using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Auth
{

    public class LocalTokenStore : ITokenStore
    {
        private const string Key = "auth_token";
        private readonly ILocalStorageService _storage;

        public LocalTokenStore(ILocalStorageService storage)
        {
            _storage = storage;
        }

        public async Task SetAsync(string token)
        {
            await _storage.SetItemAsStringAsync(Key, token);
        }

        public async Task<string?> GetAsync()
        {
            return await _storage.GetItemAsStringAsync(Key);
        }

        public async Task ClearAsync()
        {
            await _storage.RemoveItemAsync(Key);
        }
    }
}
