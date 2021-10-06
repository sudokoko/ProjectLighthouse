using ProjectLighthouse.Serialization;

namespace ProjectLighthouse.Types {
    public class Location {
        public Location(int x, int y) {
            this.X = x;
            this.Y = y;
        }
        
        public int X;
        public int Y;

        public string Serialize() {
            return LbpSerializer.StringElement("x", this.X) +
                   LbpSerializer.StringElement("x", this.Y);
        }
    }
}