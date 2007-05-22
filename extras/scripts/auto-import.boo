import System
import System.IO
import Gtk
import Banshee.Gui
import Banshee.Widgets
import Banshee.Base
import Banshee.Library

class AutoImporter:
	# Set to false if you always want to auto-import without asking
	private ask_me = true

	class AutoImportMessageDialog(HigMessageDialog):
		def constructor(): 
			super(InterfaceElements.MainWindow, 
				DialogFlags.Modal, MessageType.Info, 
				"Would you like to rescan your library?",
				"Rescanning your library may take a long time.",
				"Rescan")

	def ImportFinished(o as object, args as EventArgs):
		ImportManager.Instance.ImportFinished -= ImportFinished
		Banshee.Sources.ImportErrorsSource.Instance.Unmap()

	def Timeout() as bool:
		location = Globals.Library.Location
		if not Directory.Exists(location):
			location = Paths.DefaultLibraryPath

		ImportManager.Instance.ImportFinished += ImportFinished

		if not ask_me:
			Import.QueueSource(location)
			return false

		dialog = AutoImportMessageDialog()
		try:
			if dialog.Run() == ResponseType.Ok:
				Import.QueueSource(location)
		ensure:
			dialog.Destroy()

		return false

	def AutoImport(o as object, args as EventArgs):
		GLib.Timeout.Add(1500, Timeout)
		Globals.Library.Reloaded -= AutoImport

	def constructor():
		if Globals.UIManager.IsInitialized:
			AutoImport(null, EventArgs.Empty)
		else:
			Globals.UIManager.Initialized += AutoImport

AutoImporter()

