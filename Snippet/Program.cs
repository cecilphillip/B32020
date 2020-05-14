namespace Snippet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string secret = "uggcf://nxn.zf/ohvyqgunaxf\nGunax*Lbh*Nyy!*🌮🌮";
            string result = string.Empty;
            
            foreach (char c in secret)
            {
                if((c >= 97 && c <= 122) || (c >= 65 && c <= 90))
                { int cc = ((c & 223) - 52) % 26 + (c & 32) + 65; result+=(char)cc; }
                else if (c == '*') { result+=" "; } 
                else { result += c; }
            }
            System.Console.WriteLine(result.ToString());
        }
    }
}