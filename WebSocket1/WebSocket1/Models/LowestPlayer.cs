
namespace WebSocket1.Models
{
    public class LowestPlayer
    {
        public string name{get; set;}
        public int points { get; set;}
        public LowestPlayer(string name, int points)
        {
            this.name = name;
            this.points = points;
        }
    }
}