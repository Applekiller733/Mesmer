namespace SongAppApi.Helpers.Enumerators
{
    public enum PlaylistVisibility
    {
        Private = 0, // owner only
        Unlisted = 1, //shareable, not discoverable
        Public = 2, //public, viewable on profile etc.
    }
}