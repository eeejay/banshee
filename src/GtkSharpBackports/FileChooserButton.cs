namespace GtkSharpBackports {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

	public  class FileChooserButton : Gtk.HBox, Gtk.FileChooser {

		~FileChooserButton()
		{
			Dispose();
		}

		[Obsolete]
		protected FileChooserButton(GLib.GType gtype) : base(gtype) {}
		public FileChooserButton(IntPtr raw) : base(raw) {}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_button_new(IntPtr title, int action);

		public FileChooserButton (string title, Gtk.FileChooserAction action) : base (IntPtr.Zero)
		{
			if (GetType () != typeof (FileChooserButton)) {
				throw new InvalidOperationException ("Can't override this constructor.");
			}
			IntPtr title_as_native = GLib.Marshaller.StringToPtrGStrdup (title);
			Raw = gtk_file_chooser_button_new(title_as_native, (int) action);
			GLib.Marshaller.Free (title_as_native);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_button_new_with_backend(IntPtr title, int action, IntPtr backend);

		public FileChooserButton (string title, Gtk.FileChooserAction action, string backend) : base (IntPtr.Zero)
		{
			if (GetType () != typeof (FileChooserButton)) {
				throw new InvalidOperationException ("Can't override this constructor.");
			}
			IntPtr title_as_native = GLib.Marshaller.StringToPtrGStrdup (title);
			IntPtr backend_as_native = GLib.Marshaller.StringToPtrGStrdup (backend);
			Raw = gtk_file_chooser_button_new_with_backend(title_as_native, (int) action, backend_as_native);
			GLib.Marshaller.Free (title_as_native);
			GLib.Marshaller.Free (backend_as_native);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_button_new_with_dialog(IntPtr dialog);

		public FileChooserButton (Gtk.Widget dialog) : base (IntPtr.Zero)
		{
			if (GetType () != typeof (FileChooserButton)) {
				ArrayList vals = new ArrayList();
				ArrayList names = new ArrayList();
				if (dialog != null) {
					names.Add ("dialog");
					vals.Add (new GLib.Value (dialog));
				}
				CreateNativeObject ((string[])names.ToArray (typeof (string)), (GLib.Value[])vals.ToArray (typeof (GLib.Value)));
				return;
			}
			Raw = gtk_file_chooser_button_new_with_dialog(dialog == null ? IntPtr.Zero : dialog.Handle);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_button_get_title(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_button_set_title(IntPtr raw, IntPtr title);

		[GLib.Property ("title")]
		public string Title {
			get  {
				IntPtr raw_ret = gtk_file_chooser_button_get_title(Handle);
				string ret = GLib.Marshaller.Utf8PtrToString (raw_ret);
				return ret;
			}
			set  {
				IntPtr title_as_native = GLib.Marshaller.StringToPtrGStrdup (value);
				gtk_file_chooser_button_set_title(Handle, title_as_native);
				GLib.Marshaller.Free (title_as_native);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern int gtk_file_chooser_button_get_width_chars(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_button_set_width_chars(IntPtr raw, int n_chars);

		[GLib.Property ("width-chars")]
		public int WidthChars {
			get  {
				int raw_ret = gtk_file_chooser_button_get_width_chars(Handle);
				int ret = raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_button_set_width_chars(Handle, value);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_button_get_type();

		public static new GLib.GType GType { 
			get {
				IntPtr raw_ret = gtk_file_chooser_button_get_type();
				GLib.GType ret = new GLib.GType(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_set_current_folder_uri(IntPtr raw, IntPtr uri);

		public bool SetCurrentFolderUri(string uri) {
			IntPtr uri_as_native = GLib.Marshaller.StringToPtrGStrdup (uri);
			bool raw_ret = gtk_file_chooser_set_current_folder_uri(Handle, uri_as_native);
			bool ret = raw_ret;
			GLib.Marshaller.Free (uri_as_native);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_list_shortcut_folder_uris(IntPtr raw);

		public string[] ShortcutFolderUris { 
			get {
				IntPtr raw_ret = gtk_file_chooser_list_shortcut_folder_uris(Handle);
				string[] ret = (string[]) GLib.Marshaller.ListToArray (new GLib.SList(raw_ret, typeof (string), true, true), typeof (string));
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_list_filters(IntPtr raw);

		public Gtk.FileFilter[] Filters { 
			get {
				IntPtr raw_ret = gtk_file_chooser_list_filters(Handle);
				Gtk.FileFilter[] ret = (Gtk.FileFilter[]) GLib.Marshaller.ListToArray (new GLib.SList(raw_ret, typeof (Gtk.FileFilter), true, false), typeof (Gtk.FileFilter));
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_unselect_all(IntPtr raw);

		public void UnselectAll() {
			gtk_file_chooser_unselect_all(Handle);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_current_folder_uri(IntPtr raw);

		public string CurrentFolderUri { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_current_folder_uri(Handle);
				string ret = GLib.Marshaller.PtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_set_filename(IntPtr raw, IntPtr filename);

		public bool SetFilename(string filename) {
			IntPtr filename_as_native = GLib.Marshaller.StringToFilenamePtr (filename);
			bool raw_ret = gtk_file_chooser_set_filename(Handle, filename_as_native);
			bool ret = raw_ret;
			GLib.Marshaller.Free (filename_as_native);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_current_name(IntPtr raw, IntPtr name);

		public string CurrentName { 
			set {
				IntPtr name_as_native = GLib.Marshaller.StringToPtrGStrdup (value);
				gtk_file_chooser_set_current_name(Handle, name_as_native);
				GLib.Marshaller.Free (name_as_native);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_add_shortcut_folder_uri(IntPtr raw, IntPtr uri, out IntPtr error);

		public bool AddShortcutFolderUri(string uri) {
			IntPtr uri_as_native = GLib.Marshaller.StringToPtrGStrdup (uri);
			IntPtr error = IntPtr.Zero;
			bool raw_ret = gtk_file_chooser_add_shortcut_folder_uri(Handle, uri_as_native, out error);
			bool ret = raw_ret;
			GLib.Marshaller.Free (uri_as_native);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_select_all(IntPtr raw);

		public void SelectAll() {
			gtk_file_chooser_select_all(Handle);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_remove_shortcut_folder_uri(IntPtr raw, IntPtr uri, out IntPtr error);

		public bool RemoveShortcutFolderUri(string uri) {
			IntPtr uri_as_native = GLib.Marshaller.StringToPtrGStrdup (uri);
			IntPtr error = IntPtr.Zero;
			bool raw_ret = gtk_file_chooser_remove_shortcut_folder_uri(Handle, uri_as_native, out error);
			bool ret = raw_ret;
			GLib.Marshaller.Free (uri_as_native);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_remove_filter(IntPtr raw, IntPtr filter);

		public void RemoveFilter(Gtk.FileFilter filter) {
			gtk_file_chooser_remove_filter(Handle, filter == null ? IntPtr.Zero : filter.Handle);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_preview_filename(IntPtr raw);

		public string PreviewFilename { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_preview_filename(Handle);
				string ret = GLib.Marshaller.FilenamePtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_add_shortcut_folder(IntPtr raw, IntPtr folder, out IntPtr error);

		public bool AddShortcutFolder(string folder) {
			IntPtr folder_as_native = GLib.Marshaller.StringToFilenamePtr (folder);
			IntPtr error = IntPtr.Zero;
			bool raw_ret = gtk_file_chooser_add_shortcut_folder(Handle, folder_as_native, out error);
			bool ret = raw_ret;
			GLib.Marshaller.Free (folder_as_native);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_select_filename(IntPtr raw, IntPtr filename);

		public bool SelectFilename(string filename) {
			IntPtr filename_as_native = GLib.Marshaller.StringToFilenamePtr (filename);
			bool raw_ret = gtk_file_chooser_select_filename(Handle, filename_as_native);
			bool ret = raw_ret;
			GLib.Marshaller.Free (filename_as_native);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_uri(IntPtr raw);

		public string Uri { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_uri(Handle);
				string ret = GLib.Marshaller.PtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_unselect_filename(IntPtr raw, IntPtr filename);

		public void UnselectFilename(string filename) {
			IntPtr filename_as_native = GLib.Marshaller.StringToFilenamePtr (filename);
			gtk_file_chooser_unselect_filename(Handle, filename_as_native);
			GLib.Marshaller.Free (filename_as_native);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_current_folder(IntPtr raw);

		public string CurrentFolder { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_current_folder(Handle);
				string ret = GLib.Marshaller.FilenamePtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_uris(IntPtr raw);

		public string[] Uris { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_uris(Handle);
				string[] ret = (string[]) GLib.Marshaller.ListToArray (new GLib.SList(raw_ret, typeof (string), true, true), typeof (string));
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_add_filter(IntPtr raw, IntPtr filter);

		public void AddFilter(Gtk.FileFilter filter) {
			gtk_file_chooser_add_filter(Handle, filter == null ? IntPtr.Zero : filter.Handle);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_filename(IntPtr raw);

		public string Filename { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_filename(Handle);
				string ret = GLib.Marshaller.FilenamePtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_preview_uri(IntPtr raw);

		public string PreviewUri { 
			get {
				IntPtr raw_ret = gtk_file_chooser_get_preview_uri(Handle);
				string ret = GLib.Marshaller.PtrToStringGFree(raw_ret);
				return ret;
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_remove_shortcut_folder(IntPtr raw, IntPtr folder, out IntPtr error);

		public bool RemoveShortcutFolder(string folder) {
			IntPtr folder_as_native = GLib.Marshaller.StringToFilenamePtr (folder);
			IntPtr error = IntPtr.Zero;
			bool raw_ret = gtk_file_chooser_remove_shortcut_folder(Handle, folder_as_native, out error);
			bool ret = raw_ret;
			GLib.Marshaller.Free (folder_as_native);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_unselect_uri(IntPtr raw, IntPtr uri);

		public void UnselectUri(string uri) {
			IntPtr uri_as_native = GLib.Marshaller.StringToPtrGStrdup (uri);
			gtk_file_chooser_unselect_uri(Handle, uri_as_native);
			GLib.Marshaller.Free (uri_as_native);
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_set_current_folder(IntPtr raw, IntPtr filename);

		public bool SetCurrentFolder(string filename) {
			IntPtr filename_as_native = GLib.Marshaller.StringToFilenamePtr (filename);
			bool raw_ret = gtk_file_chooser_set_current_folder(Handle, filename_as_native);
			bool ret = raw_ret;
			GLib.Marshaller.Free (filename_as_native);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_select_uri(IntPtr raw, IntPtr uri);

		public bool SelectUri(string uri) {
			IntPtr uri_as_native = GLib.Marshaller.StringToPtrGStrdup (uri);
			bool raw_ret = gtk_file_chooser_select_uri(Handle, uri_as_native);
			bool ret = raw_ret;
			GLib.Marshaller.Free (uri_as_native);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_set_uri(IntPtr raw, IntPtr uri);

		public bool SetUri(string uri) {
			IntPtr uri_as_native = GLib.Marshaller.StringToPtrGStrdup (uri);
			bool raw_ret = gtk_file_chooser_set_uri(Handle, uri_as_native);
			bool ret = raw_ret;
			GLib.Marshaller.Free (uri_as_native);
			return ret;
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_get_use_preview_label(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_use_preview_label(IntPtr raw, bool use_label);

		[GLib.Property ("use-preview-label")]
		public bool UsePreviewLabel {
			get  {
				bool raw_ret = gtk_file_chooser_get_use_preview_label(Handle);
				bool ret = raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_set_use_preview_label(Handle, value);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_get_preview_widget_active(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_preview_widget_active(IntPtr raw, bool active);

		[GLib.Property ("preview-widget-active")]
		public bool PreviewWidgetActive {
			get  {
				bool raw_ret = gtk_file_chooser_get_preview_widget_active(Handle);
				bool ret = raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_set_preview_widget_active(Handle, value);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern int gtk_file_chooser_get_action(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_action(IntPtr raw, int action);

		[GLib.Property ("action")]
		public Gtk.FileChooserAction Action {
			get  {
				int raw_ret = gtk_file_chooser_get_action(Handle);
				Gtk.FileChooserAction ret = (Gtk.FileChooserAction) raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_set_action(Handle, (int) value);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_extra_widget(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_extra_widget(IntPtr raw, IntPtr extra_widget);

		[GLib.Property ("extra-widget")]
		public Gtk.Widget ExtraWidget {
			get  {
				IntPtr raw_ret = gtk_file_chooser_get_extra_widget(Handle);
				Gtk.Widget ret = GLib.Object.GetObject(raw_ret) as Gtk.Widget;
				return ret;
			}
			set  {
				gtk_file_chooser_set_extra_widget(Handle, value == null ? IntPtr.Zero : value.Handle);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_get_show_hidden(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_show_hidden(IntPtr raw, bool show_hidden);

		[GLib.Property ("show-hidden")]
		public bool ShowHidden {
			get  {
				bool raw_ret = gtk_file_chooser_get_show_hidden(Handle);
				bool ret = raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_set_show_hidden(Handle, value);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_get_local_only(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_local_only(IntPtr raw, bool local_only);

		[GLib.Property ("local-only")]
		public bool LocalOnly {
			get  {
				bool raw_ret = gtk_file_chooser_get_local_only(Handle);
				bool ret = raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_set_local_only(Handle, value);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_filter(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_filter(IntPtr raw, IntPtr filter);

		[GLib.Property ("filter")]
		public Gtk.FileFilter Filter {
			get  {
				IntPtr raw_ret = gtk_file_chooser_get_filter(Handle);
				Gtk.FileFilter ret = GLib.Object.GetObject(raw_ret) as Gtk.FileFilter;
				return ret;
			}
			set  {
				gtk_file_chooser_set_filter(Handle, value == null ? IntPtr.Zero : value.Handle);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_preview_widget(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_preview_widget(IntPtr raw, IntPtr preview_widget);

		[GLib.Property ("preview-widget")]
		public Gtk.Widget PreviewWidget {
			get  {
				IntPtr raw_ret = gtk_file_chooser_get_preview_widget(Handle);
				Gtk.Widget ret = GLib.Object.GetObject(raw_ret) as Gtk.Widget;
				return ret;
			}
			set  {
				gtk_file_chooser_set_preview_widget(Handle, value == null ? IntPtr.Zero : value.Handle);
			}
		}

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern bool gtk_file_chooser_get_select_multiple(IntPtr raw);

		[DllImport("libgtk-win32-2.0-0.dll")]
		static extern void gtk_file_chooser_set_select_multiple(IntPtr raw, bool select_multiple);

		[GLib.Property ("select-multiple")]
		public bool SelectMultiple {
			get  {
				bool raw_ret = gtk_file_chooser_get_select_multiple(Handle);
				bool ret = raw_ret;
				return ret;
			}
			set  {
				gtk_file_chooser_set_select_multiple(Handle, value);
			}
		}

		[GLib.CDeclCallback]
		delegate void SelectionChangedVMDelegate (IntPtr chooser);

		static SelectionChangedVMDelegate SelectionChangedVMCallback;

		static void selectionchanged_cb (IntPtr chooser)
		{
			FileChooserButton chooser_managed = GLib.Object.GetObject (chooser, false) as FileChooserButton;
			chooser_managed.OnSelectionChanged ();
		}

		private static void OverrideSelectionChanged (GLib.GType gtype)
		{
			if (SelectionChangedVMCallback == null)
				SelectionChangedVMCallback = new SelectionChangedVMDelegate (selectionchanged_cb);
			OverrideVirtualMethod (gtype, "selection-changed", SelectionChangedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(GtkSharpBackports.FileChooserButton), ConnectionMethod="OverrideSelectionChanged")]
		protected virtual void OnSelectionChanged ()
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (1);
			GLib.Value[] vals = new GLib.Value [1];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
			foreach (GLib.Value v in vals)
				v.Dispose ();
		}

		[GLib.Signal("selection-changed")]
		public event System.EventHandler SelectionChanged {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "selection-changed");
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "selection-changed");
				sig.RemoveDelegate (value);
			}
		}

		[GLib.CDeclCallback]
		delegate void FileActivatedVMDelegate (IntPtr chooser);

		static FileActivatedVMDelegate FileActivatedVMCallback;

		static void fileactivated_cb (IntPtr chooser)
		{
			FileChooserButton chooser_managed = GLib.Object.GetObject (chooser, false) as FileChooserButton;
			chooser_managed.OnFileActivated ();
		}

		private static void OverrideFileActivated (GLib.GType gtype)
		{
			if (FileActivatedVMCallback == null)
				FileActivatedVMCallback = new FileActivatedVMDelegate (fileactivated_cb);
			OverrideVirtualMethod (gtype, "file-activated", FileActivatedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(GtkSharpBackports.FileChooserButton), ConnectionMethod="OverrideFileActivated")]
		protected virtual void OnFileActivated ()
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (1);
			GLib.Value[] vals = new GLib.Value [1];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
			foreach (GLib.Value v in vals)
				v.Dispose ();
		}

		[GLib.Signal("file-activated")]
		public event System.EventHandler FileActivated {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "file-activated");
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "file-activated");
				sig.RemoveDelegate (value);
			}
		}

		[GLib.CDeclCallback]
		delegate void UpdatePreviewVMDelegate (IntPtr chooser);

		static UpdatePreviewVMDelegate UpdatePreviewVMCallback;

		static void updatepreview_cb (IntPtr chooser)
		{
			FileChooserButton chooser_managed = GLib.Object.GetObject (chooser, false) as FileChooserButton;
			chooser_managed.OnUpdatePreview ();
		}

		private static void OverrideUpdatePreview (GLib.GType gtype)
		{
			if (UpdatePreviewVMCallback == null)
				UpdatePreviewVMCallback = new UpdatePreviewVMDelegate (updatepreview_cb);
			OverrideVirtualMethod (gtype, "update-preview", UpdatePreviewVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(GtkSharpBackports.FileChooserButton), ConnectionMethod="OverrideUpdatePreview")]
		protected virtual void OnUpdatePreview ()
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (1);
			GLib.Value[] vals = new GLib.Value [1];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
			foreach (GLib.Value v in vals)
				v.Dispose ();
		}

		[GLib.Signal("update-preview")]
		public event System.EventHandler UpdatePreview {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "update-preview");
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "update-preview");
				sig.RemoveDelegate (value);
			}
		}

		[GLib.CDeclCallback]
		delegate void CurrentFolderChangedVMDelegate (IntPtr chooser);

		static CurrentFolderChangedVMDelegate CurrentFolderChangedVMCallback;

		static void currentfolderchanged_cb (IntPtr chooser)
		{
			FileChooserButton chooser_managed = GLib.Object.GetObject (chooser, false) as FileChooserButton;
			chooser_managed.OnCurrentFolderChanged ();
		}

		private static void OverrideCurrentFolderChanged (GLib.GType gtype)
		{
			if (CurrentFolderChangedVMCallback == null)
				CurrentFolderChangedVMCallback = new CurrentFolderChangedVMDelegate (currentfolderchanged_cb);
			OverrideVirtualMethod (gtype, "current-folder-changed", CurrentFolderChangedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(GtkSharpBackports.FileChooserButton), ConnectionMethod="OverrideCurrentFolderChanged")]
		protected virtual void OnCurrentFolderChanged ()
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (1);
			GLib.Value[] vals = new GLib.Value [1];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
			foreach (GLib.Value v in vals)
				v.Dispose ();
		}

		[GLib.Signal("current-folder-changed")]
		public event System.EventHandler CurrentFolderChanged {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "current-folder-changed");
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "current-folder-changed");
				sig.RemoveDelegate (value);
			}
		}

		[DllImport ("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_get_filenames (IntPtr raw);

		public string[] Filenames {
			get {
				IntPtr raw_ret = gtk_file_chooser_get_filenames (Handle);
				GLib.SList list = new GLib.SList (raw_ret, typeof (GLib.ListBase.FilenameString), true, true);
				return (string[]) GLib.Marshaller.ListToArray (list, typeof (string));
			}
		}

		[DllImport ("libgtk-win32-2.0-0.dll")]
		static extern IntPtr gtk_file_chooser_list_shortcut_folders (IntPtr raw);

		public string[] ShortcutFolders {
			get {
				IntPtr raw_ret = gtk_file_chooser_list_shortcut_folders (Handle);
				GLib.SList list = new GLib.SList (raw_ret, typeof (GLib.ListBase.FilenameString), true, true);
				return (string[]) GLib.Marshaller.ListToArray (list, typeof (string));
			}
		}
	}
}
