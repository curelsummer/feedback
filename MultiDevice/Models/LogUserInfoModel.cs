using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    public enum UserRole
    {
        医生,
        患者
    }

    public class LogUserInfoModel
    {
        public string UserName { get; set;}
        public string Password { get; set;} 
        public UserRole UserRole { get; set;}
    }
}
