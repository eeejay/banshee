// See /System/Library/Frameworks/CoreServices.framework/Frameworks/CarbonCore.framework/Headers/Script.h for language IDs.
data 'LPic' (5000) {
  // Default language ID, 0 = English
  $"0000"
  // Number of entries in list
  $"0001"

  // Entry 1
  // Language ID, 0 = English
  $"0000"
  // Resource ID, 0 = STR#/TEXT/styl 5000
  $"0000"
  // Multibyte language, 0 = no
  $"0000"
};

resource 'STR#' (5000, "English") {
  {
    // Language (unused?) = English
    "English",
    // Accept (Agree)
    "Accept",
    // Decline (Disagree)
    "Decline",
    // Print, ellipsis is 0xC9
    "Print…",
    // Save As, ellipsis is 0xC9
    "Save As…",
    // Descriptive text, curly quotes are 0xD2 and 0xD3
    "You are about to install the\n"
    "Banshee Media Player.\n"
    "\n"
    "Please read the license agreement.  If you agree to its terms and accept, click “Accept” to access the software.  Otherwise, click “Decline” to cancel."
  };
};

// Beware of 1024(?) byte (character?) line length limitation.  Split up long
// lines.
// If straight quotes are used ("), remember to escape them (\").
// Newline is \n, to leave a blank line, use two of them.
// 0xD2 and 0xD3 are curly double-quotes ("), 0xD4 and 0xD5 are curly
//   single quotes ('), 0xD5 is also the apostrophe.
data 'TEXT' (5000, "English") {
  "BANSHEE MEDIA PLAYER END-USER SOFTWARE LICENSE AGREEMENT\n"
  "\n"
  "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n"
  "\n"
  "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n"
  "\n"
  "THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
};

data 'styl' (5000, "English") {
  // Number of styles following = 2
  $"0002"

  // Style 1.  This is used to display the header lines in bold text.
  // Start character = 0
  $"0000 0000"
  // Height = 16
  $"0010"
  // Ascent = 12
  $"000C"
  // Font family = 1024 (Lucida Grande)
  $"0400"
  // Style bitfield, 0x1=bold 0x2=italic 0x4=underline 0x8=outline
  // 0x10=shadow 0x20=condensed 0x40=extended
  $"01"
  // Style, unused?
  $"02"
  // Size = 12 point
  $"000C"
  // Color, RGB
  $"0000 0000 0000"

  // Style 2.  This is used to display the body.
  // Start character = 56
  $"0000 0038"
  // Height = 16
  $"0010"
  // Ascent = 12
  $"000C"
  // Font family = 1024 (Lucida Grande)
  $"0400"
  // Style bitfield, 0x1=bold 0x2=italic 0x4=underline 0x8=outline
  // 0x10=shadow 0x20=condensed 0x40=extended
  $"00"
  // Style, unused?
  $"02"
  // Size = 12 point
  $"000C"
  // Color, RGB
  $"0000 0000 0000"
};
