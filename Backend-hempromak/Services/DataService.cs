using Backend_hempromak.Models;
using System.Text.Json;

using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Previewer;
using QuestPDF.Helpers;

namespace Backend_hempromak.Services
{
    public class DataService
    {

        private readonly DbContext _dbContext;
        private readonly IConfiguration _configuration;

        public DataService(DbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<bool> postTransactionAsync (TransferModel transferModel)
        {
            try
            {
                createPDF();

                var transferItemsJson = transferModel.transfer_data;
                var transferItems = JsonSerializer.Deserialize<List<TransferItem>>(transferItemsJson);
                DateTime dateTimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                string formattedDateTimeNow = dateTimeNow.ToString("yyyy-MM-dd HH:mm:ss");
                var query_results = _dbContext.executeSqlQuery($"INSERT INTO transactions (seriski_broj, date_created, type, from_to, vozilo_broj, ispratnica_broj) VALUES " +
                    $"('{transferModel.seriskiBroj}', '{formattedDateTimeNow}', '{transferModel.type}', '{transferModel.kupuva}', '{transferModel.voziloBroj}', '{transferModel.ispratnicaBroj}');");

                if (query_results.Count > 0)
                {
                    var id = query_results[0]["id"];
                    query_results = _dbContext.executeSqlQuery($"INSERT INTO postmeta (post_id, meta_key, meta_value)" +
                        $" VALUES ('{id}', 'seriski_broj', '{transferModel.seriskiBroj}')," +
                        $" ('{id}', 'ispratnica_broj', '{transferModel.ispratnicaBroj}')," +
                        $" ('{id}', 'kupuva', '{transferModel.kupuva}')," +
                        $" ('{id}', 'vozilo_broj', '{transferModel.voziloBroj}')," +
                        $" ('{id}', 'type', '{transferModel.type}')," +
                        $" ('{id}', 'date_created', '{formattedDateTimeNow}')," +
                        $"('{id}', 'transfer_data_json', '{transferItemsJson}');");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private Task createPDF()
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(20));

                    page.Header()
                        .Text("Hello PDF!")
                        .SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().Text(Placeholders.LoremIpsum());
                            x.Item().Image(Placeholders.Image(200, 100));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf("hello.pdf");


            return Task.CompletedTask;

        }

        public async Task<List<Dictionary<string, object>>> getAll()
        {
            var query = _dbContext.executeSqlQuery("select * from adapteri_nipli as an LEFT JOIN adapteri_nipli_details as ands ON an.sifra = ands.sifra");
            
            return query;
        }

        public async Task<List<Dictionary<string, object>>> getAllTransactions()
        {
            var query = _dbContext.executeSqlQuery("select * from transactions");

            foreach (var transaction in query)
            {
                if (transaction.ContainsKey("type") && transaction["type"] != null && transaction["type"].ToString() == "1")
                {
                    transaction["type"] = "izvoz";
                }
                else
                {
                    transaction["type"] = "uvoz";
                }

                if (transaction.ContainsKey("date_created") && transaction["date_created"] is DateTime dateTimeValue)
                {
                    transaction["date_created"] = dateTimeValue.ToString("yyyy-MM-dd");
                }
            }

            return query;
        }


    }
}
