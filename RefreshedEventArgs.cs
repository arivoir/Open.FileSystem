namespace Open.FileSystem
{
    public class RefreshedEventArgs : AsyncEventArgs
    {
        public RefreshedEventArgs(string dirId = null)
            : base()
        {
            DirId = dirId;
        }

        public string DirId { get; private set; }
    }
}
