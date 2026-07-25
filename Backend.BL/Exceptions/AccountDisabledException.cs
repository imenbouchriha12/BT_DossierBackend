namespace Backend.BL.Exceptions
{
    public class AccountDisabledException : UnauthorizedAccessException
    {
        public AccountDisabledException()
            : base("Votre compte a été désactivé. Veuillez contacter l'administrateur.") { }

        public AccountDisabledException(string message)
            : base(message) { }
    }
}
