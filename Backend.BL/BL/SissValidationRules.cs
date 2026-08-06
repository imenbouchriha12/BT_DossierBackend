using System.Security.Cryptography;
using System.Text;

namespace Backend.BL.BL
{
    /// Règles métier partagées entre le SISS et l'application (aucune divergence).
    public static class SissValidationRules
    {
        public static readonly string[] SexesValides = { "H", "F" };

        public static readonly string[] TypesDocumentValides = { "CIN", "Passeport", "JustifDomicile", "JustifRevenu", "Autre" };

        public const int AgeMinimum = 18;

        public const int LongueurCodeClient = 10;

        public static bool IsCodeClientValide(string? code)
        {
            return !string.IsNullOrWhiteSpace(code)
                && code.Length == LongueurCodeClient
                && code.All(char.IsDigit);
        }

        public static bool IsSexeValide(string? sexe)
        {
            return sexe is "H" or "F";
        }

        public static bool IsMajeur(DateTime dateNaissance)
        {
            return dateNaissance <= DateTime.Today.AddYears(-AgeMinimum);
        }

        public static bool IsTypeDocumentValide(string? type)
        {
            return !string.IsNullOrWhiteSpace(type) && TypesDocumentValides.Contains(type);
        }

        public static string CalculerHashReferentiel(
            string nom, string prenom, DateTime dateNaissance, string sexe,
            string nationalite, string telephone, string email, string adresse, string ville)
        {
            var source = string.Join('|',
                nom, prenom, dateNaissance.ToString("yyyy-MM-dd"), sexe,
                nationalite, telephone, email, adresse, ville)
                .Trim().ToUpperInvariant();

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
            return Convert.ToHexString(bytes);
        }
    }
}
