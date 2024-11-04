using Backend_hempromak.Models;
using System.Text.Json;

using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Previewer;
using QuestPDF.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net.Mime;
using Org.BouncyCastle.Crypto.Operators;
using System.Security.Policy;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Backend_hempromak.Services
{
    public class DataService
    {

        private readonly DbContext _dbContext;
        private readonly IConfiguration _configuration;
        private MemoryStream _pdfStream;

        public DataService(DbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<bool> postTransactionAsync (TransferModel transferModel)
        {
            try
            {
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

                if (!await changStock(transferItems)) return false;

                createPDF(transferModel);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> changStock (List<TransferItem> transferItem)
        {
            try
            {
                transferItem.RemoveAt(transferItem.Count - 1);
                foreach (var _item in transferItem)
                {
                    try
                    {
                        _dbContext.executeSqlQuery($"UPDATE items_adapteri_nipli_details SET komada = '{_item.kol_sos}' WHERE sifra = {_item.sifra}");
                        _dbContext.executeSqlQuery($"UPDATE items_adapteri_nipli_details SET times_used = times_used + 1 WHERE sifra = {_item.sifra}");
                        if (Int32.Parse(_item.kol_sos) <= 10)
                            await criticalItem(_item.sifra, "insert");
                    }
                    catch (Exception ex) { throw new Exception(ex.Message); }
                }

                return true;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task criticalItem (string _item_sifra, string query_type)
        {
            try
            {
                if (query_type == "insert")
                    _dbContext.executeSqlQuery($"INSERT INTO critical_items (sifra, isActive, table_from) VALUES ({Int32.Parse(_item_sifra)}, 1, 'adapteri_nipli') ON DUPLICATE KEY UPDATE isActive = 1");
                else if (query_type == "update")
                    _dbContext.executeSqlQuery($"UPDATE critical_items SET isActive = 0 WHERE sifra = {Int32.Parse(_item_sifra)}");
            }
            catch(Exception ex) { throw new Exception(ex.Message); }
        }

        private async Task createPDF(TransferModel transferModel)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                const int inchesToPoints = 72;
                DateTime dateTimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                string formattedDateTimeNow = dateTimeNow.ToString("dd-MM-yyyy");

                using var memoryStream = new MemoryStream();

                // Generate PDF document
                Document.Create(container =>
                {
                    container.Page(page =>
                    {

                        page.Header().Text($"Faktura \n {transferModel.seriskiBroj}-{transferModel.kupuva} {formattedDateTimeNow} ").AlignCenter();

                        page.Margin(30);

                        page.Content().Column(column =>
                        {

                            column.Item().PaddingTop(25);

                            column.Item().Text($"So ispratnica broj {transferModel.seriskiBroj} - {transferModel.ispratnicaBroj}/{DateTime.Now.Year} od {DateTime.Today:dd-MM-yyyy} god so vozilo br. {transferModel.voziloBroj} isporachano e:");

                            column.Item().PaddingTop(15);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem(4).Text($"Kupuvac: {transferModel.kupuva}");
                                row.RelativeItem(5).AlignRight().Text($"Data na faktura: {formattedDateTimeNow}\nSeriski broj: {transferModel.seriskiBroj}")
                                    .FontSize(10);
                            });

                            column.Item().PaddingTop(15);

                            column.Item().MinimalBox().Border(1).DefaultTextStyle(x => x.FontSize(9)).Table(table =>
                            {
                                QuestPDF.Infrastructure.IContainer DefaultCellStyle(QuestPDF.Infrastructure.IContainer container, string backgroundColor)
                                {
                                    return container
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Background(backgroundColor)
                                        .PaddingVertical(5)
                                        .PaddingHorizontal(10)
                                        .AlignCenter()
                                        .AlignMiddle();
                                }

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).AlignLeft().Text("Sifra").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("Opis").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("Kolicina Prenos").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("Kolicina Sostojba").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("Cena").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("Iznos bez DDV").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("DDV %").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("DDV").Bold();
                                    header.Cell().Element(c => DefaultCellStyle(c, Colors.Grey.Lighten3)).Text("Iznos so DDV").Bold();
                                });

                                var transferItems = JsonSerializer.Deserialize<List<TransferItem>>(transferModel.transfer_data);
                                if (transferItems != null)
                                {
                                    foreach (var item in transferItems)
                                    {
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.sifra);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.opis);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.kol);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.kol_sos);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.cena);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.no_ddv);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.ddv_percent);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.ddv);
                                        table.Cell().Element(c => DefaultCellStyle(c, Colors.White)).Text(item.ddv_amount);
                                    }
                                }
                            });

                            column.Item().PaddingTop(35);

                            column.Item().Text("Plaawe vo rok od 60 dena od datumot na promet, po ovoj rok zasmetuvame zakonska zatezna kamata i ednokraten nadomest (3.000 denari) soglasno Zakonot za finansiska disciplina. Reklamacija vo rok od 3 dena, so ureden komisiski zapisnik. Vo slu~aj na spor nadle`en e Osnovniot Sud vo Skopje").AlignLeft();

                            column.Item().PaddingTop(150);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem(5).AlignCenter().Text("Potpis 1 \n\n\n ___________________________");
                                row.RelativeItem(5).AlignCenter().Text("Potpis 2 \n\n\n ____________________________");
                            });

                        });

                        page.Footer().Text($"HEMPROMAK {DateTime.Now.Year}").AlignCenter();

                    });

                }).GeneratePdf(memoryStream);

                memoryStream.Position = 0;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("nikpetrovski007@gmail.com"),
                    Subject = "Test",
                    Body = "Please find the attached PDF document."
                };
                mailMessage.To.Add("nikpetrovski007@gmail.com");

                var attachment = new Attachment(memoryStream, $"NewPdf.pdf", MediaTypeNames.Application.Pdf);
                mailMessage.Attachments.Add(attachment);

                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("hhempromak@gmail.com", "mlkccaiwcjhvcork"),
                    EnableSsl = true,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // Reusable style for table cells
        IContainer CellStyle(IContainer container) => container.Padding(5).Border(1).Background(Colors.Grey.Lighten3);

        public async Task<List<Dictionary<string, object>>> getAll(string current_table)
        {
            var query = _dbContext.executeSqlQuery($"SELECT * FROM items_{current_table} LEFT JOIN items_{current_table}_details ON items_{current_table}.sifra = items_{current_table}_details.sifra ORDER BY items_{current_table}_details.times_used DESC");
            
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

        public async Task<List<Dictionary<string, object>>> getTransactionDetailsAsync(int transaction_id)
        {
            var query = _dbContext.executeSqlQuery($"select meta_value from postmeta where post_id = {transaction_id} and meta_key = 'transfer_data_json'");

            return query;

        }

        public async Task<List<Dictionary<string, object>>> getCriticalItemsAsync()
        {
            var query = _dbContext.executeSqlQuery($"select * from critical_items WHERE isActive = 1");

            return query;
        }

        public async Task<List<Dictionary<string, object>>> getHeadTypesAsync(string head_type)
        {
            var query = _dbContext.executeSqlQuery($"SELECT sifra, cena, opis FROM items_{head_type}_details ORDER BY times_used DESC");

            var result = query.Select(item => new Dictionary<string, object>
            {
                { "label", $"{item["sifra"]} - {item["cena"]} MKD ({item["opis"]})" },
                { "value", $"{item["sifra"]} - {item["cena"]} MKD {item["opis"]}" }
            }).ToList();

            return result;
        }

        public async Task<List<Dictionary<string, object>>> getItemByHeadTypeAndSifraAsync(string head_type, string sifra)
        {
            var query = _dbContext.executeSqlQuery($"SELECT * FROM items_{head_type} LEFT JOIN items_{head_type}_details ON items_{head_type}.sifra = items_{head_type}_details.sifra WHERE items_{head_type}.sifra = {sifra}");

            return query;
        }

        public async Task<List<Dictionary<string, object>>> addItemAsync(ItemDTO item)
        {

            var item_exsists = _dbContext.executeSqlQuery($"SELECT * FROM items_{item.head_type} WHERE sifra = {item.sifra}");

            if (item_exsists.Count > 0)
            {
                var query = _dbContext.executeSqlQuery($"UPDATE items_{item.head_type} SET head_type = '{item.head_type}', type = '{item.type}' WHERE sifra = {item.sifra}");

                query = _dbContext.executeSqlQuery($"UPDATE items_{item.head_type}_details SET cena = {item.cena}, komada = '{item.komada}', opis = '{item.opis}', times_used = times_used + 1 WHERE sifra = {item.sifra} AND ABS(cena - {item.cena}) < 0.01");

                if (item.komada > 10)
                    await criticalItem(item.sifra, "update");

                return query;
            }   
            else
            {
                var query = _dbContext.executeSqlQuery($@"
                    INSERT INTO items_{item.head_type} (sifra, head_type, type) 
                    VALUES ({Int32.Parse(item.sifra)}, '{item.head_type}', '{item.type}');
                ");

                query = _dbContext.executeSqlQuery($@"
                    INSERT INTO items_{item.head_type}_details (sifra, cena, komada, opis, times_used)
                    VALUES ({Int32.Parse(item.sifra)}, {item.cena}, '{item.komada}', '{item.opis}', 0);
                ");

                return query;

            }
        }

        public async Task<List<List<Dictionary<string, object>>>> getCriticalItemAsync()
        {
            var table_from = _dbContext.executeSqlQuery("SELECT DISTINCT(table_from) FROM critical_items WHERE isActive = 1");
            var queries = new List<List<Dictionary<string, object>>>();

            foreach (var table in table_from)
            {
                if (table.ContainsKey("table_from") && table["table_from"] != null)
                {
                    var table_name = table["table_from"].ToString();

                    var query = _dbContext.executeSqlQuery($"SELECT {table_name}.sifra, {table_name}.head_type, {table_name}.type, {table_name}_details.komada, {table_name}_details.cena, {table_name}_details.opis FROM critical_items AS ct LEFT JOIN {table_name}_details ON ct.sifra = {table_name}_details.sifra LEFT JOIN {table_name} ON {table_name}_details.sifra = {table_name}.sifra WHERE ct.isActive = 1 AND {table_name}_details.komada < 10 ORDER BY {table_name}_details.times_used DESC");

                    queries.Add(query);

                }
            }

            return queries;

        }

    }
}
