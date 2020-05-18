namespace B320.Transformers
{
    public interface ITextTransformer
    {
        string Encode(string data);
        string Decode(string data);
    }
}