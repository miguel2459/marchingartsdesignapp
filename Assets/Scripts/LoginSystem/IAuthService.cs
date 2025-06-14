using System;
namespace LoginSystem
{
    public interface IAuthService
    {
        void Login(string email, string password, System.Action<LoginResult> onComplete);
        void SignUp(string name, string email, string password, System.Action<SignUpResult> onComplete);
        void RequestPasswordReset(string email, Action<bool, string> onComplete);

    }
}