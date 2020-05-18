using SimpleObjectBrowser.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Meziantou.Framework.Win32;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace SimpleObjectBrowser.Services
{
    public class CredentialRoot
    {
        public IEnumerable<StoredAccount> Accounts { get; set; }
    }

    public class StoredAccount
    {
        public ICredential Credential { get; set; }
    }

    public class ConfigService
    {

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        private const string Key = "SimpleStorageBrowser.Accounts";
        public static List<AccountViewModel> GetSavedAccounts()
        {
            var list = new List<AccountViewModel>();

            try
            {
                var credential = CredentialManager.ReadCredential(Key);
                if (string.IsNullOrEmpty(credential?.Password) == false)
                {
                    var root = JsonConvert.DeserializeObject<CredentialRoot>(credential.Password, _jsonSettings);

                    foreach (var account in root.Accounts)
                    {
                        try
                        {
                            var viewModel = new AccountViewModel(account.Credential);
                            list.Add(viewModel);
                        }
                        catch (Exception ex)
                        {
                            // Omit it
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Omit it
            }

            return list;
        }
        public static void SaveAccounts(IEnumerable<AccountViewModel> accounts)
        {
            var root = new CredentialRoot
            {
                Accounts = accounts.Select(a => new StoredAccount { Credential = a.Credential }).ToList(),
            };

            var json = JsonConvert.SerializeObject(root, _jsonSettings);
            CredentialManager.WriteCredential(Key, "accounts", json, CredentialPersistence.LocalMachine);
        }
    }
}
