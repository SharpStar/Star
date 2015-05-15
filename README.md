# Star
A Starbound proxy server implementation


[![Build Status](https://travis-ci.org/SharpStar/Star.svg)](https://travis-ci.org/SharpStar/Star)
Linux

[![Build status](https://ci.appveyor.com/api/projects/status/nyw18cah6foqov0y/branch/master?svg=true)](https://ci.appveyor.com/project/Mitch528/star/branch/master)
Windows

# Downloads
Coming soon!

# Installation

## Linux

### Prerequisites
- Mono (optional)
- libsqlite3-dev

There are two options for running Star on Linux. If you want to run Star as a daemon, you'll want to pick the second option.

#### Option 1

1. Run ./SharpStar
2. Refer to Windows steps 2 and 3

#### Option 2

1. Install Mono (v 4.0.0+) through a package manager or compile it manually ([refer here](<http://www.mono-project.com/docs/compiling-mono/linux/>)).
   - Ubuntu: sudo apt-get install mono-complete
2. Run "mono SharpStar.exe" for normal usage. If you want to run it as a daemon, run "mono-service SharpStar.exe" instead.
4. Refer to Windows steps 2 and 3

## Windows

### Prerequisites
- .NET Framework 4.5

1. Run SharpStar.exe
2. Open serverconfig.json in your favorite text editor and modify the properties to your liking.
3. Enter the command "reloadconfigs" (without the quotes) into the console