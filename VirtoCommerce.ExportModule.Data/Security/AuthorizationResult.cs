namespace VirtoCommerce.ExportModule.Data.Security
{
    public class AuthorizationResult
    {
        public bool Succeeded { get; private set; }

        public static AuthorizationResult Success()
        {
            return new AuthorizationResult() { Succeeded = true };
        }
    }
}
