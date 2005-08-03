using Gtk;
using Gdk;

namespace Sonance
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
			
			DrawRating(window, widget, cell_area, state);
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
			Gdk.Rectangle area, StateType state)
		{
			for(int i = 0; i < Track.Rating; i++)
				canvas.DrawPixbuf(widget.Style.TextGC(state), Star, 0, 0,
					area.X + (i * Star.Width) + 1, area.Y + 1, 
					Star.Width, Star.Height,
					RgbDither.None, 0, 0);
		}
	}
}
