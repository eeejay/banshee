#!/usr/bin/perl

# This scripts takes the mimetypes.txt list, make sure it's sorted and
# has no duplicates, and outputs a ; separated list of them.

use strict;

my %mimetypes;

# Read in and store in hash so unique
open (IN, 'mimetypes.txt');
foreach my $mimetype (readline(IN)) {
    chomp($mimetype);
    $mimetypes{$mimetype} = 1;
}
close (IN);

# Write out in sorted order, and print in ; separated list
open (OUT, '>mimetypes.txt');
foreach my $mimetype (sort(keys(%mimetypes))) {
    print OUT "$mimetype\n";
    if ($mimetype !~ /^#/) {
        print "$mimetype;";
    }
}
close (OUT);

print "\n";
