# © 2008 Novell Inc. Licensed under the MIT X11 license.
# Written by Aaron Bockover <abockover@novell.com>
#
# This script demonstrates how to override the default formatting
# options for the duration segment of source status messages.
#
# There is no limit to the number of formatters. Users will be
# able to cycle between all of them in the UI.

import System
import System.Text
import Banshee.Sources

formatters = Source.DurationStatusFormatters

# We hate all of the crappy Banshee default duration
# formatter since they are for normal earthlings
formatters.Clear ()

# Now add our own so we can actually make use of the data
formatters.Add ({ builder as StringBuilder, span as TimeSpan |
    builder.Append ("${span.TotalMilliseconds}ms")
})

formatters.Add ({ builder as StringBuilder, span as TimeSpan |
    builder.Append ("${span.Ticks} ticks")
})

# Actually, they do have one that isn't *too* bad
formatters.Add (DurationStatusFormatters.ConfusingPreciseFormatter)
