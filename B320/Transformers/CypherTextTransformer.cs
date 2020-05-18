using System.Collections.Generic;
using System.Linq;

namespace B320.Transformers
{
    public class CypherTextTransformer: ITextTransformer
    {
        private IDictionary<char, char> _characterMap;
        public CypherTextTransformer()
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