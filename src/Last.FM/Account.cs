using System;
using System.Collections;

using Gnome.Keyring;

namespace Last.FM
{
	public static class Account
	{
		public class AsyncRequest
		{	
			private Exception exception;

			public bool Success {
				get { return exception == null; }
			}

			public Exception Exception {
				get { return exception; }
				internal set { exception = value; }
			}
		}
	
		public class LoginRequest : AsyncRequest
		{
			private string username;
			private string password;
			
			internal LoginRequest()
			{
			}

			internal LoginRequest(string username, string password)
			{
				this.username = username;
				this.password = password;
			}

			public string Username {
				get { return username; }
				internal set { username = value; }
			}

			public string Password {
				get { return password; }
				internal set { password = value; }
			}
		}
	
		private const string keyring_item_name = "Last.fm Account";
		private static Hashtable request_attributes = new Hashtable();

		static Account()
		{
			request_attributes["name"] = keyring_item_name;
		}
		
		private delegate void RequestLoginHandler(LoginRequest login);
		private delegate void UpdateLoginHandler(AsyncRequest request, string username, string password);
		
		public static LoginRequest RequestLogin()
		{
			LoginRequest login = new LoginRequest();
			RequestLogin(login);
			
			if(!login.Success) {
				throw login.Exception;
			}
			
			return login;
		}
		
		private static void RequestLogin(LoginRequest login)
		{	
			try {
				foreach(ItemData result in Ring.Find(ItemType.GenericSecret, request_attributes)) {
					if(!result.Attributes.ContainsKey("name") || !result.Attributes.ContainsKey("username") || 
						(result.Attributes["name"] as string) != keyring_item_name) {
						continue;
					}

					string username = (string)result.Attributes["username"];
					string password = result.Secret;

					if(username == null || username == String.Empty || password == null || password == String.Empty) {
						throw new ApplicationException("Invalid username/password in keyring");
					}

					login.Username = username;
					login.Password = password;
					
					return;
				}

				throw new ApplicationException("Last.fm account information not found in default keyring");
			} catch(Exception e) {
				login.Exception = e;
			}	
		}

		public static void UpdateLogin(string username, string password)
		{
			AsyncRequest request = new AsyncRequest();
			UpdateLogin(request, username, password);
			if(!request.Success) {
				throw request.Exception;
			}
		}

		private static void UpdateLogin(AsyncRequest request, string username, string password)
		{
			try {
				string keyring = Ring.GetDefaultKeyring();
				Hashtable update_request_attributes = request_attributes.Clone() as Hashtable;
				update_request_attributes["username"] = username;

				Ring.CreateItem(keyring, ItemType.GenericSecret, keyring_item_name, 
					update_request_attributes, password, true);
			} catch(Exception e) {
				request.Exception = e;
			}
		}

		public static void RequestLoginAsync(AsyncCallback callback)
		{
			LoginRequest login = new LoginRequest();
			RequestLoginHandler handler = new RequestLoginHandler(RequestLogin);
			handler.BeginInvoke(login, callback, login);
		}

		public static void UpdateLoginAsync(string username, string password, AsyncCallback callback)
		{
			AsyncRequest request = new AsyncRequest();
			UpdateLoginHandler handler = new UpdateLoginHandler(UpdateLogin);
			handler.BeginInvoke(request, username, password, callback, request);
		}
	}
}

