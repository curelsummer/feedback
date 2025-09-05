using Microsoft.Xaml.Behaviors.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    public class UserInfoModel
    {
        private string _testNumber = "";
        private string _detectNumber = "";
        private string _name = "";
        private string _sex = "";
        private string _birthDay = "";
        private string _age = "";
        private string _createDateTime = "";
        private string _remarks1 = "";
        private string _remarks2 = "";
        private string _status = "";

        public string TestNumber { get { return _testNumber; } set { _testNumber = value; } }
        public string DetectNumber { get { return _detectNumber; } set { _detectNumber = value; } }
        public string Name { get { return _name;} set { _name = value; } }
        public string Sex { get { return _sex;} set { _sex = value; } }
        public string BirthDay { get { return _birthDay; } set { _birthDay = value; } }
        public string Age { get { return CalculateAge(DateTime.Parse(BirthDay)).ToString(); }  }
        public string CreateDateTime { get { return _createDateTime; } set { _createDateTime = value; } }
        public string Remarks1 { get { return _remarks1; } set { _remarks1 = value; } }
        public string Remarks2 { get { return _remarks2; } set { _remarks2 = value; } }
        public string Status { get { return _status; } set { _status = value; } }

        public string GameStartTime { get; set; }
        public string GameEndTime { get; set; }

        public int TotalPowerTimes { get; set; }
        public int ValidTimes {  get; set; }
        public void clearInfo()
        {
            TestNumber = "";
            DetectNumber = "";
            Name = "";
            Sex = "";
            BirthDay = "";
            Remarks1 = "";
            Remarks2 = "";
            Status = "未开始";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="birthDate"></param>
        /// <returns></returns>
        public int CalculateAge(DateTime birthDate)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;

            // 如果生日还没过，则年龄减1
            if (birthDate > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }


        public UserInfoModel Clone()
        {
            return new UserInfoModel
            {
                TestNumber = this.TestNumber,
                DetectNumber = this.DetectNumber,
                Name = this.Name,
                Sex = this.Sex,
                BirthDay = this.BirthDay,
                Remarks1 = this.Remarks1,
                Remarks2 = this.Remarks2,
                Status = this.Status
            };
        }
    }
}
