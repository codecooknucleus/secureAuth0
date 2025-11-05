namespace secureAuth0.Models
{
    public class UserProfile
    {

        public string Name { get; set; }
        public string Email { get; set; }


        // public string email { get; set; }

        public bool email_verified { get; set; }
        public string created_at { get; set; }
        public List<Identity> identities { get; set; }

        // public string name { get; set; }


        public string nickname { get; set; }
        public string picture { get; set; }
        public string updated_at { get; set; }
        public string user_id { get; set; }
        public UserMetadata user_metadata { get; set; }
        public string last_password_reset { get; set; }
        public AppMetadata app_metadata { get; set; }
        public string last_ip { get; set; }
        public string last_login { get; set; }
        public int logins_count { get; set; }
        public string ProfileImageUrl { get; set; }

    }


    public class Identity
    {
        public string connection { get; set; }
        public string provider { get; set; }
        public string user_id { get; set; }
        public bool isSocial { get; set; }
        public dynamic profileData { get; set; }
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }


    public class UserMetadata
    {
        public string company_name { get; set; }
        public string job_title { get; set; }
        public string phone_number { get; set; }
    }


    public class AppMetadata
    {
        public string App_screen_prefrence { get; set; }
        public bool privacy_policies { get; set; }
        public long privacy_policies_timestamp { get; set; }
    }

}
