using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Reports;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend.BL.BL
{
    /// Génération de relevés de compte (PDF / Excel) pour le module financier.
    public class ReleveService : IReleveService
    {
        private readonly ICompteRepository _compteRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IMouvementRepository _mouvementRepository;
        private readonly ILogger<ReleveService> _logger;

        public ReleveService(
            ICompteRepository compteRepository,
            IClientRepository clientRepository,
            IMouvementRepository mouvementRepository,
            ILogger<ReleveService> logger)
        {
            _compteRepository = compteRepository;
            _clientRepository = clientRepository;
            _mouvementRepository = mouvementRepository;
            _logger = logger;
        }

        public async Task<byte[]> GenerateRelevePdfAsync(ReleveRequestDto request)
        {
            _logger.LogInformation("Génération PDF relevé compte #{CompteId}", request.CompteId);

            var (compte, client, mouvements) = await ChargerDonneesAsync(request);
            if (compte == null)
                throw new KeyNotFoundException("Compte introuvable.");

            var (totalCredit, totalDebit, soldeInitial) = CalculerTotaux(compte, mouvements);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("BT DocCenter").FontSize(18).Bold().FontColor(Colors.Blue.Darken4);
                                c.Item().Text("Relevé de compte").FontSize(13).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Édité le {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"Période : {LibellePeriode(request)}").FontSize(9).Bold().FontColor(Colors.Blue.Darken2);
                            });
                        });
                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Client").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().Text(client == null ? "-" : $"{client.Prenom} {client.Nom}").FontSize(10).Bold();
                                c.Item().Text(client == null ? "" : $"Code client : {client.CodeClient}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            });
                            r.ConstantItem(210).Column(c =>
                            {
                                c.Item().Text("Compte").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"{compte.NumCompte} - Agence {compte.Agence}").FontSize(10).Bold().FontColor(Colors.Blue.Darken4);
                                c.Item().Text($"Devise : {compte.Devise}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            });
                        });

                        col.Item().PaddingVertical(6).Row(r =>
                        {
                            r.ConstantItem(120).Background(Colors.Green.Lighten4).Padding(5).Column(c =>
                            {
                                c.Item().Text("Solde d'ouverture").FontSize(8).FontColor(Colors.Green.Darken4);
                                c.Item().Text(soldeInitial.ToString("N2")).FontSize(12).Bold().FontColor(Colors.Green.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(120).Background(Colors.Amber.Lighten4).Padding(5).Column(c =>
                            {
                                c.Item().Text("Total Crédit").FontSize(8).FontColor(Colors.Amber.Darken4);
                                c.Item().Text(totalCredit.ToString("N2")).FontSize(12).Bold().FontColor(Colors.Amber.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(120).Background(Colors.Red.Lighten4).Padding(5).Column(c =>
                            {
                                c.Item().Text("Total Débit").FontSize(8).FontColor(Colors.Red.Darken4);
                                c.Item().Text(totalDebit.ToString("N2")).FontSize(12).Bold().FontColor(Colors.Red.Darken4);
                            });
                            r.ConstantItem(10);
                            r.ConstantItem(120).Background(Colors.Blue.Lighten4).Padding(5).Column(c =>
                            {
                                c.Item().Text("Solde actuel").FontSize(8).FontColor(Colors.Blue.Darken4);
                                c.Item().Text(compte.Solde.ToString("N2")).FontSize(12).Bold().FontColor(Colors.Blue.Darken4);
                            });
                        });

                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.RelativeColumn();
                                c.ConstantColumn(60);
                                c.ConstantColumn(80);
                                c.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Date").FontSize(8).Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Libellé").FontSize(8).Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Devise").FontSize(8).Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Débit").FontSize(8).Bold().FontColor(Colors.White).AlignRight();
                                header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Crédit").FontSize(8).Bold().FontColor(Colors.White).AlignRight();
                            });

                            var index = 0;
                            foreach (var m in mouvements.OrderByDescending(m => m.DateMvt))
                            {
                                var bg = index++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                                table.Cell().Background(bg).Padding(3).Text(m.DateMvt.ToString("dd/MM/yyyy")).FontSize(7);
                                table.Cell().Background(bg).Padding(3).Text($"Mouvement {m.Sens} - {m.NumCompte}").FontSize(7);
                                table.Cell().Background(bg).Padding(3).Text(m.DevMVT).FontSize(7);
                                table.Cell().Background(bg).Padding(3).Text(m.Sens == "D" ? m.Montant.ToString("N2") : "").FontSize(7).FontColor(Colors.Red.Darken2).AlignRight();
                                table.Cell().Background(bg).Padding(3).Text(m.Sens == "C" ? m.Montant.ToString("N2") : "").FontSize(7).FontColor(Colors.Green.Darken2).AlignRight();
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("BT DocCenter - Relevé de compte | Page ").FontSize(8).FontColor(Colors.Grey.Darken2);
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateReleveExcelAsync(ReleveRequestDto request)
        {
            _logger.LogInformation("Génération Excel relevé compte #{CompteId}", request.CompteId);

            var (compte, client, mouvements) = await ChargerDonneesAsync(request);
            if (compte == null)
                throw new KeyNotFoundException("Compte introuvable.");

            var (totalCredit, totalDebit, soldeInitial) = CalculerTotaux(compte, mouvements);

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var ws = package.Workbook.Worksheets.Add("Relevé");
                ws.Cells["A1"].Value = "LUMINA FINANCE - RELEVÉ DE COMPTE";
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 14;
                ws.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(0, 25, 68));

                ws.Cells["A2"].Value = client == null ? "" : $"Client : {client.Prenom} {client.Nom} ({client.CodeClient})";
                ws.Cells["A3"].Value = $"Compte : {compte.NumCompte} - Agence {compte.Agence} - Devise {compte.Devise}";
                ws.Cells["A4"].Value = $"Période : {LibellePeriode(request)} - Édité le {DateTime.Now:dd/MM/yyyy HH:mm}";

                ws.Cells["A6"].Value = "Solde d'ouverture";
                ws.Cells["B6"].Value = soldeInitial;
                ws.Cells["C6"].Value = "Total Crédit";
                ws.Cells["D6"].Value = totalCredit;
                ws.Cells["E6"].Value = "Total Débit";
                ws.Cells["F6"].Value = totalDebit;
                ws.Cells["G6"].Value = "Solde actuel";
                ws.Cells["H6"].Value = compte.Solde;
                ws.Cells["A6:H6"].Style.Font.Bold = true;

                var headers = new[] { "Date", "Libellé", "Devise", "Débit", "Crédit" };
                for (var i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cells[8, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 25, 68));
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                var row = 9;
                foreach (var m in mouvements.OrderByDescending(m => m.DateMvt))
                {
                    ws.Cells[row, 1].Value = m.DateMvt.ToString("dd/MM/yyyy");
                    ws.Cells[row, 2].Value = $"Mouvement {m.Sens} - {m.NumCompte}";
                    ws.Cells[row, 3].Value = m.DevMVT;
                    ws.Cells[row, 4].Value = m.Sens == "D" ? (decimal?)m.Montant : null;
                    ws.Cells[row, 5].Value = m.Sens == "C" ? (decimal?)m.Montant : null;
                    row++;
                }

                ws.Cells[row, 1].Value = "TOTAUX";
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 4].Value = totalDebit;
                ws.Cells[row, 4].Style.Font.Bold = true;
                ws.Cells[row, 5].Value = totalCredit;
                ws.Cells[row, 5].Style.Font.Bold = true;

                ws.Cells[1, 1, row, 5].AutoFitColumns();
                package.Save();
            }

            stream.Position = 0;
            return stream.ToArray();
        }

        private async Task<(Compte? Compte, Client? Client, List<Mouvement> Mouvements)> ChargerDonneesAsync(ReleveRequestDto request)
        {
            var compte = await _compteRepository.GetByIdAsync(request.CompteId);
            if (compte == null)
                return (null, null, new List<Mouvement>());

            var client = await _clientRepository.GetByIdAsync(compte.IdClient);
            var (items, _) = await _mouvementRepository.GetByCompteAsync(
                compte.NumCompte, compte.Agence,
                request.DateDebut, request.DateFin, request.Sens,
                request.MontantMin, request.MontantMax,
                1, int.MaxValue);

            return (compte, client, items);
        }

        private static (decimal TotalCredit, decimal TotalDebit, decimal SoldeInitial) CalculerTotaux(Compte compte, List<Mouvement> mouvements)
        {
            var totalCredit = mouvements.Where(m => m.Sens == "C").Sum(m => m.Montant);
            var totalDebit = mouvements.Where(m => m.Sens == "D").Sum(m => m.Montant);
            var soldeInitial = compte.Solde - (totalCredit - totalDebit);
            return (totalCredit, totalDebit, soldeInitial);
        }

        private static string LibellePeriode(ReleveRequestDto request)
        {
            if (request.DateDebut.HasValue || request.DateFin.HasValue)
                return $"{request.DateDebut?.ToString("dd/MM/yyyy") ?? "..."} → {request.DateFin?.ToString("dd/MM/yyyy") ?? "..."}";
            return "Historique complet";
        }
    }
}
