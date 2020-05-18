using System.Linq;

namespace B320.Transformers
{
    public class L33TTransformer : ITextTransformer
    {
        public string Encode(string data)
        {
            string map(char ch) => ch switch
            {
                'a' => "4", 'A' => "4",
                'e' => "3", 'E' => "3",
                'i' => "1", 'I' => "1",
                'o' => "0", 'O' => "0",
                's' => "5", 'S' => "5",
                't' => "7", 'T' => "7",
                _ => ch.ToString()
            };

            string mappedTransform = string.Join("", data.Select(map));
            return mappedTransform;
        }

        public string Decode(string data)
        {
            string map(char ch) => ch switch
            {
                '4' => "a", '3' => "e",
                '1' => "i", '0' => "o",
                '5' => "s", '7' => "t",
                _ => ch.ToString()
            };

            string mappedTransform = string.Join("", data.Select(map));
            return mappedTransform;
        }
    }
}