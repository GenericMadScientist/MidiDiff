using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            string firstSong = args[0];
            string secondSong = args[1];

            MidiFile firstFile = MidiFile.Read(firstSong);
            MidiFile secondFile = MidiFile.Read(secondSong);

            var oldTempoMap = firstFile.GetTempoMap();
            var newTempoMap = secondFile.GetTempoMap();

            IList<string> firstTrackNames = TrackNames(firstFile);
            IList<string> secondTrackNames = TrackNames(secondFile);
            IList<string> commonTracks = new List<string>();

            foreach (string name in firstTrackNames)
            {
                if (secondTrackNames.Contains(name))
                {
                    commonTracks.Add(name);
                }
                else
                {
                    Console.WriteLine($"First midi contains track {name} that second midi lacks");
                }
            }
            foreach (string name in secondTrackNames)
            {
                if (!firstTrackNames.Contains(name))
                {
                    Console.WriteLine($"Second midi contains track {name} that first midi lacks");
                }
            }

            foreach (string track in commonTracks)
            {
                TrackChunk oldTrack = GetTrackByName(firstFile, track);
                TrackChunk newTrack = GetTrackByName(secondFile, track);
                Console.WriteLine($"Comparing track {track}");
                CompareTracks(oldTrack, newTrack, oldTempoMap, newTempoMap);
                Console.WriteLine();
            }
        }

        private static IList<string> TrackNames(MidiFile file)
        {
            return file.GetTrackChunks().Select(x => TrackName(x)).ToList();
        }

        private static TrackChunk GetTrackByName(MidiFile file, string trackName)
        {
            foreach (TrackChunk trackChunk in file.GetTrackChunks())
            {
                if (TrackName(trackChunk) == trackName)
                {
                    return trackChunk;
                }
            }

            throw new ArgumentException();
        }

        private static string TrackName(TrackChunk trackChunk)
        {
            foreach (MidiEvent e in trackChunk.Events)
            {
                if (e.EventType == MidiEventType.SequenceTrackName)
                {
                    return ((SequenceTrackNameEvent)e).Text;
                }
            }

            return null;
        }

        private static void CompareTracks(TrackChunk oldTrack, TrackChunk newTrack, TempoMap oldTempoMap, TempoMap newTempoMap)
        {
            IList<TimedEvent> oldEvents = oldTrack.GetTimedEvents().ToList();
            IList<TimedEvent> newEvents = newTrack.GetTimedEvents().ToList();
            oldEvents = SetNoteOffsToZeroVelocity(oldEvents);
            newEvents = SetNoteOffsToZeroVelocity(newEvents);
            IList<(TimedEvent, int)> eventDiffs = new List<(TimedEvent, int)>();

            foreach (TimedEvent e in ExceptIn(oldEvents, newEvents))
            {
                eventDiffs.Add((e, 0));
            }

            foreach (TimedEvent e in ExceptIn(newEvents, oldEvents))
            {
                eventDiffs.Add((e, 1));
            }

            IEnumerable<(TimedEvent, int)> query = eventDiffs.OrderBy(pair => pair.Item1.Time);

            foreach ((TimedEvent, int) pair in query)
            {
                var tempoMap = pair.Item2 == 0 ? oldTempoMap : newTempoMap;
                Console.WriteLine($"Midi {pair.Item2 + 1} has event {pair.Item1} ({HumanTime(pair.Item1.Time, tempoMap)})");
            }
        }

        private static string HumanTime(long position, TempoMap tempoMap)
        {
            var time = (MetricTimeSpan)TimeConverter.ConvertTo(position, TimeSpanType.Metric, tempoMap);
            int minutes = 60 * time.Hours + time.Minutes;
            int seconds = time.Seconds;
            int millisecs = time.Milliseconds;
            return $"{minutes}:{seconds:d2}.{millisecs:d3}";
        }

        private static IList<TimedEvent> SetNoteOffsToZeroVelocity(IList<TimedEvent> events)
        {
            IList<TimedEvent> newEvents = new List<TimedEvent>();

            foreach (TimedEvent e in events)
            {
                if (e.Event.EventType == MidiEventType.NoteOff)
                {
                    ((NoteOffEvent)e.Event).Velocity = new SevenBitNumber(0);
                }
                newEvents.Add(e);
            }

            return newEvents;
        }

        // Yes this is O(n^2), no I do not care.
        private static IList<TimedEvent> ExceptIn(IList<TimedEvent> oldEvents, IList<TimedEvent> newEvents)
        {
            IList<TimedEvent> retVal = new List<TimedEvent>();

            foreach (TimedEvent e in oldEvents)
            {
                bool isInNew = false;
                foreach (TimedEvent e2 in newEvents)
                {
                    if (e.Time != e2.Time)
                    {
                        continue;
                    }
                    if (MidiEvent.Equals(e.Event, e2.Event))
                    {
                        isInNew = true;
                        break;
                    }
                }
                if (!isInNew)
                {
                    retVal.Add(e);
                }
            }

            return retVal;
        }
    }
}
