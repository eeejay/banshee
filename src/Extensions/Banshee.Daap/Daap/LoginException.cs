
using System;

namespace Daap {

    public class LoginException : ApplicationException {

        public LoginException (string msg, Exception e) : base (msg, e) {
        }
    }
}
