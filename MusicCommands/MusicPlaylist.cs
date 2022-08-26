using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCommands
{
    internal class MusicPlaylist
    {
        internal MusicPlaylist()
        {
            Console.WriteLine("Initialized playlist.");
        }

        public Queue<AudioFile> QueueList = new();

        public void Enqueue(AudioFile query) => QueueList.Enqueue(query);
        public void ClearQueue()             => QueueList.Clear();

        public int       Count       => QueueList.Count;
        public AudioFile GetNextSong => QueueList.Dequeue();
        public AudioFile WhatIsNext  => QueueList.Peek();
    }
}
