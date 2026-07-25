namespace Backend.DTO.Reports
{
    public class ReportRequestDto
    {
        public string Format { get; set; } = "pdf";
        public string? Ville { get; set; }
        public string? Statut { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
    }
}
