namespace Backend.DTO.Reports
{
    /// Demande de génération d'un relevé de compte (PDF ou Excel).
    public class ReleveRequestDto
    {
        public int CompteId { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string? Sens { get; set; }
        public decimal? MontantMin { get; set; }
        public decimal? MontantMax { get; set; }
    }
}
