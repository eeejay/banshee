using Gtk;
using Gdk;

namespace Banshee
{
	public class RatingRenderer : CellRenderer
	{
		static private Pixbuf star;
		static private Pixbuf circle;
		
		public TrackInfo Track;
		
		public static Pixbuf Star
		{
			get {
				if(star == null)
					star = Gdk.Pixbuf.LoadFromResource("star.png");
					
				return star;
			}
		}
		
		public static Pixbuf Circle
		{
			get {
				if(circle == null)
					circle = Gdk.Pixbuf.LoadFromResource("circle.png");
					
				return circle;
			}
		}
		
		public RatingRenderer()
		{
			
		}

		protected RatingRenderer(System.IntPtr ptr) : base(ptr)
		{
		
		}
		
		~RatingRenderer()
		{
			Dispose();
		}
		
		private StateType RendererStateToWidgetState(CellRendererState flags)
		{
			StateType state = StateType.Normal;
			
			if((CellRendererState.Insensitive & flags).Equals(
				CellRendererState.Insensitive)) {
				state = StateType.Insensitive;
			} else if((CellRendererState.Selected & flags).Equals( 
				CellRendererState.Selected)) {
				state = StateType.Selected;
			}
			
			return state;
		}
		
		protected override void Render(Gdk.Drawable drawable, 
			Widget widget, Gdk.Rectangle background_area, 
			Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, 
			CellRendererState flags)
		{
			Gdk.Window window = drawable as Gdk.Window;
			StateType state = RendererStateToWidgetState(flags);
			
			DrawRating(window, widget, cell_area, state, flags);
		}
		
		public override void GetSize(Widget widget, ref Gdk.Rectangle cell_area, 
			out int x_offset, out int y_offset, out int width, out int height)
		{
			height = Star.Height + 2;
			width = (Star.Width * 5) + 4;
			x_offset = 0;
			y_offset = 0;
		}
	
		private void DrawRating(Gdk.Window canvas, Gtk.Widget widget,
			Gdk.Rectangle area, StateType state, CellRendererState flags)
		{
			
			/*Point [] starPoints = {
				new Point(area.X + 9, area.Y),
				new Point(area.X + 5 , area.Y + 4),
				new Point(area.X, area.Y + 4),
				new Point(area.X + 5, area.Y + 8),
				new Point(area.X + 1, area.Y + 13),
				new Point(area.X + 9, area.Y + 10),
				new Point(area.X + 17, area.Y + 15),
				new Point(area.X + 16, area.Y + 10),
				new Point(area.X + 9, area.Y),
			};*/
			
			int rating = (int)Track.Rating;
		//	int CursorX = (widget as PlaylistView).CursorX;
			//int CursorY = (widget as PlaylistView).CursorY;
			
			/*if(CursorY >= area.Y && CursorY <= area.Y + area.Height &&
				CursorX >= area.X && CursorX <= area.X + area.Width) {
				int offset = CursorX - area.X;
				rating = offset / (Star.Width + 1);
			}*/
			
			for(int i = 0; i < rating; i++) {
				canvas.DrawPixbuf(widget.Style.TextGC(state), Star, 0, 0,
					area.X + (i * Star.Width) + 1, area.Y + 1, 
					Star.Width, Star.Height,
					RgbDither.None, 0, 0);
			}
			
			/*if((flags & CellRendererState.Prelit) == CellRendererState.Prelit) {
				for(int i = (int)Track.Rating; i < 5; i++) {
					canvas.DrawPixbuf(widget.Style.TextGC(state), Circle, 0, 0,
						area.X + (i * Star.Width) + (Star.Width / 2) - 1, 
						area.Y + (area.Height - Circle.Height) / 2, 
						Circle.Width, Circle.Height,
						RgbDither.None, 0, 0);
				}
			}*/
		}
	}
}
