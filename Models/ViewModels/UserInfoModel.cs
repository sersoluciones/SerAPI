namespace SerAPI.Models.ViewModels
{
    public class UserInfoModel
    {
        public string sub { get; set; }

        public string username { get; set; }

        public string name { get; set; }

        public string last_name { get; set; }

        public string email { get; set; }

        public bool email_verified { get; set; }

        public bool phone_number_verified { get; set; }

        public string role { get; set; }

        public string[] claims { get; set; }

        public string photo { get; set; }

        public string phone_number { get; set; }

        public bool dark_mode { get; set; }

        public bool is_super_user { get; set; }

    }
}
