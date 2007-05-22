Banshee.Base.FileNamePattern.Filter = { songpath as string |
	@/[ ]+/.Replace(@/[^0-9A-Za-z\/ ]+/.Replace(songpath, "").ToLower(), "_")
}

