namespace Upland.Types.BlockchainTypes
{
    public class GetEOSFlareActionsRequest
    {
        public string account_name { get; set; }
        public long pos { get; set; }
        public int offset { get; set; }
    }
}
