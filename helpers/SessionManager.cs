namespace AMONIC.Helpers
{
    public static class SessionManager
    {
        public static int CurrentUserID { get; set; }
        public static string CurrentUserEmail { get; set; }
        public static string CurrentUserFirstName { get; set; }
        public static string CurrentUserLastName { get; set; }
        public static string CurrentUserRole { get; set; }
        public static int CurrentSessionID { get; set; }

        public static string FullName
        {
            get { return $"{CurrentUserFirstName} {CurrentUserLastName}"; }
        }
    }
}