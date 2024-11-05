using Backend_hempromak.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend_hempromak.Services
{
    public interface IDataService
    {
        Task<bool> postTransactionAsync(TransferModel transferModel);
        Task<bool> changStock(List<TransferItem> transferItem);
        Task<List<Dictionary<string, object>>> getAll(string current_table);
        Task<List<Dictionary<string, object>>> getAllTransactions();
        Task<List<Dictionary<string, object>>> getTransactionDetailsAsync(int transaction_id);
        Task<List<Dictionary<string, object>>> getCriticalItemsAsync();
        Task<List<Dictionary<string, object>>> getHeadTypesAsync(string headType);
        Task<List<Dictionary<string, object>>> getItemByHeadTypeAndSifraAsync(string head_type, string sifra);
        Task<List<Dictionary<string, object>>> addItemAsync(ItemDTO item);
        Task<List<List<Dictionary<string, object>>>> getCriticalItemAsync();
        Task<List<Dictionary<string, object>>> getTableCountAsync(string tables);
    }
}
