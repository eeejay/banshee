/* ex: set ts=4: */
/***************************************************************************
 *  banshee.c
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
 
#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdarg.h>
#include <unistd.h>
#include <errno.h>
#include <signal.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <sys/types.h>

static pid_t mono_pid;

static char *
strconcat(const char *first, ...)
{
	size_t length;
	va_list argp;
	char *p, *buf;
	
	va_start(argp, first);

	length = strlen(first);
	while((p = va_arg(argp, char *)) != 0) {
		length += strlen(p);
	}

	buf = (char *)malloc(length + 1);

	va_start(argp, first);
	strcpy(buf, first);

	while((p = va_arg(argp, char *)) != 0) {
		strcat(buf, p);
	}

	va_end(argp);

	return buf;
}

static void
setup_environment(int local_exe)
{
	char *dyld_library_path;
	char *ld_library_path;
	char *mono_path;
	
	dyld_library_path = strconcat(LIBDIR, ":", getenv("DYLD_LIBRARY_PATH"), 0);
	ld_library_path = strconcat(LIBDIR, ":", getenv("LD_LIBRARY_PATH"), 0);

	setenv("DYLD_LIBRARY_PATH", dyld_library_path, 1);
	setenv("LD_LIBRARY_PATH", ld_library_path, 1);
	setenv("HELIX_LIBS", HELIX_LIBRARY_PATH, 1);
	
	free(dyld_library_path);
	free(ld_library_path);
	
	if(local_exe != 0) {
		mono_path = strconcat(
			"../entagged-sharp/",
			":../hal-sharp/:",
			"../burn-sharp/:",
			"../plugincore/:",
			"/usr/lib/ipod-sharp/:", 
			getenv("MONO_PATH"), 0);
		setenv("MONO_PATH", mono_path, 1);
		free(mono_path);
	}
}

static void
signal_handler(int signum)
{
	kill(mono_pid, signum);
}

int 
main(int argc, char **argv)
{
	char *mono_exe = 0;
	char **shift_argv;
	struct stat statbuf;
	int p_status;
	int local_exe = 0;
	int i;

	if(stat("./banshee.exe", &statbuf) == 0 
		&& stat("./ConfigureDefines.cs", &statbuf) == 0 
		&& stat("./Makefile", &statbuf) == 0) {
		puts("*** Warning - Running UNINSTALLED banshee.exe ***\n");
		mono_exe = strdup("./banshee.exe");
		local_exe = 1;
	} else {
		mono_exe = strconcat(LIBDIR, "/", PACKAGE, "/banshee.exe", 0);
		local_exe = 0;
	}
	
	setup_environment(local_exe);
	
	shift_argv = (char **)malloc((argc + local_exe + 2) * sizeof(char *));
	shift_argv[0] = argv[0];
	
	if(local_exe == 0) {
		shift_argv[1] = mono_exe; 
	} else {
		shift_argv[1] = "--debug";
		shift_argv[2] = mono_exe;
	}
	
	for(i = 1; i < argc; i++)
		shift_argv[i + local_exe + 1] = argv[i];

	shift_argv[argc + local_exe + 1] = 0;

	signal(SIGTERM, signal_handler);
	signal(SIGINT, signal_handler);

	if((mono_pid = fork()) == 0) 
		execvp(MONO, shift_argv);
	
	waitpid(mono_pid, &p_status, WUNTRACED);
	
	free(mono_exe);
	free(shift_argv);
	
	exit(errno);
}

