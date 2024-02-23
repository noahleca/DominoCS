using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSocket1.Models
{
    public class Tile
    {
        public string face1 { get; set; }
        public string face2 { get; set; }
        public string tile { get; set; }
        public Tile() { }
        public Tile(string face1, string face2, string tile)
        {
            this.tile = tile;
            this.face1 = face1;
            this.face2 = face2;
        }
    }
}