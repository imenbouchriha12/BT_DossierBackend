using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Reports;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend.BL.BL
{
    public class ReportService : IReportService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IClientRepository clientRepository, ILogger<ReportService> logger)
        {
            _clientRepository = clientRepository;
            _logger = logger;
        }

        public async Task<byte[]> GenerateClientListPdfAsync(ReportRequestDto request)
        {
            _logger.LogInformation("Génération PDF liste clients");

            var statutEnum = !string.IsNullOrWhiteSpace(request.Statut)
                ? (StatutClient?)Enum.Parse(typeof(StatutClient), request.Statut)
                : null;

            var (clients, _) = await _clientRepository.SearchAsync(
                null, null, null,
                request.Ville, statutEnum, request.DateDebut, request.DateFin,
                1, int.MaxValue);

            var total = clients.Count;
            var actifs = clients.Count(c => c.Statut == StatutClient.Actif);
            var enAttente = clients.Count(c => c.Statut == StatutClient.EnAttente);
            var bloques = clients.Count(c => c.Statut == StatutClient.Bloqué);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Lumina Finance").FontSize(18).Bold().FontColor(Colors.Blue.Darken4);
                                c.Item().Text("Rapport - Liste des Clients").FontSize(12).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"Total: {total} clients").FontSize(9).FontColor(Colors.Blue.Darken2);
                            });
                        });
                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(80).Background(Colors.Green.Lighten4).Padding(4).AlignCenter().Column(c =>
                            {
                                c.Item().Text("Actifs").FontSize(8).FontColor(Colors.Green.Darken4);
                                c.Item().Text(actifs.ToString()).FontSize(14).Bold().FontColor(Colors.Green.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(80).Background(Colors.Amber.Lighten4).Padding(4).AlignCenter().Column(c =>
                            {
                                c.Item().Text("En attente").FontSize(8).FontColor(Colors.Amber.Darken4);
                                c.Item().Text(enAttente.ToString()).FontSize(14).Bold().FontColor(Colors.Amber.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(80).Background(Colors.Red.Lighten4).Padding(4).AlignCenter().Column(c =>
                            {
                                c.Item().Text("Bloqués").FontSize(8).FontColor(Colors.Red.Darken4);
                                c.Item().Text(bloques.ToString()).FontSize(14).Bold().FontColor(Colors.Red.Darken4);
                            });
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(55);
                            c.ConstantColumn(90);
                            c.ConstantColumn(80);
                            c.ConstantColumn(80);
                            c.ConstantColumn(100);
                            c.ConstantColumn(70);
                            c.ConstantColumn(60);
                            c.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Code").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Nom & Prénom").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Téléphone").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Email").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Ville").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Date Création").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Statut").FontSize(8).Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Profession").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        foreach (var c in clients)
                        {
                            var bg = clients.IndexOf(c) % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                            table.Cell().Background(bg).Padding(3).Text(c.CodeClient).FontSize(7);
                            table.Cell().Background(bg).Padding(3).Text($"{c.Prenom} {c.Nom}").FontSize(7);
                            table.Cell().Background(bg).Padding(3).Text(c.Telephone).FontSize(7);
                            table.Cell().Background(bg).Padding(3).Text(c.Email).FontSize(7);
                            table.Cell().Background(bg).Padding(3).Text(c.Ville).FontSize(7);
                            table.Cell().Background(bg).Padding(3).Text(c.DateCreation.ToString("dd/MM/yyyy")).FontSize(7);
                            table.Cell().Background(bg).Padding(3).Text(c.Statut.ToString()).FontSize(7).FontColor(
                                c.Statut == StatutClient.Actif ? Colors.Green.Darken2 :
                                c.Statut == StatutClient.Bloqué ? Colors.Red.Darken2 : Colors.Amber.Darken2);
                            table.Cell().Background(bg).Padding(3).Text(c.Detail?.Profession ?? "-").FontSize(7);
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Lumina Finance - Rapport généré le ").FontSize(8).FontColor(Colors.Grey.Darken2);
                        t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                        t.Span(" | Page ").FontSize(8).FontColor(Colors.Grey.Darken2);
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateStatsPdfAsync()
        {
            _logger.LogInformation("Génération PDF statistiques");

            var total = await _clientRepository.CountAsync();
            var villes = await _clientRepository.GroupByVilleAsync();
            var statuts = await _clientRepository.GroupByStatutAsync();
            var actifs = statuts.FirstOrDefault(g => g.Key == StatutClient.Actif)?.Count() ?? 0;
            var enAttente = statuts.FirstOrDefault(g => g.Key == StatutClient.EnAttente)?.Count() ?? 0;
            var bloques = statuts.FirstOrDefault(g => g.Key == StatutClient.Bloqué)?.Count() ?? 0;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Lumina Finance").FontSize(20).Bold().FontColor(Colors.Blue.Darken4);
                        col.Item().Text("Rapport Statistiques Clients").FontSize(14).FontColor(Colors.Grey.Darken2);
                        col.Item().Text($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(16).Row(r =>
                        {
                            r.ConstantItem(110).Background(Colors.Blue.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().Text("Total").FontSize(10).FontColor(Colors.Blue.Darken4);
                                c.Item().Text(total.ToString()).FontSize(28).Bold().FontColor(Colors.Blue.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(110).Background(Colors.Green.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().Text("Actifs").FontSize(10).FontColor(Colors.Green.Darken4);
                                c.Item().Text(actifs.ToString()).FontSize(28).Bold().FontColor(Colors.Green.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(110).Background(Colors.Amber.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().Text("En attente").FontSize(10).FontColor(Colors.Amber.Darken4);
                                c.Item().Text(enAttente.ToString()).FontSize(28).Bold().FontColor(Colors.Amber.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(110).Background(Colors.Red.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().Text("Bloqués").FontSize(10).FontColor(Colors.Red.Darken4);
                                c.Item().Text(bloques.ToString()).FontSize(28).Bold().FontColor(Colors.Red.Darken4);
                            });
                        });

                        col.Item().PaddingBottom(8).Text("Répartition par Ville").FontSize(12).Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.ConstantColumn(80);
                                c.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Ville").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Nombre").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Pourcentage").FontSize(9).Bold().FontColor(Colors.White);
                            });

                            foreach (var g in villes.OrderByDescending(g => g.Count()))
                            {
                                var pct = total > 0 ? (g.Count() * 100.0 / total) : 0;
                                table.Cell().Padding(3).Text(g.Key).FontSize(8);
                                table.Cell().Padding(3).Text(g.Count().ToString()).FontSize(8);
                                table.Cell().Padding(3).Text($"{pct:F1}%").FontSize(8);
                            }
                        });

                        col.Item().PaddingTop(12).Text("Répartition par Statut").FontSize(12).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.ConstantColumn(80);
                                c.ConstantColumn(100);
                            });

                            foreach (var (label, count, color) in new[] {
                                ("Actif", actifs, Colors.Green.Darken2),
                                ("En attente", enAttente, Colors.Amber.Darken2),
                                ("Bloqué", bloques, Colors.Red.Darken2) })
                            {
                                var pct = total > 0 ? (count * 100.0 / total) : 0;
                                table.Cell().Padding(3).Text(label).FontSize(8).FontColor(color);
                                table.Cell().Padding(3).Text(count.ToString()).FontSize(8);
                                table.Cell().Padding(3).Text($"{pct:F1}%").FontSize(8);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Lumina Finance | Page ").FontSize(8).FontColor(Colors.Grey.Darken2);
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateClientListExcelAsync(ReportRequestDto request)
        {
            _logger.LogInformation("Génération Excel rapport clients");

            var statutEnum = !string.IsNullOrWhiteSpace(request.Statut)
                ? (StatutClient?)Enum.Parse(typeof(StatutClient), request.Statut)
                : null;

            var (clients, _) = await _clientRepository.SearchAsync(
                null, null, null,
                request.Ville, statutEnum, request.DateDebut, request.DateFin,
                1, int.MaxValue);

            var stream = new MemoryStream();
            using (var package = new OfficeOpenXml.ExcelPackage(stream))
            {
                var ws = package.Workbook.Worksheets.Add("Clients");

                ws.Cells[1, 1].Value = "Code Client";
                ws.Cells[1, 2].Value = "Nom";
                ws.Cells[1, 3].Value = "Prénom";
                ws.Cells[1, 4].Value = "Date Naissance";
                ws.Cells[1, 5].Value = "Sexe";
                ws.Cells[1, 6].Value = "Nationalité";
                ws.Cells[1, 7].Value = "Téléphone";
                ws.Cells[1, 8].Value = "Email";
                ws.Cells[1, 9].Value = "Adresse";
                ws.Cells[1, 10].Value = "Ville";
                ws.Cells[1, 11].Value = "Date Création";
                ws.Cells[1, 12].Value = "Statut";
                ws.Cells[1, 13].Value = "Profession";
                ws.Cells[1, 14].Value = "Employeur";
                ws.Cells[1, 15].Value = "Revenu Mensuel";
                ws.Cells[1, 16].Value = "Secteur Activité";
                ws.Cells[1, 17].Value = "Situation Familiale";

                using (var range = ws.Cells[1, 1, 1, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(26, 35, 126));
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                int row = 2;
                foreach (var c in clients)
                {
                    ws.Cells[row, 1].Value = c.CodeClient;
                    ws.Cells[row, 2].Value = c.Nom;
                    ws.Cells[row, 3].Value = c.Prenom;
                    ws.Cells[row, 4].Value = c.DateNaissance.ToString("yyyy-MM-dd");
                    ws.Cells[row, 5].Value = c.Sexe;
                    ws.Cells[row, 6].Value = c.Nationalite;
                    ws.Cells[row, 7].Value = c.Telephone;
                    ws.Cells[row, 8].Value = c.Email;
                    ws.Cells[row, 9].Value = c.Adresse;
                    ws.Cells[row, 10].Value = c.Ville;
                    ws.Cells[row, 11].Value = c.DateCreation.ToString("yyyy-MM-dd HH:mm");
                    ws.Cells[row, 12].Value = c.Statut.ToString();
                    ws.Cells[row, 13].Value = c.Detail?.Profession;
                    ws.Cells[row, 14].Value = c.Detail?.Employeur;
                    ws.Cells[row, 15].Value = c.Detail?.RevenuMensuel;
                    ws.Cells[row, 16].Value = c.Detail?.SecteurActivite;
                    ws.Cells[row, 17].Value = c.Detail?.SituationFamiliale;
                    row++;
                }

                ws.Cells.AutoFitColumns();
                package.Save();
            }

            stream.Position = 0;
            return stream.ToArray();
        }
    }
}
