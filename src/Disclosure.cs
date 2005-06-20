
using System;
using Gtk;

namespace Sonance
{
	public class Disclosure : CheckButton
	{
		private Widget container;
		private string shown;
		private string hidden;
	
		private int expand_id;
		private ExpanderStyle style;

		private int expander_size;
		private int direction;
		
		private void GetXY(out int x, out int y, out StateType state_type)
		{
			CheckButton check_button;
			int indicator_size, indicator_spacing;
			int focus_width;
			int focus_pad;
			bool interior_focus;
			Bin bin = this as Bin;
			int width;
	
			if(Visible && IsMapped) {
				check_button = this as CheckButton;
		
		
		
		gtk_widget_style_get (GTK_WIDGET (check_button),
				      "indicator_size", &indicator_size,
				      "indicator_spacing", &indicator_spacing,
				      NULL);

		gtk_widget_style_get (widget,
				      "interior_focus", &interior_focus,
				      "focus-line-width", &focus_width,
				      "focus-padding", &focus_pad,
				      NULL);
		
		*state_type = GTK_WIDGET_STATE (widget);
		if ((*state_type != GTK_STATE_NORMAL) &&
		    (*state_type != GTK_STATE_PRELIGHT)) {
			*state_type = GTK_STATE_NORMAL;
		}

		if (bin->child) {
			width = indicator_spacing * 3 + indicator_size ;
		} else {
			width = widget->allocation.width - 2 * GTK_CONTAINER (widget)->border_width;
		}
		
		*x = widget->allocation.x + GTK_CONTAINER (widget)->border_width + (width) / 2;
		*y = widget->allocation.y + widget->allocation.height / 2;

		if (interior_focus == FALSE) {
			*x += focus_width + focus_pad;
		}

		*state_type = GTK_WIDGET_STATE (widget) == GTK_STATE_ACTIVE ? GTK_STATE_NORMAL : GTK_WIDGET_STATE (widget);

		if (gtk_widget_get_direction (widget) == GTK_TEXT_DIR_RTL) {
			*x = widget->allocation.x + widget->allocation.width - (*x - widget->allocation.x);
		}
	} else {
		*x = 0;
		*y = 0;
		*state_type = GTK_STATE_NORMAL;
	}
}
		
	}
}
