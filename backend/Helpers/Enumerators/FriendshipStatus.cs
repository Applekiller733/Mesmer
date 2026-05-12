namespace SongAppApi.Helpers.Enumerators
{
    public enum FriendshipStatus
    {
        /// <summary>
        /// Sender has invited Receiver. Receiver may accept (→ Accepted)
        /// or decline (row deleted).
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Friendship is mutual and active. Both parties can see each
        /// other normally; downstream features (messaging, playlist
        /// sharing) check for this state.
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// Sender has blocked Receiver. Receiver loses visibility of the
        /// Sender (profile, playlists, messages). Sender's view of the
        /// Receiver depends on application policy — a separate row may
        /// or may not exist for the reverse direction.
        ///
        /// Block always wins: a Blocked row prevents any other state on
        /// either direction (no pending requests, no accepted friendship)
        /// until it's lifted by deleting the Blocked row.
        /// </summary>
        Blocked = 2,
    }

}
