# BIGly

## Introduction

BIGly is a dirt simple extractor and packer for .big files used by EA games in the early-mid 2000s. It will allow extracting a .big file in its entirety or packing a bunch of folders into a .big file from the command line. It's meant as a simple tool for automation and bulk operations and will never have any real UI.

It will be tested with Command and Conquer Generals because that's what I've been working with but should work with any .big file in theory.

## Usage (eventually)

bigly extract INIZH.big

bigly pack --generals test.big

bigly pack -i Window -i Art -i Data -i Maps test.big

## Current Status

Very early, not really usable yet.

Includes a dependency just thrown haphazardly into the project, which will be changed once I fix up a proper fork of that.