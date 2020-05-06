using System.Collections.Generic;
using System.Linq;

namespace B320
{
    public interface ITextTransformer
    {
        string Encode(string data);
        string Decode(string data);
    }

    public class L33tTransformer : ITextTransformer
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

    
    public class CypherTestTransformer: ITextTransformer
    {
        private IDictionary<char, char> _characterMap;
        public CypherTestTransformer()
        {
            var lowerCase = Enumerable.Range('a', 26).Select(x => (char) x);
            var upperCase = Enumerable.Range('A', 26).Select(x => (char) x);
            
            var mappedTuples = new[]
            {
                lowerCase.Zip(lowerCase.Skip(13).Concat(lowerCase.Take(13)), (src,val) => (src,val)),
                upperCase.Zip(upperCase.Skip(13).Concat(upperCase.Take(13)), (src,val) => (src,val))
            };
            
            _characterMap = mappedTuples.SelectMany(x => x)
                .ToDictionary(x => x.src, x => x.val);

        }
        public string Encode(string data)
        {
            string map(char ch) => _characterMap[ch].ToString();
            
            string mappedTransform = string.Join("", data.Select(map));
            return mappedTransform;
        }

        public string Decode(string data)
        {
            string map(char ch) => _characterMap[ch].ToString();
            
            string mappedTransform = string.Join("", data.Select(map));
            return mappedTransform;
        }
    }
}