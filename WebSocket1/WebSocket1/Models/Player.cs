using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSocket1.Models
{
    public class Player
    {
        public string name { get; set; }
        public int id { get; set; }
        public List<Tile> tiles { get; set; }
        public Player(string name)
        {
            this.name = name;
        }

    }
}