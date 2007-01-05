/***************************************************************************
 *  LibrarySchema.cs
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
using Banshee.Configuration;
 
namespace Banshee.Configuration.Schema
{
    public static class LibrarySchema
    {
        public static readonly SchemaEntry<string> Location = new SchemaEntry<string>(
            "library", "base_location",
            String.Empty,
            "Library location",
            "Base location for library music"
        );
    
        public static readonly SchemaEntry<string> FolderPattern = new SchemaEntry<string>(
            "library", "folder_pattern",
            Banshee.Base.FileNamePattern.DefaultFolder,
            "Library Folder Pattern",
            "Format for creating a track folder inside the library. Do not create an absolute path. " +
                "Path here is relative to the Banshee music directory. See LibraryLocation. Legal tokens: " +
                "%artist%, %album%, %title%, %track_number%, %track_count%, %track_number_nz% (No prefixed zero), " + 
                "%track_count_nz% (No prefixed zero), %path_sep% (portable directory separator (/))."
        );

        public static readonly SchemaEntry<string> FilePattern = new SchemaEntry<string>(
            "library", "file_pattern",
            Banshee.Base.FileNamePattern.DefaultFile,
            "Library File Pattern",
            "Format for creating a track filename inside the library. Do not use path tokens/characters here. " +
                "See LibraryFolderPattern. Legal tokens: %artist%, %album%, %title%, %track_number%, %track_count%, " +
                "%track_number_nz% (No prefixed zero), %track_count_nz% (No prefixed zero)."
        );

        public static readonly SchemaEntry<int> SortColumn = new SchemaEntry<int>(
            "library", "sort_column",
            -1,
            "Column index",
            "Column index for sorting the library source. -1 for unset."
        );

        public static readonly SchemaEntry<int> SortType = new SchemaEntry<int>(
            "library", "sort_type",
            0,
            "Column sort type",
            "Column sort type for the library source. Ascending (0) or Descending (1)"
        );
        
        public static readonly SchemaEntry<bool> SourceExpanded = new SchemaEntry<bool>(
            "library", "source_expanded",
            true,
            "Library source expansion",
            "Whether to expand the library node in the source view"
        );
        
        public static readonly SchemaEntry<bool> CopyOnImport = new SchemaEntry<bool>(
            "library", "copy_on_import",
            false,
            "Copy music on import",
            "Copy and rename music to banshee music library directory when importing"
        );

        public static readonly SchemaEntry<bool> MoveOnInfoSave = new SchemaEntry<bool>(
            "library", "move_on_info_save",
            false,
            "Move music on info save",
            "Move music within banshee music library directory when saving track info"
        );
        
        public static readonly SchemaEntry<bool> WriteMetadata = new SchemaEntry<bool>(
            "library", "write_metadata",
            false,
            "Write metadata back to audio files",
            "If enabled, metadata (tags) will be written back to audio files when using the track metadata editor."
        );
        
        public static readonly SchemaEntry<int> PlaylistSortOrder = new SchemaEntry<int>(
            "library", "playlist_sort_order",
            0,
            "Sort order of playlists",
            "Sort order of library playlists in the source view (0 = Ascending, 1 = Descending)"
        );
        
        public static readonly SchemaEntry<int> PlaylistSortCriteria = new SchemaEntry<int>(
            "library", "playlist_sort_criteria",
            0,
            "Sort criteria of playlists",
            "Sort criteria of library playlists in the source view (0 = Name, 1 = Size)"
        );
    }
}
