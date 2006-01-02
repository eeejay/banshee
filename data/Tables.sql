--This file contains most of the Database construction/initialization
--for banshee. Because SQL92 does not support "IF [NOT] EXIST", I have
--added a proprietary rule '--IF TABLE NOT EXISTS {Name}' that when 
--encountered will cause the parser to query the database for the table 
--and only execute the next statement if the table does not exist. 

USE Library;

--Track Table Setup

--IF TABLE NOT EXISTS Tracks;
CREATE TABLE Tracks (
	TrackID INTEGER PRIMARY KEY,
	Uri TEXT NOT NULL,
	MimeType TEXT,
	
	Artist TEXT,
	Performer TEXT,
	AlbumTitle INTEGER,
	ReleaseDate Date,
	ASIN TEXT,
	Label TEXT,
	Title TEXT,
	Genre TEXT,
	Year INTEGER,
	
	TrackNumber INTEGER,
	TrackCount INTEGER,
	Duration INTEGER,
	
	TrackGain FLOAT,
	TrackPeak FLOAT,
	AlbumGain FLOAT,
	AlbumPeak FLOAT,
	
	Rating INTEGER,
	NumberOfPlays INTEGER,
	LastPlayedStamp INTEGER,
	DateAddedStamp INTEGER,
	
	RemoteLookupStatus INTEGER
);

--IF TABLE NOT EXISTS Playlists;
CREATE TABLE Playlists (
	PlaylistID INTEGER PRIMARY KEY,
	Name TEXT NOT NULL
);

--IF TABLE NOT EXISTS PlaylistEntries;
CREATE TABLE PlaylistEntries (
	EntryID INTEGER PRIMARY KEY,
	PlaylistID INTEGER NOT NULL,
	TrackID INTEGER NOT NULL
);

--IF TABLE NOT EXISTS SmartPlaylists;
CREATE TABLE SmartPlaylists (
	PlaylistID INTEGER PRIMARY KEY,
	Name TEXT NOT NULL,
	SelectedBy TEXT NOT NULL,
	LimitToType TEXT NOT NULL,
	LimitToValue TEXT NOT NULL
);

--IF TABLE NOT EXISTS SmartPlaylistMatches;
CREATE TABLE SmartPlaylistMatches (
	MatchID INTEGER PRIMARY KEY,
	PlaylistID INTEGER NOT NULL,
	Field TEXT NOT NULL,
	Condition TEXT NOT NULL,
	Value TEXT NOT NULL
);
