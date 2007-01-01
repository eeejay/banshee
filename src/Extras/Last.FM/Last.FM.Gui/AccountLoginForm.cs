/***************************************************************************
 *  AccountLoginForm.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Mono.Unix;
using Gtk;

namespace Last.FM.Gui
{
    public class AccountLoginForm : Gtk.Table
    {
        private Entry username_entry;
        private Entry password_entry;
        private LinkButton signup_button;
        
        private bool is_destroyed = false;
        private bool save_on_edit = false;

        public AccountLoginForm() : base(2, 2, false)
        {
            BorderWidth = 5;
            RowSpacing = 5;
            ColumnSpacing = 5;
        
            Label username_label = new Label(Catalog.GetString("Account Name:"));
            username_label.Xalign = 1.0f;
            username_label.Show();
            
            username_entry = new Entry();
            username_entry.Show();
            
            Label password_label = new Label(Catalog.GetString("Password:"));
            password_label.Xalign = 1.0f;
            password_label.Show();
            
            password_entry = new Entry();
            password_entry.Visibility = false;
            password_entry.Show();
            
            Attach(username_label, 0, 1, 0, 1, AttachOptions.Fill, 
                AttachOptions.Shrink, 0, 0);
            
            Attach(username_entry, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, 
                AttachOptions.Shrink, 0, 0);
            
            Attach(password_label, 0, 1, 1, 2, AttachOptions.Fill, 
                AttachOptions.Shrink, 0, 0);
            
            Attach(password_entry, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, 
                AttachOptions.Shrink, 0, 0);
                
            Account.LoginRequestFinished += OnAccountLoginRequestFinished;
        }
        
        private bool have_requested_on_shown;
        
        protected override void OnShown()
        {
            base.OnShown();
            
            if(!have_requested_on_shown) {
                have_requested_on_shown = true;
                GLib.Timeout.Add(500, delegate {
                    Account.RequestLogin();
                    return false;
                });
            }
        }
        
        protected override void OnDestroyed()
        {
            if(save_on_edit) {
                UpdateLogin();
            }
            
            base.OnDestroyed();
            is_destroyed = true;
        }
        
        public void AddSignUpButton()
        {
            if(signup_button != null) {
                return;
            }
            
            Resize(3, 2);
            signup_button = new LinkButton("Sign Up for Last.fm");
            signup_button.Clicked += delegate { Account.SignUp(); };
            signup_button.Show();
            Attach(signup_button, 1, 2, 2, 3, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
        }
        
        private uint update_login_timeout = 0;
        
        private void OnEntryChanged(object o, EventArgs args)
        {
            if(!save_on_edit || update_login_timeout > 0) {
                return;
            }

            update_login_timeout = GLib.Timeout.Add(1000, delegate {
                UpdateLogin();
                update_login_timeout = 0;
                return false;
            });
        }

        private void UpdateLogin()
        {
            Account.Username = username_entry.Text.Trim();
            Account.Password = password_entry.Text.Trim();
            Account.CommitLogin();
        }

        private void OnAccountLoginRequestFinished(AccountEventArgs args)
        {
            Application.Invoke(delegate { 
                if(is_destroyed) {
                    return;
                }
            
                Sensitive = true;
                
                if(args.Success) {
                    username_entry.Text = Account.Username;
                    password_entry.Text = Account.Password;
                }

                username_entry.Changed += OnEntryChanged;
                password_entry.Changed += OnEntryChanged;
            });    
        }
        
        public bool SaveOnEdit {
            get { return save_on_edit; }
            set { save_on_edit = value; }
        }
        
        public string Username {
            get { return username_entry.Text; }
        }
        
        public string Password {
            get { return password_entry.Text; }
        }
    }
}

