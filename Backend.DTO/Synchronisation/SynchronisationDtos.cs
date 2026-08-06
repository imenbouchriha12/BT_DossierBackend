namespace Backend.DTO.Synchronisation
{
    public class SynchronisationResultDto
    {
        public int Id { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int NbInsert { get; set; }
        public int NbUpdate { get; set; }
        public int NbRejet { get; set; }
        public long DureeMs { get; set; }
        public string Declencheur { get; set; } = string.Empty;
        public string? MessageErreur { get; set; }
    }

    public class SynchronisationStateDto
    {
        public bool EnCours { get; set; }
        public SynchronisationResultDto? Derniere { get; set; }
        public int TotalRejetsNonResolus { get; set; }
        public DateTime? ProchaineExecution { get; set; }
    }

    public class SynchronisationRejetDto
    {
        public int Id { get; set; }
        public int IdSynchronisation { get; set; }
        public string Fichier { get; set; } = string.Empty;
        public int? NumeroLigne { get; set; }
        public string Motif { get; set; } = string.Empty;
        public string ContenuBrut { get; set; } = string.Empty;
    }
}
