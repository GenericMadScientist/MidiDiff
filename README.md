# MidiDiff

A program to check for differences between nearly identical rips for Clone Hero.

## About

If you're not converting songs from games like Guitar Hero and Rock Band for use
in Clone Hero, this is probably not of any interest to you. This project is
specifically to aid in that effort.

MidiDiff checks for any tracks present in only one midi, and any midi events
only in one of the midis.

## Install

Grab the executable from the [Releases page](../../releases).

## Usage

Specify the paths to two midi files and MidiDiff will check for differences
between them.

```bat
MidiDiff.exe "first-chart.midi" "second-chart.midi"
```
